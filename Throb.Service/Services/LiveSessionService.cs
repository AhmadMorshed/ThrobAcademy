using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Throb.Service.Interfaces;

namespace Throb.Service.Services
{
    public class LiveSessionService :ILiveSession
    {
        private readonly ILiveSessionRepository _liveSessionRepository;
        private readonly ICourseRepository _courseRepository;

        public LiveSessionService(ILiveSessionRepository liveSessionRepository, ICourseRepository courseRepository)
        {
            _liveSessionRepository = liveSessionRepository;
            _courseRepository = courseRepository;
        }

        public void Add(LiveSession liveSession)
        {
            throw new NotImplementedException();
        }

        public void Delete(LiveSession liveeSession)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<LiveSession> GetAll()
        {
            // جلب جميع الجلسات الحية مع الكورس المرتبط بها
            return _liveSessionRepository.GetAll();       }

        public LiveSession GetById(int? id)
        {
            throw new NotImplementedException();
        }

        public void Update(LiveSession liveSession)
        {
            throw new NotImplementedException();
        }

        // باقي العمليات الأخرى كما هي
    }
}
