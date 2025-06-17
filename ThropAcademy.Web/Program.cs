using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.Features;
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

            // إضافة الخدمات
            builder.Services.AddControllersWithViews();

            // الاتصال بقاعدة البيانات
            builder.Services.AddDbContext<ThrobDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // تفعيل الهوية
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<ThrobDbContext>()
            .AddDefaultTokenProviders();

            // تفعيل رفع الملفات حتى 100MB
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 104857600; // 100MB
            });
            builder.Services.AddHttpClient("Deepgram", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(10); // تمديد الوقت لتجنب انتهاء الاتصال
            });
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddEnvironmentVariables();


            // تستخدم builder.Configuration بدلًا من configuration
            //builder.Services.AddAuthentication(options =>
            //{
            //    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            //})
            //    .AddCookie()
            //    .AddGoogle(options =>
            //    {
            //        options.ClientId = builder.Configuration["Google:ClientId"];
            //        options.ClientSecret = builder.Configuration["Google:ClientSecret"];
            //        options.Scope.Add("https://www.googleapis.com/auth/drive.readonly");
            //        options.SaveTokens = true;
            //    });


            // الكوكيز
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
                options.LoginPath = "/Account/login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            // الجلسات
            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = ".Throb.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            // المستودعات والخدمات
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

            var app = builder.Build();

            // الـ middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication(); // الترتيب الصحيح
            app.UseAuthorization();
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}
