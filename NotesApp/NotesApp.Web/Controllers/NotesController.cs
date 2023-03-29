using Microsoft.AspNetCore.Mvc;
using NotesApp.Web.Models;
using NotesApp.Web.Services;
using EventType = NotesApp.Web.Models.EventType;

namespace NotesApp.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotesController : Controller
    {
        private INoteStorageService _noteStorageService;

        private readonly ILogger<NotesController> _logger;
        private readonly string _user = "notesappuser"; //hard-coded for now

        public NotesController(INoteStorageService noteStorageService, ILogger<NotesController> logger)
        {
            _noteStorageService = noteStorageService;
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogWarning($"Getting note list for user '{_user}'");

            var notes = await _noteStorageService.GetNoteList(_user);

            return Ok(notes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            Note note = await _noteStorageService.GetNote(_user, id);

            if (note == null)
            {
                _logger.LogWarning($"Coulnd't find note with ID '{id}' for user '{_user}'");
                return BadRequest("Not Found");
            }

            _logger.LogInformation($"Editing note with ID '{id}' for user '{_user}'");

            return Ok(note);
        }


        //add or edit

        [HttpPost("edit")]
        public async Task<IActionResult> Edit(Note note)
        {
            EventType eventType = EventType.NoteEdited;

            if (!note.NoteId.HasValue)
            {
                note.NoteId = Guid.NewGuid();
                note.CreatedAt = DateTime.UtcNow;
                eventType = EventType.NoteCreated;

                _logger.LogInformation($"Creating new note with ID '{note.NoteId.Value}' for user '{_user}'");
            }
            else
            {
                // check the owner of the note
                Note originalNote = await _noteStorageService.GetNote(_user, note.NoteId.Value);//User.Identity.Name

                if (originalNote == null)
                {
                    _logger.LogWarning($"Coulnd't find note with ID '{note.NoteId.Value}' for user '{_user}'");
                    return BadRequest("Not found");
                }

                _logger.LogInformation($"Saving changes to existing note with ID '{note.NoteId.Value}' for user '{_user}'");
            }

            // reset the user name as we were not displaying it on the page on purpose
            note.UserId = _user;

            await _noteStorageService.SaveNote(note);



            return Ok();
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            // check the owner of the note
            Note originalNote = await _noteStorageService.GetNote(_user, id);

            if (originalNote == null)
            {
                _logger.LogWarning($"Coulnd't find note with ID '{id}' for user '{User.Identity.Name}'");
                return BadRequest("Not found");
            }

            await _noteStorageService.DeleteNote(_user, id);

            _logger.LogInformation($"Deleting note with ID '{id}' for user '{_user}'");

            return Ok();
        }
        
    }
}
