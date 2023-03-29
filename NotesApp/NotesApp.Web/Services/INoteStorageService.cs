using NotesApp.Web.Models;

namespace NotesApp.Web.Services
{
    public interface INoteStorageService
    {
        Task<Note> GetNote(string username, Guid noteId);
        Task SaveNote(Note note);
        Task DeleteNote(string username, Guid noteId);
        Task<List<NoteSummary>> GetNoteList(string username);
    }
}
