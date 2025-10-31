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
    public HomeController(
        ICourseRepository courseRepository,
        IStudentRepository studentRepository,
        IStudentCourseRepository studentCourseRepository)
    {
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
        _studentCourseRepository = studentCourseRepository;
    }

    // عرض صفحة التسجيل مع الكورس المختار إذا كان موجودًا
    public IActionResult Register(int? courseId)
    {
        var courses = _courseRepository.GetAll();
        ViewBag.Courses = courses;

        if (courseId.HasValue)
        {
            var selectedCourse = courses.FirstOrDefault(c => c.Id == courseId.Value);
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
            student.CreateAt = DateTime.Now;
            _studentRepository.Add(student);

            if (courseId.HasValue)
            {
                var studentCourse = new StudentCourse
                {
                    StudentId = student.Id,
                    CourseId = courseId.Value
                };
                _studentCourseRepository.Add(studentCourse);
            }

            TempData["RegistrationSuccess"] = "You have successfully registered for the course!";

            if (paymentMethod == "Card")
            {
                return RedirectToAction("Payment", new { studentId = student.Id });
            }
            else if (paymentMethod == "Cash")
            {
                TempData["PaymentStatus"] = "Registration successful, please pay on delivery.";
                return RedirectToAction("Index", "Home");
            }
        }

        return View(student);
    }

    // عرض صفحة الدفع
    public IActionResult Payment(int studentId)
    {
        var student = _studentRepository.GetById(studentId);
        return View(student);
    }

    // معالجة الدفع
    [HttpPost]
    public IActionResult CompletePayment(int studentId, string CardholderName, string CardNumber, string CardSecurityCode)
    {
        // هنا يتم التحقق من بيانات البطاقة مع مزود خدمة الدفع
        TempData["PaymentStatus"] = "Payment successful, you are now registered!";
        return RedirectToAction("Index", "Home");
    }

    // عرض الصفحة الرئيسية
    public IActionResult Index()
    {
        ViewBag.PaymentStatus = TempData["PaymentStatus"];

        var coursesWithInstructors = _courseRepository.GetAll()
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
            return RedirectToAction("Login", "Account", new { returnUrl = $"/Home/JoinCourse/{courseId}" });
        }

        var course = _courseRepository.GetById(courseId);

        var studentCourse = new StudentCourse
        {
            StudentId = int.Parse(studentId),
            CourseId = courseId
        };

        _studentCourseRepository.Add(studentCourse);

        TempData["RegistrationSuccess"] = $"You have successfully joined the course: {course.Name}";

        return RedirectToAction("Details", "Course", new { id = courseId });
    }

    public IActionResult Privacy()
    {
        ViewData["Title"] = "Privacy Policy";
        return View();
    }
}
