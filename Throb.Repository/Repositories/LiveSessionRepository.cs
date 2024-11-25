using Throb.Data.DbContext;
using Throb.Data.Entities;
using System.Linq;

using Throb.Repository.Interfaces;

namespace Throb.Repository.Repositories
{
    public class LiveSessionRepository : GenericRepository<LiveSession>, ILiveSessionRepository
    {
        private readonly ThrobDbContext _context;

        public LiveSessionRepository(ThrobDbContext context) : base(context)
        {
            _context = context;
        }

        //public void Add(LiveSession liveSession)
        // =>_context.Add(liveSession);

        //public void Delete(LiveSession liveSession)
        // =>_context.Remove(liveSession);

        //public IEnumerable<LiveSession> GetAll() => _context.LiveSessions.ToList();

        //public LiveSession GetById(int id)

        //=>_context.LiveSessions.Find(id);

        //public void Update(LiveSession liveSession)
        //    =>_context.Update(liveSession);

    }
}
