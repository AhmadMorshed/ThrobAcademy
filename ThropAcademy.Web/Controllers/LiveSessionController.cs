using Microsoft.AspNetCore.Mvc;
using ThropAcademy.Web.Models;
using Throb.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ThropAcademy.Web.Controllers
{
    //[Authorize(Roles = "Student")]

    public class LiveSessionController : Controller
    {
        private readonly ILiveSession _liveSession;
        private readonly ICourseService _courseService;

        public LiveSessionController(ILiveSession liveSession, ICourseService courseService)
        {
            _liveSession = liveSession;
            _courseService = courseService;
        }

        // عرض الصفحة الرئيسية مع الجلسات والكورسات
        //[Authorize(Roles = "Student")]
        public IActionResult Index()
        {
            // جلب جميع الجلسات الحية والكورسات
            var liveSessions = _liveSession.GetAll();
            var courses = _courseService.GetAll();

            var model = new LiveSessionViewModel
            {
                LiveSessions = liveSessions,
                Courses = courses
            };

            return View(model);
        }

        // دالة لمعالجة التحديثات
        [HttpPost]
        public IActionResult UpdateLinks(LiveSessionViewModel model)
        {
            // تحقق من أن الروابط غير فارغة
            if (!string.IsNullOrEmpty(model.Link) || !string.IsNullOrEmpty(model.Link))
            {
                // هنا يمكنك إضافة الكود لتخزين الروابط في قاعدة البيانات
                // على سبيل المثال:
                // var session = _liveSession.GetById(sessionId); // إذا كان لديك الـ session ID
                // session.DiscordLink = model.DiscordLink;
                // session.VConnectLink = model.VConnectLink;
                // _liveSession.Update(session);

                // يمكنك إضافة المنطق لحفظ الروابط إذا كان لديك طريقة لحفظ الجلسات
                // افترض هنا أنك تقوم بتحديث الجلسات الموجودة بناءً على الـ ID أو على حسب البيانات.

                // إعادة التوجيه إلى الصفحة الرئيسية بعد التحديث
                return RedirectToAction("Index");
            }

            // في حالة وجود خطأ، أعِد تحميل الصفحة مع نموذج محدث
            return View("Index", model);
        }
    }
}
