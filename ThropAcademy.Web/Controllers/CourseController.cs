using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Throb.Service.Interfaces;

namespace ThropAcademy.Web.Controllers
{
    public class CourseController : Controller
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        public IActionResult Index()
        {
            var course = _courseService.GetAll();   
            return View(course);
        }
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Create(Course course)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _courseService.Add(course); 
                    return RedirectToAction(nameof(Index)); 
                }
                return View(course); 
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("CourseError", ex.Message); 
                return View(course); 
            }
        }



    }
}

