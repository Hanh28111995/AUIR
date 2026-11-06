using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading.Channels;
using AUIR_Software.Models.LLM;
using AUIR_Software.Services.Text;
using Microsoft.AspNetCore.Hosting;

namespace AUIR_Software.Services;

public sealed class RagUploadQueue : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Channel<string> _jobs;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeJobs = new();
    private readonly ConcurrentDictionary<string, byte> _cancelledJobs = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppConfig _config;
    private readonly ILogger<RagUploadQueue> _logger;
    private readonly string _queueDirectory;

    public RagUploadQueue(
        IServiceScopeFactory scopeFactory,
        AppConfig config,
        IWebHostEnvironment environment,
        ILogger<RagUploadQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
        _jobs = Channel.CreateBounded<string>(new BoundedChannelOptions(
            Math.Max(1, config.Rag.UploadQueueCapacity))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
        _queueDirectory = Path.IsPathRooted(config.Rag.UploadQueuePath)
            ? config.Rag.UploadQueuePath
            : Path.Combine(environment.ContentRootPath, config.Rag.UploadQueuePath);
    }

    public async Task<RagUploadJob> EnqueueAsync(
        IReadOnlyList<(IFormFile File, string Source)> files,
        IReadOnlyList<RagUploadSkippedFile> skippedFiles,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_queueDirectory);
        var id = Guid.NewGuid().ToString("N");
        var dataDirectory = GetDataDirectory(id);
        Directory.CreateDirectory(dataDirectory);

        var job = new RagUploadJob
        {
            Id = id,
            Status = "Queued",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            TotalFiles = files.Count,
            SkippedFiles = skippedFiles.ToList()
        };

        try
        {
            foreach (var (file, source) in files)
            {
                var extension = Path.GetExtension(source).ToLowerInvariant();
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var path = Path.Combine(dataDirectory, storedName);
                await using var output = new FileStream(
                    path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                await file.CopyToAsync(output, cancellationToken);
                job.Files.Add(new RagUploadFile(source, storedName));
            }

            await SaveJobAsync(job, cancellationToken);
            if (!_jobs.Writer.TryWrite(id))
            {
                throw new InvalidOperationException("The RAG upload queue is not accepting new jobs.");
            }

            return job;
        }
        catch
        {
            if (Directory.Exists(dataDirectory))
            {
                Directory.Delete(dataDirectory, recursive: true);
            }

            var manifestPath = GetManifestPath(id);
            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }

            throw;
        }
    }

    public async Task<RagUploadJob?> GetJobAsync(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(id, "N", out var parsedId))
        {
            return null;
        }

        id = parsedId.ToString("N");
        var manifestPath = GetManifestPath(id);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(manifestPath);
        return await JsonSerializer.DeserializeAsync<RagUploadJob>(
            stream, JsonOptions, cancellationToken);
    }

    public async Task<bool> CancelAsync(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(id, "N", out var parsedId))
        {
            return false;
        }

        id = parsedId.ToString("N");
        var job = await GetJobAsync(id, cancellationToken);
        if (job is null || job.Status is "Completed" or "Failed" or "TimedOut" or "Cancelled")
        {
            return false;
        }

        _cancelledJobs[id] = 0;
        if (_activeJobs.TryGetValue(id, out var activeCts))
        {
            activeCts.Cancel();
        }
        else
        {
            _cancelledJobs.TryRemove(id, out _);
        }

        job.Status = "Cancelled";
        job.Stage = "Cancelled";
        job.Error = "The upload job was cancelled.";
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await SaveJobAsync(job, cancellationToken);
        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverPendingJobsAsync(stoppingToken);

        await foreach (var id in _jobs.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected RAG upload worker error. JobId={JobId}", id);
            }
        }
    }

    private async Task RecoverPendingJobsAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_queueDirectory);
        foreach (var manifestPath in Directory.EnumerateFiles(_queueDirectory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var stream = File.OpenRead(manifestPath);
                var job = await JsonSerializer.DeserializeAsync<RagUploadJob>(
                    stream, JsonOptions, cancellationToken);
                if (job is null || job.Status is not ("Queued" or "Processing"))
                {
                    continue;
                }

                if (job.Status == "Processing" &&
                    DateTimeOffset.UtcNow - job.UpdatedAtUtc >
                    TimeSpan.FromSeconds(_config.Rag.ProcessingTimeoutSeconds))
                {
                    job.Status = "TimedOut";
                    job.Stage = "TimedOut";
                    job.Error = "The job was still Processing after the configured timeout and was not resumed automatically.";
                    job.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    await SaveJobAsync(job, cancellationToken);
                    _logger.LogWarning(
                        "Stale RAG upload job marked as timed out during recovery. JobId={JobId}",
                        job.Id);
                    continue;
                }

                job.Status = "Queued";
                job.Stage = "Queued";
                job.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await SaveJobAsync(job, cancellationToken);
                await _jobs.Writer.WriteAsync(job.Id, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover RAG upload job. Manifest={ManifestPath}", manifestPath);
            }
        }
    }

    private async Task ProcessJobAsync(string id, CancellationToken cancellationToken)
    {
        var job = await GetJobAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"RAG upload job {id} was not found.");

        if (job.Status is "Completed" or "Failed" or "TimedOut" or "Cancelled")
        {
            _cancelledJobs.TryRemove(id, out _);
            return;
        }

        using var processingTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        processingTimeout.CancelAfter(TimeSpan.FromSeconds(_config.Rag.ProcessingTimeoutSeconds));
        var jobCancellationToken = processingTimeout.Token;
        _activeJobs[id] = processingTimeout;

        job.Status = "Processing";
        job.Stage = "Starting";
        job.Error = null;
        await SaveJobAsync(job, cancellationToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var rag = scope.ServiceProvider.GetRequiredService<RagPipeline>();

            for (var index = job.NextFileIndex; index < job.Files.Count; index++)
            {
                jobCancellationToken.ThrowIfCancellationRequested();
                var file = job.Files[index];
                var raw = string.Empty;
                var extractionFailed = false;
                job.Stage = $"Extracting:{file.Source}";
                await SaveJobAsync(job, CancellationToken.None);
                try
                {
                    var path = Path.Combine(GetDataDirectory(id), file.StoredName);
                    raw = Path.GetExtension(file.Source).ToLowerInvariant() switch
                    {
                        ".pdf" => PdfTextExtractor.Extract(path),
                        ".txt" or ".md" => await File.ReadAllTextAsync(path, jobCancellationToken),
                        _ => string.Empty
                    };
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    extractionFailed = true;
                    _logger.LogWarning(
                        ex, "Failed to extract uploaded file text. JobId={JobId} FileName={FileName}",
                        id, file.Source);
                    job.SkippedFiles.Add(new RagUploadSkippedFile(
                        file.Source, "File processing failed. See server logs."));
                }

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    job.Stage = $"EmbeddingAndUpserting:{file.Source}";
                    await SaveJobAsync(job, CancellationToken.None);
                    var chunks = TextChunker.Chunk(
                        raw, _config.Rag.ChunkSize, _config.Rag.ChunkOverlap).ToList();
                    await rag.ReplaceDocumentAsync(file.Source, chunks, jobCancellationToken);
                    job.IngestedChunks += chunks.Count;
                }
                else if (!extractionFailed)
                {
                    job.SkippedFiles.Add(new RagUploadSkippedFile(
                        file.Source, "No extractable text found."));
                }

                job.CompletedFiles++;
                job.NextFileIndex = index + 1;
                job.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await SaveJobAsync(job, cancellationToken);
            }

            job.Status = "Completed";
            job.Stage = "Completed";
        }
        catch (OperationCanceledException) when (_cancelledJobs.TryRemove(id, out _))
        {
            job.Status = "Cancelled";
            job.Stage = "Cancelled";
            job.Error = "The upload job was cancelled.";
            job.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await SaveJobAsync(job, CancellationToken.None);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            job.Status = "Queued";
            job.Stage = "Cancelled";
            job.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await SaveJobAsync(job, CancellationToken.None);
            throw;
        }
        catch (OperationCanceledException) when (processingTimeout.IsCancellationRequested)
        {
            job.Status = "TimedOut";
            job.Stage = "TimedOut";
            job.Error = $"Processing exceeded {_config.Rag.ProcessingTimeoutSeconds} seconds.";
            _logger.LogError(
                "RAG upload job timed out. JobId={JobId} Stage={Stage} TimeoutSeconds={TimeoutSeconds}",
                id, job.Stage, _config.Rag.ProcessingTimeoutSeconds);
        }
        catch (Exception ex)
        {
            job.Status = "Failed";
            job.Stage = "Failed";
            job.Error = ex.Message;
            _logger.LogError(ex, "RAG upload job failed. JobId={JobId} Stage={Stage}", id, job.Stage);
        }
        finally
        {
            _activeJobs.TryRemove(id, out _);
            _cancelledJobs.TryRemove(id, out _);
            job.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await SaveJobAsync(job, CancellationToken.None);
            if ((job.Status is "Completed" or "Failed" or "TimedOut" or "Cancelled") && Directory.Exists(GetDataDirectory(id)))
            {
                try
                {
                    Directory.Delete(GetDataDirectory(id), recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogError(ex, "Failed to remove staged upload files. JobId={JobId}", id);
                }
            }
        }
    }

    private async Task SaveJobAsync(RagUploadJob job, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_queueDirectory);
        var manifestPath = GetManifestPath(job.Id);
        var temporaryPath = $"{manifestPath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = new FileStream(
            temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, job, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, manifestPath, overwrite: true);
    }

    private string GetManifestPath(string id) => Path.Combine(_queueDirectory, $"{id}.json");
    private string GetDataDirectory(string id) => Path.Combine(_queueDirectory, id);
}

public sealed class RagUploadJob
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public string Stage { get; set; } = "Queued";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public int IngestedChunks { get; set; }
    public int NextFileIndex { get; set; }
    public List<RagUploadFile> Files { get; set; } = [];
    public List<RagUploadSkippedFile> SkippedFiles { get; set; } = [];
    public string? Error { get; set; }
}

public sealed record RagUploadFile(string Source, string StoredName);
public sealed record RagUploadSkippedFile(string File, string Reason);
