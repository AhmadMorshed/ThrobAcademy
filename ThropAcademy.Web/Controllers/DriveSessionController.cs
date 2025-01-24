using Microsoft.AspNetCore.Mvc;
using Throb.Service.Interfaces;
using Throb.Data.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ThropAcademy.Web.Controllers
{
    public class DriveSessionController : Controller
    {
        private readonly ICourseService _courseService;

        // Constructor to inject the ICourseService
        public DriveSessionController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        //[Authorize(Roles = "Student")]
        //[Authorize(Roles = "Instructor")]

        public IActionResult Index()
        {
            // الحصول على جميع الكورسات
            var courses = _courseService.GetAll();

            // تمرير قائمة الكورسات إلى الـ View
            return View(courses);
        }
    }
}
