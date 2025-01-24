using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Throb.Service.Interfaces;
using System;
using System.Linq;

namespace Throb.Service.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;

        public CourseService(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public void Add(Course course)
        {
            // التحقق إذا كانت الدورة بنفس الاسم موجودة مسبقًا
            var existingCourse = _courseRepository.GetAll()
                                                   .FirstOrDefault(c => c.Name.Equals(course.Name, StringComparison.OrdinalIgnoreCase));

            if (existingCourse != null)
            {
                // إذا كانت الدورة موجودة، ألقِ استثناء مع رسالة الخطأ
                throw new InvalidOperationException($"دورة بنفس الاسم '{course.Name}' موجودة بالفعل.");
            }

            // إذا لم توجد دورة بنفس الاسم، تابع إضافة الدورة الجديدة
            var mappedCourse = new Course
            {
                Name = course.Name,
                Description = course.Description,
                StartDate = course.StartDate,
                EndDate = course.EndDate,
                CreatedAt = DateTime.Now // تعيين تاريخ الإنشاء بشكل لحظي
            };

            _courseRepository.Add(mappedCourse);
        }

        public void Delete(Course course)
        {
            _courseRepository.Delete(course);
        }

        public IEnumerable<Course> GetAll()
        {
            var courses = _courseRepository.GetAll().ToList();
            return courses;
        }

        public Course GetById(int? id)
        {
            if (id is null)
                return null;

            var course = _courseRepository.GetById(id.Value);

            if (course is null)
                return null;

            return course;
        }

        public void Update(Course course)
        {
            _courseRepository.Update(course);
        }
      
    }
}
