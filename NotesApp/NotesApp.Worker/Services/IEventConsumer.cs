namespace NotesApp.Worker.Services
{
    public interface IEventConsumer
    {
        void Start();
        void Stop();
    }
}