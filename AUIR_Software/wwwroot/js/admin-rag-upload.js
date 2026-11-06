document.addEventListener('DOMContentLoaded', function () {
    var form = document.getElementById('rag-upload-form');
    if (!form) {
        return;
    }

    var fileInput = document.getElementById('rag-upload-files');
    var submitButton = document.getElementById('rag-upload-submit');
    var status = document.getElementById('rag-upload-status');
    var documentsList = document.getElementById('rag-documents-list');
    var documentsStatus = document.getElementById('rag-documents-status');
    var refreshButton = document.getElementById('rag-documents-refresh');

    async function readResponse(response) {
        var text = await response.text();
        if (!text) {
            return null;
        }

        try {
            return JSON.parse(text);
        } catch {
            return text;
        }
    }

    function getMessage(result, fallback) {
        return typeof result === 'string'
            ? result
            : result && (result.message || result.Message) || fallback;
    }

    function getRequestError(response, result, fallback) {
        if (response.status === 401) {
            return getMessage(result, 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
        }
        if (response.status === 403) {
            return getMessage(result, 'Bạn không có quyền thực hiện thao tác này.');
        }
        return getMessage(result, fallback);
    }

    async function loadDocuments() {
        if (!documentsList) {
            return;
        }

        documentsStatus.className = 'small text-muted mb-2';
        documentsStatus.textContent = 'Đang tải danh sách tài liệu...';
        if (refreshButton) {
            refreshButton.disabled = true;
        }

        try {
            var response = await fetch('/api/querry/documents');
            var result = await readResponse(response);
            if (!response.ok) {
                throw new Error(getRequestError(response, result, 'Không thể tải danh sách tài liệu.'));
            }

            documentsList.replaceChildren();
            if (!result || result.length === 0) {
                var emptyRow = document.createElement('tr');
                var emptyCell = document.createElement('td');
                emptyCell.colSpan = 3;
                emptyCell.className = 'text-muted';
                emptyCell.textContent = 'Chưa có tài liệu nào được nạp.';
                emptyRow.appendChild(emptyCell);
                documentsList.appendChild(emptyRow);
            } else {
                result.forEach(function (item) {
                    var source = item.source || item.Source;
                    var count = item.chunkCount ?? item.ChunkCount ?? 0;
                    var row = document.createElement('tr');
                    var sourceCell = document.createElement('td');
                    var countCell = document.createElement('td');
                    var actionCell = document.createElement('td');
                    var deleteButton = document.createElement('button');

                    sourceCell.textContent = source;
                    countCell.textContent = count;
                    actionCell.className = 'text-end';
                    deleteButton.type = 'button';
                    deleteButton.className = 'btn btn-outline-danger btn-sm';
                    deleteButton.innerHTML = '<i class="fas fa-trash me-1"></i>Xóa';
                    deleteButton.addEventListener('click', function () {
                        deleteDocument(source, deleteButton);
                    });

                    actionCell.appendChild(deleteButton);
                    row.append(sourceCell, countCell, actionCell);
                    documentsList.appendChild(row);
                });
            }

            documentsStatus.textContent = '';
        } catch (error) {
            documentsStatus.className = 'alert alert-danger py-2 mb-2';
            documentsStatus.textContent = error.message || 'Không thể tải danh sách tài liệu.';
        } finally {
            if (refreshButton) {
                refreshButton.disabled = false;
            }
        }
    }

    async function deleteDocument(source, button) {
        if (!window.confirm('Xóa "' + source + '" khỏi kho dữ liệu chatbot?')) {
            return;
        }

        button.disabled = true;
        documentsStatus.className = 'small text-muted mb-2';
        documentsStatus.textContent = 'Đang xóa tài liệu...';

        try {
            var token = form.querySelector('input[name="__RequestVerificationToken"]').value;
            var response = await fetch('/api/querry/documents?source=' + encodeURIComponent(source), {
                method: 'DELETE',
                headers: { RequestVerificationToken: token }
            });
            var result = await readResponse(response);
            if (!response.ok) {
                throw new Error(getRequestError(response, result, 'Không thể xóa tài liệu.'));
            }

            await loadDocuments();
            documentsStatus.className = 'alert alert-success py-2 mb-2';
            documentsStatus.textContent = 'Đã xóa tài liệu "' + source + '".';
        } catch (error) {
            button.disabled = false;
            documentsStatus.className = 'alert alert-danger py-2 mb-2';
            documentsStatus.textContent = error.message || 'Không thể xóa tài liệu.';
        }
    }

    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        if (!fileInput.files.length || submitButton.disabled) {
            return;
        }

        var formData = new FormData(form);
        var token = form.querySelector('input[name="__RequestVerificationToken"]').value;
        submitButton.disabled = true;
        status.className = 'text-muted';
        status.textContent = 'Đang tải file lên hàng đợi...';
        var acceptedJobId = null;

        try {
            var response = await fetch('/api/querry/upload', {
                method: 'POST',
                headers: { RequestVerificationToken: token },
                body: formData
            });
            var result = await readResponse(response);

            if (!response.ok) {
                var message = getRequestError(response, result, 'Không thể nạp tài liệu.');
                var skipped = result && (result.skippedFiles || result.SkippedFiles) || [];
                if (skipped.length) {
                    message += ' ' + skipped.map(function (file) {
                        return (file.file || file.File) + ': ' + (file.reason || file.Reason);
                    }).join(' | ');
                }
                throw new Error(message);
            }

            var jobId = result && (result.jobId || result.JobId);
            var statusUrl = result && (result.statusUrl || result.StatusUrl) ||
                '/api/querry/upload/' + encodeURIComponent(jobId || '');
            if (!jobId) {
                throw new Error('File đã được gửi nhưng server không trả về mã theo dõi job.');
            }
            acceptedJobId = jobId;
            form.reset();
            status.className = 'alert alert-info mb-0';

            var job;
            var pollDelayMs = 1500;
            var pollingStartedAt = Date.now();
            var pollingTimeoutMs = 130000;
            do {
                status.textContent = 'Job ' + jobId + ' đã vào hàng đợi. Đang xử lý ' +
                    (result.files || result.Files) + ' file...';
                await new Promise(function (resolve) { window.setTimeout(resolve, pollDelayMs); });
                var statusResponse = await fetch(statusUrl);
                job = await readResponse(statusResponse);
                if (!statusResponse.ok) {
                    throw new Error(getRequestError(statusResponse, job, 'Không thể lấy trạng thái upload.'));
                }

                var completedFiles = job.completedFiles ?? job.CompletedFiles ?? 0;
                var totalFiles = job.totalFiles ?? job.TotalFiles ?? 0;
                var jobStatus = job.status || job.Status;
                var jobStage = job.stage || job.Stage;
                status.textContent = 'Đang xử lý tài liệu: ' + completedFiles + '/' + totalFiles +
                    ' file' + (jobStage ? ' (' + jobStage + ')' : '') + '.';
                if (jobStatus === 'Failed' || jobStatus === 'TimedOut' || jobStatus === 'Cancelled') {
                    throw new Error(job.error || job.Error || ('Job kết thúc với trạng thái ' + jobStatus + '.'));
                }
                if (Date.now() - pollingStartedAt >= pollingTimeoutMs) {
                    throw new Error('Hết thời gian theo dõi job. Hãy kiểm tra lại bằng JobId: ' + jobId);
                }
                pollDelayMs = Math.min(pollDelayMs + 1000, 5000);
            } while (jobStatus === 'Queued' || jobStatus === 'Processing');
            if (jobStatus !== 'Completed') {
                throw new Error('Trạng thái xử lý upload không hợp lệ: ' + jobStatus);
            }

            var skippedFiles = job.skippedFiles || job.SkippedFiles || [];
            var summary = 'Đã nạp/cập nhật ' +
                (job.ingestedChunks ?? job.IngestedChunks ?? 0) +
                ' đoạn từ ' + (job.completedFiles ?? job.CompletedFiles ?? 0) + ' file.';
            if (skippedFiles.length) {
                summary += ' Bỏ qua: ' + skippedFiles.map(function (file) {
                    return (file.file || file.File) + ' (' + (file.reason || file.Reason) + ')';
                }).join('; ');
            }

            status.className = 'alert alert-success mb-0';
            status.textContent = summary;
            await loadDocuments();
        } catch (error) {
            status.className = 'alert alert-danger mb-0';
            status.textContent = error instanceof TypeError
                ? (acceptedJobId
                    ? 'Mất kết nối khi theo dõi job ' + acceptedJobId + '. File đã được lưu; hãy tải lại trạng thái sau.'
                    : 'Mất kết nối khi tải file. File vẫn được giữ trong lựa chọn; hãy kiểm tra kết nối rồi thử lại.')
                : error.message || 'Lỗi khi nạp tài liệu.';
        } finally {
            submitButton.disabled = false;
        }
    });

    if (refreshButton) {
        refreshButton.addEventListener('click', loadDocuments);
    }
    loadDocuments();
});
