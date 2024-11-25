using Throb.Data.DbContext;
using Throb.Data.Entities;
using System.Linq;

using Throb.Repository.Interfaces;

namespace Throb.Repository.Repositories
{
    public class DriveSessionRepository : GenericRepository<DriveSession>, IDriveSessionRepository
    {
        private readonly ThrobDbContext _context;

        public DriveSessionRepository(ThrobDbContext context) : base(context)
        {
            _context = context;
        }

        //public void Add(DriveSession driveSession)
        // =>_context.Add(driveSession);

        //public void Delete(DriveSession driveSession)
        // =>_context.Remove(driveSession);

        //public IEnumerable<DriveSession> GetAll() => _context.DriveSessions.ToList();

        //public DriveSession GetById(int id)

        //=>_context.DriveSessions.Find(id);

        //public void Update(DriveSession driveSession)
        //    =>_context.Update(driveSession);

    }
}
