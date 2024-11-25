using Microsoft.AspNetCore.Mvc;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;

namespace ThropAcademy.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IStudentCourseRepository _studentCourseRepository; 

        public StudentController(IStudentRepository studentRepository, ICourseRepository courseRepository, IStudentCourseRepository studentCourseRepository)
        {
            _studentRepository = studentRepository;
            _courseRepository = courseRepository;
            _studentCourseRepository = studentCourseRepository;
        }

        public IActionResult Index()
        {
            var students = _studentRepository.GetAll();
            return View(students);
        }

        
        public IActionResult Create()
        {
            var courses = _courseRepository.GetAll();
            ViewBag.Courses = new SelectList(courses, "Id", "Name");
            return View();
        }

        
        [HttpPost]
        public IActionResult Create(Student student, int[] selectedCourses) 
        {
            try
            {
                if (ModelState.IsValid)
                {
                    
                    student.CreateAt = DateTime.Now;

                   
                    _studentRepository.Add(student);

                    
                    if (selectedCourses != null && selectedCourses.Any())
                    {
                        foreach (var courseId in selectedCourses)
                        {
                            var studentCourse = new StudentCourse
                            {
                                StudentId = student.Id,
                                CourseId = courseId
                            };
                            _studentCourseRepository.Add(studentCourse); 
                        }
                    }

                    
                    return RedirectToAction(nameof(Index));
                }

                
                var courses = _courseRepository.GetAll();
                ViewBag.Courses = new SelectList(courses, "Id", "Name");
                return View(student);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("CourseError", ex.Message);

                
                var courses = _courseRepository.GetAll();
                ViewBag.Courses = new SelectList(courses, "Id", "Name");
                return View(student);
            }
        }
    }
}
