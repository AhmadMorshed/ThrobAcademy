using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Throb.Data.Entities;

namespace Throb.Data.DbContext
{
    public class ThrobDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ThrobDbContext(DbContextOptions options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<InstructorCourse>()
                .HasKey(ic => new { ic.InstructorId, ic.CourseId });

            
            modelBuilder.Entity<InstructorCourse>()
                .HasOne(ic => ic.Instructor)
                .WithMany(i => i.InstructorCourses)  
                .HasForeignKey(ic => ic.InstructorId);

            
            modelBuilder.Entity<InstructorCourse>()
                .HasOne(ic => ic.Course)
                .WithMany(c => c.InstructorCourses)  
                .HasForeignKey(ic => ic.CourseId);





            modelBuilder.Entity<StudentCourse>()
                .HasKey(sc => new { sc.StudentId, sc.CourseId });

       
            modelBuilder.Entity<StudentCourse>()
                .HasOne(sc => sc.Student)
                .WithMany(s => s.StudentCourses)  
                .HasForeignKey(sc => sc.StudentId);

            
            modelBuilder.Entity<StudentCourse>()
                .HasOne(sc => sc.Course)
                .WithMany(c => c.StudentCourses)  
                .HasForeignKey(sc => sc.CourseId);











        }

        public DbSet<Student> Students { get; set; }
       public DbSet<Instructor> Instructors { get; set; }
    public    DbSet<Course> Courses { get;set; }
 public       DbSet<LiveSession> LiveSessions { get; set; }
     public   DbSet<DriveSession> DriveSessions { get; set; }
  public      DbSet<Video> Videos { get; set; }
      public  DbSet<Pdf> Pdfs { get; set; }
      public  DbSet<Assignment> Assignments { get; set; }
    }
}
