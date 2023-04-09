using NotesApp.Web.Services;
using NotesApp.Web.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Configure S3Settings using config
builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("Storage"));

builder.Services.AddSingleton<INoteStorageService, S3NoteStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
