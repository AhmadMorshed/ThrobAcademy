using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;

namespace ThropAcademy.Web.Controllers
{
    public class InstructorController : Controller
    {
        private readonly IInstructorRepository _instructorRepository;
        private readonly ICourseRepository _courseRepository;

        public InstructorController(IInstructorRepository instructorRepository,ICourseRepository courseRepository)
        {
            _instructorRepository = instructorRepository;
            _courseRepository = courseRepository;
        }

        public IActionResult Index()
        {
            var courses = _instructorRepository.GetAll();
            ViewBag.Courses = new SelectList(courses, "Id", "Name");
            return View();
        }
        public IActionResult Create(Instructor instructor)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    instructor.CreateAt = DateTime.Now;
                    _instructorRepository.Add(instructor);
                    return RedirectToAction(nameof(Index));
                }
                var courses = _courseRepository.GetAll();
                ViewBag.Courses = new SelectList(courses, "Id", "Name");
                return View(instructor);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("InstructorError", ex.Message);
                var courses = _courseRepository.GetAll();
                ViewBag.Courses = new SelectList(courses, "Id", "Name");
                return View(instructor);
            }

        }

    }
}
