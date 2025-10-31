using System.ComponentModel.DataAnnotations;

namespace ThropAcademy.Web.Models
{
    public class UploadVideoInputModel
    {
        public string Title { get; set; }
        public IFormFile VideoFile { get; set; }
        [Required(ErrorMessage = "الرجاء اختيار كورس واحد على الأقل.")]
        public int[] CourseIds { get; set; }
    }
}
