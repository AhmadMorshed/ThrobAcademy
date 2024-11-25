using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Throb.Data.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ICollection<Student>? Students { get; set; }
        public ICollection<InstructorCourse>? InstructorCourses { get; set; }
        public ICollection<StudentCourse>? StudentCourses { get; set; }
        public LiveSession? LiveSession { get; set; }
        public int? LiveSessionId { get; set; }
        public DriveSession? DriveSession { get; set; }
        public int? DriveSessionId { get; set; }
    }
}
