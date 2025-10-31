using Microsoft.AspNetCore.Http;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Throb.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Throb.Service.Services
{
    public class DriveSessionService : IDriveSessionService
    {
        private readonly IDriveSessionRepository _driveSessionRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/videos");
        private readonly ILogger<DriveSessionService> _logger;

        public DriveSessionService(IDriveSessionRepository driveSessionRepository, ICourseRepository courseRepository, ILogger<DriveSessionService> logger)
        {
            _driveSessionRepository = driveSessionRepository;
            _courseRepository = courseRepository;
            _logger = logger;
        }

        public async Task AddAsync(IFormFile file, int[] courseIds, string title)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            if (courseIds == null || !courseIds.Any())
                throw new ArgumentException("At least one course ID is required.");

            var courses = await _courseRepository.GetAll()
                .Where(c => courseIds.Contains(c.Id))
                .ToListAsync();
            if (!courses.Any())
                throw new ArgumentException("Invalid Course IDs.");

            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
                _logger.LogInformation("Created storage directory: {StoragePath}", _storagePath);
            }

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(_storagePath, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation("File saved successfully at: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file at: {FilePath}", filePath);
                throw;
            }

            var driveSession = new DriveSession
            {
                Title = title,
                UploadDate = DateTime.UtcNow,
                Content_Type = file.ContentType,
                FilePath = $"/videos/{fileName}",
                Courses = courses
            };

            try
            {
                _driveSessionRepository.Add(driveSession);
                _logger.LogInformation("DriveSession added to database with ID: {Id}", driveSession.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add DriveSession to database with title: {Title}", title);
                throw;
            }
        }

        public void Delete(DriveSession driveSession)
        {
            if (driveSession == null)
                throw new ArgumentNullException(nameof(driveSession));

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", driveSession.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            _driveSessionRepository.Delete(driveSession);
        }

        public IEnumerable<DriveSession> GetAll()
        {
            return _driveSessionRepository.GetAll();
        }

        public DriveSession? GetById(int? id)
        {
            if (id == null)
                return null;

            return _driveSessionRepository.GetById(id.Value);
        }

        public async Task<IEnumerable<DriveSession>> GetByCourseId(int courseId)
        {
            return await _driveSessionRepository.GetByCourseIdAsync(courseId);
        }

        public void Update(DriveSession driveSession)
        {
            if (driveSession == null)
                throw new ArgumentNullException(nameof(driveSession));

            _driveSessionRepository.Update(driveSession);
        }
    }
}