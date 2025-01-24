using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;

public class HomeController : Controller
{
    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentCourseRepository _studentCourseRepository;

    // تهيئة الـ repositories
    public HomeController(ICourseRepository courseRepository, IStudentRepository studentRepository, IStudentCourseRepository studentCourseRepository)
    {
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
        _studentCourseRepository = studentCourseRepository;
    }

    // عرض صفحة التسجيل مع الكورس المختار إذا كان موجودًا
    // عرض صفحة التسجيل مع الكورس المختار إذا كان موجودًا
    public IActionResult Register(int? courseId)
    {
        var courses = _courseRepository.GetAll();
        ViewBag.Courses = courses;  // إرسال الكورسات إلى الـ View

        // إذا تم تمرير courseId، نعينه للكورس المختار
        if (courseId.HasValue)
        {
            var selectedCourse = _courseRepository.GetAll().FirstOrDefault(c => c.Id == courseId.Value);
            ViewBag.SelectedCourse = selectedCourse;
        }

        return View();
    }


    // معالجة التسجيل عند إرسال البيانات
    [HttpPost]
    public IActionResult Register(Student student, string paymentMethod, int? courseId)
    {
        if (ModelState.IsValid)
        {
            // إضافة الطالب إلى قاعدة البيانات
            student.CreateAt = DateTime.Now;
            _studentRepository.Add(student);

            // إذا تم تحديد الكورس، نضيف الطالب إلى الكورس
            if (courseId.HasValue)
            {
                var studentCourse = new StudentCourse
                {
                    StudentId = student.Id,
                    CourseId = courseId.Value
                };
                _studentCourseRepository.Add(studentCourse); // إضافة الطالب إلى الكورس
            }

            // إضافة رسالة تأكيد إلى TempData
            TempData["RegistrationSuccess"] = "You have successfully registered for the course!";

            // إذا كانت طريقة الدفع "Card"، توجيه المستخدم إلى صفحة الدفع
            if (paymentMethod == "Card")
            {
                return RedirectToAction("Payment", new { studentId = student.Id });
            }
            // إذا كانت طريقة الدفع "Cash"، توجيه المستخدم إلى صفحة النجاح أو الصفحة الرئيسية
            else if (paymentMethod == "Cash")
            {
                TempData["PaymentStatus"] = "Registration successful, please pay on delivery.";
                return RedirectToAction("Index", "Home");
            }
        }

        // في حال وجود أخطاء في النموذج، إعادة تحميل الصفحة مع الرسائل
        return View(student);
    }


    // عرض صفحة الدفع
    public IActionResult Payment(int studentId)
    {
        // جلب بيانات الطالب
        var student = _studentRepository.GetById(studentId);
        return View(student);
    }

    // معالجة الدفع عند تقديم تفاصيل البطاقة
    [HttpPost]
    public IActionResult CompletePayment(int studentId, string CardholderName, string CardNumber, string CardSecurityCode)
    {
        // هنا يمكنك إضافة الكود للتحقق من بيانات البطاقة باستخدام مزود خدمة الدفع

        // على سبيل المثال: إذا تم الدفع بنجاح، يتم عرض رسالة تأكيد
        TempData["PaymentStatus"] = "Payment successful, you are now registered!";
        return RedirectToAction("Index", "Home");
    }

    // عرض الصفحة الرئيسية مع الرسالة
    public IActionResult Index()
    {
        // عرض الرسالة إذا كانت موجودة
        ViewBag.PaymentStatus = TempData["PaymentStatus"];

        var coursesWithInstructors = _courseRepository.GetAl()
            .Include(c => c.InstructorCourses)
            .ThenInclude(ic => ic.Instructor)
            .ToList();

        return View(coursesWithInstructors);
    }

    // منطق الانضمام للكورس
    [HttpPost]
    public IActionResult JoinCourse(int courseId)
    {
        var studentId = HttpContext.Session.GetString("StudentId");

        if (string.IsNullOrEmpty(studentId))
        {
            // إذا لم يكن المستخدم مسجل دخول، نوجهه إلى صفحة تسجيل الدخول مع حفظ الرابط للعودة إليه بعد تسجيل الدخول
            return RedirectToAction("Login", "Account", new { returnUrl = $"/Home/JoinCourse/{courseId}" });
        }

        var course = _courseRepository.GetById(courseId);

        // إضافة الطالب إلى الكورس
        var studentCourse = new StudentCourse
        {
            StudentId = int.Parse(studentId),  // افترضنا أن معرف الطالب مخزن في الجلسة
            CourseId = courseId
        };

        _studentCourseRepository.Add(studentCourse);

        // رسالة تأكيد
        TempData["RegistrationSuccess"] = $"You have successfully joined the course: {course.Name}";

        // إعادة توجيه المستخدم إلى صفحة تفاصيل الكورس
        return RedirectToAction("Details", "Course", new { id = courseId });
    }



   


    public IActionResult Privacy()
    {
        // تعيين العنوان في الـ ViewData
        ViewData["Title"] = "Privacy Policy";
        return View();
    }
}

