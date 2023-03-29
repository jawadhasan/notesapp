namespace NotesApp.Web.Models
{
    public class Note
    {
        public string UserId { get; set; }
        public Guid? NoteId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
