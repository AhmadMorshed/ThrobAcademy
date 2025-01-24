using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Throb.Data.DbContext;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Throb.Repository.Repositories;
using Throb.Service.Interfaces;
using Throb.Service.Services;

namespace ThropAcademy.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // إضافة خدمات النظام إلى الحاوية (DI Container)
            builder.Services.AddControllersWithViews();

            // تكوين الاتصال بقاعدة البيانات مع استخدام SQL Server
            builder.Services.AddDbContext<ThrobDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // إعدادات lockout
                options.Lockout.MaxFailedAccessAttempts = 10;  // عدد المحاولات الفاشلة قبل القفل
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);  // مدة القفل
            }).AddEntityFrameworkStores<ThrobDbContext>().AddDefaultTokenProviders();

            // تكوين الكوكيز مع إعدادات أفضل
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // مدة الكوكيز
                options.SlidingExpiration = true; // التحديث التلقائي للجلسة
                options.LoginPath = "/Account/login"; // مسار صفحة تسجيل الدخول
                options.LogoutPath = "/Account/Logout"; // مسار تسجيل الخروج
                options.AccessDeniedPath = "/Account/AccessDenied"; // مسار الوصول المرفوض

                // تأكد من أن الكوكيز تتعامل مع بيئة التطوير بشكل صحيح
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;  // لضمان عمل الكوكيز في بيئة التطوير
                options.Cookie.SameSite = SameSiteMode.Lax;  // تجنب مشاكل مرور الكوكيز بين المواقع
            });

            // إضافة المستودعات (Repositories)
            builder.Services.AddScoped<ICourseRepository, CourseRepository>();
            builder.Services.AddScoped<IStudentRepository, StudentRepository>();
            builder.Services.AddScoped<IInstructorRepository, InstructorRepository>();
            builder.Services.AddScoped<ILiveSessionRepository, LiveSessionRepository>();
            builder.Services.AddScoped<ICourseService, CourseService>();
            builder.Services.AddScoped<IStudentCourseRepository, StudentCourseRepository>();
            builder.Services.AddScoped<IDriveSessionRepository, DriveSessionRepository>();
            builder.Services.AddScoped<IInstructorCourseRepository, InstructorCourseRepository>();
            builder.Services.AddScoped<ILiveSession, LiveSessionService>();
            builder.Services.AddScoped<IDriveSession, DriveSessionService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // إضافة خدمة الـ Session
            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = ".Throb.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(30); // زمن انتهاء الجلسة
            });

            var app = builder.Build();

            // تكوين خط الأنابيب للطلبات HTTP
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();
            // تفعيل الـ Session في الـ Middleware
            app.UseSession();

            // تكوين Routing للـ Controller و Action الافتراضي
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // تشغيل التطبيق
            await app.RunAsync();
        }
    }
}
