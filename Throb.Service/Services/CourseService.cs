using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Throb.Service.Interfaces;

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
            var mappedCourse = new Course
            {
                
                Name = course.Name,
                Description = course.Description,
                StartDate = course.StartDate,
                EndDate = course.EndDate,
                
            };

            _courseRepository.Add(mappedCourse);
            
        }

        public void Delete(Course course)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Course> GetAll()
        {
            var courses=_courseRepository.GetAll();
            return courses;
        }

        public Course GetById(int id)
        {
            throw new NotImplementedException();
        }

        public void Update(Course course)
        {
            throw new NotImplementedException();
        }
    }
}
