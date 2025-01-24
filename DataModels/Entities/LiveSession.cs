namespace Throb.Data.Entities
{
    public class LiveSession
    {
        public int Id { get; set; }
        public string Title { get; set; }  // عنوان الجلسة
        public DateTime Date { get; set; }  // تاريخ الجلسة
        public int CourseId { get; set; }  // ربط الجلسة بالكورس
        public Course Course { get; set; }  // البيانات المتعلقة بالكورس
        public string DiscordLink { get; set; }  // رابط Discord
        public string VConnectLink { get; set; } // رابط vConnect
    }
}
