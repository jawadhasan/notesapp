using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NotesApp.Web.Models;
using NotesApp.Web.Settings;

namespace NotesApp.Web.Services
{
    public class S3NoteStorageService : INoteStorageService
    {
        private readonly ILogger _logger;
        private readonly S3Settings _s3Settings;
        private readonly IAmazonS3 _s3Client;

        public S3NoteStorageService(IOptions<S3Settings> s3Settings,  ILoggerFactory loggerFactory)
        {
            
            _logger = loggerFactory.CreateLogger<S3NoteStorageService>();
            _s3Settings = s3Settings.Value;

            var region = RegionEndpoint.GetBySystemName(s3Settings.Value.AWSRegion);

            _s3Client = new AmazonS3Client(
                new BasicAWSCredentials(
                    s3Settings.Value.AWSAccessKey,
                    s3Settings.Value.AWSSecretKey),
                region);
        }

        public async Task SaveNote(Note note)
        {
            if (note == null) throw new ArgumentNullException("note");

            if (string.IsNullOrWhiteSpace(note.UserId)) throw new ArgumentException("The note provided didn't have a user ID assigned");

            Guid noteId = note.NoteId ?? Guid.NewGuid();

            string noteObjectKey = GetNoteObjectKey(note.UserId, noteId);

            try
            {
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = _s3Settings.BucketName,
                    Key = noteObjectKey,
                    StorageClass = S3StorageClass.Standard,
                    CannedACL = S3CannedACL.Private,
                    ContentType = "application/json",
                    ContentBody = JsonConvert.SerializeObject(note)
                };

                PutObjectResponse response = await _s3Client.PutObjectAsync(putRequest);

                // Save data in cache.
             

                // Update summary
                List<NoteSummary> notes = await GetNoteList(note.UserId);
                NoteSummary oldNote = notes.Where(n => n.NoteId == noteId).SingleOrDefault();

                if (notes != null)
                {
                    notes.Remove(oldNote);
                }

                notes.Add(new NoteSummary { NoteId = note.NoteId, UserId = note.UserId, Title = note.Title, CreatedAt = note.CreatedAt });

                await SaveNoteList(note.UserId, notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while saving a note.");

                throw;
            }
        }

        public async Task<Note> GetNote(string username, Guid noteId)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException("username");
            if (noteId == null) throw new ArgumentNullException("noteId");

            string noteObjectKey = GetNoteObjectKey(username, noteId);

            try
                {
                    GetObjectRequest getRequest = new GetObjectRequest
                    {
                        BucketName = _s3Settings.BucketName,
                        Key = noteObjectKey
                    };

                    GetObjectResponse response = await _s3Client.GetObjectAsync(getRequest);

                    using (StreamReader reader = new StreamReader(response.ResponseStream))
                    {
                        string responseBody = reader.ReadToEnd();

                        Note note = JsonConvert.DeserializeObject<Note>(responseBody);

                        return note;
                    }
                }
                catch (AmazonS3Exception)
                {
                    // Not found if we get an exception
                    return null;
                }
        }

        public async Task DeleteNote(string username, Guid noteId)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException("username");
            if (noteId == null) throw new ArgumentNullException("noteId");

            string noteObjectKey = GetNoteObjectKey(username, noteId);

            DeleteObjectRequest deleteRequest = new DeleteObjectRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = noteObjectKey
            };

            try
            {
                DeleteObjectResponse response = await _s3Client.DeleteObjectAsync(deleteRequest);

                // Remove data from cache.
             

                // Update summary
                List<NoteSummary> notes = await GetNoteList(username);
                NoteSummary oldNote = notes.Where(n => n.NoteId == noteId).SingleOrDefault();

                if (notes != null)
                {
                    notes.Remove(oldNote);
                }

                await SaveNoteList(username, notes);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "An error occured while deleting a note.");

                return;
            }
        }

        public async Task<List<NoteSummary>> GetNoteList(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException("username");

            string summaryObjectKey = GetSummaryObjectKey(username);

                // Key not in cache, so get data.
                GetObjectRequest getRequest = new GetObjectRequest
                {
                    BucketName = _s3Settings.BucketName,
                    Key = summaryObjectKey
                };

                try
                {
                    GetObjectResponse response = await _s3Client.GetObjectAsync(getRequest);

                    using (StreamReader reader = new StreamReader(response.ResponseStream))
                    {
                        string responseBody = reader.ReadToEnd();

                        List<NoteSummary> notes = JsonConvert.DeserializeObject<List<NoteSummary>>(responseBody);

                    
                        return notes;
                    }
                }
                catch (AmazonS3Exception)
                {
                    // Not found if we get an exception
                    return new List<NoteSummary>();
                }

        }

        private async Task SaveNoteList(string username, List<NoteSummary> notes)
        {
            if (notes == null) throw new ArgumentNullException("notes");

            if (notes.Any())
            {
                // check that notes are for a single user
                var groups = notes.GroupBy(n => n.UserId);

                if (groups.Count() > 1) throw new ArgumentException("The list of note refers to more than one user.");
            }

            string summaryObjectKey = GetSummaryObjectKey(username);

            try
            {
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = _s3Settings.BucketName,
                    Key = summaryObjectKey,
                    StorageClass = S3StorageClass.Standard,
                    CannedACL = S3CannedACL.Private,
                    ContentType = "application/json",
                    ContentBody = JsonConvert.SerializeObject(notes.OrderByDescending(n => n.CreatedAt))
                };

                PutObjectResponse response = await _s3Client.PutObjectAsync(putRequest);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while saving a note list.");

                throw;
            }
        }

        private string GetSummaryObjectKey(string username)
        {
            return $"{username}/summary.json";
        }


        private string GetNoteObjectKey(string username, Guid noteId)
        {
            return $"{username}/{noteId.ToString()}.json";
        }
    }
}
