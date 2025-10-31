using Microsoft.EntityFrameworkCore;
using Throb.Data.DbContext;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Throb.Repository.Repositories
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly ThrobDbContext _context;

        public QuestionRepository(ThrobDbContext context)
        {
            _context = context;
        }

        public async Task<Question> GetByIdAsync(int id)
        {
            return await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.QuestionId == id);
        }

        public async Task<List<Question>> GetAllAsync()
        {
            return await _context.Questions
                .Include(q => q.Options)
                .ToListAsync();
        }

        public async Task<List<Question>> GetQuestionsByTranscriptAsync(string transcript)
        {
            return await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.QuestionText.Contains(transcript))
                .ToListAsync();
        }

        public async Task<List<Question>> GetQuestionsByDifficultyAsync(string difficulty)
        {
            return await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.Difficulty == difficulty)
                .ToListAsync();
        }

        public async Task AddAsync(Question question)
        {
            await _context.Questions.AddAsync(question);
            await SaveChangesAsync();
        }

        public async Task UpdateAsync(Question question)
        {
            _context.Questions.Update(question);
            await SaveChangesAsync();
        }

        public async Task DeleteAsync(Question question)
        {
            _context.Questions.Remove(question);
            await SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}