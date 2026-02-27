using AUIR_Software.Models;
using ApplicationDbContext.Data;
using Microsoft.EntityFrameworkCore;

namespace AUIR_Software.Repositories
{
    public class ContentRepository
    {
        private readonly ApplicationDbContext.Data.ApplicationDbContext _context;

        public ContentRepository(ApplicationDbContext.Data.ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<Content>> GetAll()
        {
            return await _context.Contents.ToListAsync();
        }

        public async Task<Content?> GetById(int id)
        {
            return await _context.Contents.FindAsync(id);
        }

        public async Task<bool> Update(Content content)
        {
            try
            {
                if (content == null) return false;

                _context.Contents.Update(content);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> Add(Content content)
        {
            try
            {
                await _context.Contents.AddAsync(content);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> Delete(Content content)
        {
            try
            {
                _context.Contents.Remove(content);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


    }
}