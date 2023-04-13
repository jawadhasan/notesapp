using NotesApp.Web.Models;

namespace NotesApp.Web.Services
{
    public interface IEventPublisher
    {
        void PublishEvent(Event eventData);
    }
}