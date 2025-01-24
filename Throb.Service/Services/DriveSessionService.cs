using System;
using System.Collections.Generic;
using System.Linq;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Throb.Service.Interfaces;

namespace Throb.Service.Services
{
    public class DriveSessionService : IDriveSession
    {
        private readonly IDriveSessionRepository _driveSessionRepository;
        private readonly ICourseRepository _courseRepository; // ربط الكورس مع الـ DriveSession

        public DriveSessionService(IDriveSessionRepository driveSessionRepository, ICourseRepository courseRepository)
        {
            _driveSessionRepository = driveSessionRepository;
            _courseRepository = courseRepository;
        }

     

        public void Add(Data.Entities.DriveSession driveSession)
        {
            if (driveSession == null)
                throw new ArgumentNullException(nameof(driveSession));

            // إضافة الـ DriveSession إلى الـ Repository
            _driveSessionRepository.Add(driveSession);
        }

      

        public void Delete(Data.Entities.DriveSession driveSession)
        {
            if (driveSession == null)
                throw new ArgumentNullException(nameof(driveSession));

            // حذف الـ DriveSession من الـ Repository
            _driveSessionRepository.Delete(driveSession);
        }

        public void Update(Data.Entities.DriveSession driveSession)
        {
            if (driveSession == null)
                throw new ArgumentNullException(nameof(driveSession));

            // تحديث الـ DriveSession في الـ Repository
            _driveSessionRepository.Update(driveSession);
        }

        IEnumerable<Data.Entities.DriveSession> IDriveSession.GetAll()
        {
            return _driveSessionRepository.GetAll();

        }

        Data.Entities.DriveSession IDriveSession.GetById(int? id)
        {
            if (id == null)

                return null;

            // جلب الـ DriveSession بناءً على الـ ID
            return _driveSessionRepository.GetById(id.Value);
        }
    }
}
