using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Throb.Data.Entities;
using Throb.Service.Interfaces;
using ThropAcademy.Web.Models;

namespace ThropAcademy.Web.Controllers
{
    [Authorize(Roles = "Admin,Instructor,Student")]
    public class DriveSessionController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly IDriveSessionService _driveSessionService;
        private readonly ILogger<DriveSessionController> _logger;

        public DriveSessionController(ICourseService courseService, IDriveSessionService driveSessionService, ILogger<DriveSessionController> logger)
        {
            _courseService = courseService;
            _driveSessionService = driveSessionService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var courses = _courseService.GetAll();
            return View(courses);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult UploadVideo()
        {
            var courses = _courseService.GetAll();
            var model = new UploadVideoViewModel
            {
                Courses = courses
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> UploadVideo(UploadVideoInputModel inputModel)
        {
            var model = new UploadVideoViewModel
            {
                Title = inputModel.Title,
                VideoFile = inputModel.VideoFile,
                CourseIds = inputModel.CourseIds,
                Courses = _courseService.GetAll() ?? Enumerable.Empty<Course>()
            };

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }

            if (model.VideoFile == null || model.VideoFile.Length == 0)
            {
                ModelState.AddModelError("", "الرجاء رفع ملف فيديو صالح.");
                return View(model);
            }

            if (!model.VideoFile.ContentType.StartsWith("video/"))
            {
                ModelState.AddModelError("", "يُسمح برفع ملفات الفيديو فقط.");
                return View(model);
            }

            if (model.VideoFile.Length > 100_000_000) // الحد الأقصى 100 ميجابايت
            {
                ModelState.AddModelError("", "حجم الملف يتجاوز الحد الأقصى (100 ميجابايت).");
                return View(model);
            }

            if (model.CourseIds == null || !model.CourseIds.Any())
            {
                ModelState.AddModelError("CourseIds", "الرجاء اختيار كورس واحد على الأقل.");
                return View(model);
            }

            try
            {
                await _driveSessionService.AddAsync(model.VideoFile, model.CourseIds, model.Title);
                _logger.LogInformation("Video uploaded successfully for session with title: {Title}", model.Title);
                var firstCourseId = model.CourseIds.FirstOrDefault();
                var redirectUrl = Url.Action("View", "DriveSession", new { courseId = firstCourseId });
                _logger.LogInformation("Redirecting to: {RedirectUrl}", redirectUrl);
                return RedirectToAction("View", new { courseId = firstCourseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video for session with title: {Title}", model.Title);
                ModelState.AddModelError("", $"خطأ أثناء رفع الفيديو: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> View(int courseId)
        {
            var videos = await _driveSessionService.GetByCourseId(courseId);
            ViewBag.CourseId = courseId;
            return View(videos);
        }
    }




}