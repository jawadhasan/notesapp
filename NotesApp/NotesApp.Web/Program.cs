using NotesApp.Web.Services;
using NotesApp.Web.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Configure S3Settings using config
builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<SQSSettings>(builder.Configuration.GetSection("Queueing"));

builder.Services.AddSingleton<INoteStorageService, S3NoteStorageService>();
builder.Services.AddSingleton<IEventPublisher, SQSEventPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
