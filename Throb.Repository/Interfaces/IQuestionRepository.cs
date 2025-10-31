using Throb.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Throb.Repository.Interfaces
{
    public interface IQuestionRepository
    {
        Task<Question> GetByIdAsync(int id);
        Task<List<Question>> GetAllAsync();
        Task<List<Question>> GetQuestionsByTranscriptAsync(string transcript);
        Task<List<Question>> GetQuestionsByDifficultyAsync(string difficulty); // دالة جديدة
        Task AddAsync(Question question);
        Task UpdateAsync(Question question);
        Task DeleteAsync(Question question);
        Task<int> SaveChangesAsync();
    }
}