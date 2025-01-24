using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Throb.Data.Entities;

namespace Throb.Service.Interfaces
{
    public interface IDriveSession
    {
        DriveSession GetById(int? id);
        IEnumerable<DriveSession> GetAll();
        void Add(DriveSession driveSession);
        void Update(DriveSession driveSession);
        void Delete(DriveSession driveSession);
    }
}
