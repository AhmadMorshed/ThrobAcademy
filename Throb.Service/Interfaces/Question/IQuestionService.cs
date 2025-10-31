using System.Threading.Tasks;
using Throb.Data.Entities;

namespace Throb.Service.Interfaces
{
    public interface IQuestionService
    {
        public Task<string> GenerateAndStoreQuestionsAsync(string transcript, string type);
        //Task<string> GenerateAndStoreQuestionsAsync(string text, string type, string videoTitle, IEnumerable<int> courseIds);

        public Task<List<Question>> GetAllQuestionsAsync();
    }
}