using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows;

namespace DataGridNamespace.Services
{
    public class CloudStorageService
    {
        private readonly string _bucketName = AppConfig.StorageBucket;
        private readonly HttpClient _httpClient;

        public CloudStorageService()
        {
            try
            {
                _httpClient = new HttpClient();
                Debug.WriteLine("CloudStorageService initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing CloudStorageService: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a signed URL for reading an object from Cloud Storage via Cloud Function
        /// </summary>
        /// <param name="objectName">The name of the object in Cloud Storage</param>
        /// <returns>A signed URL that can be used to access the object</returns>
        public async Task<string> GetSignedReadUrl(string objectName)
        {
            try
            {
                if (string.IsNullOrEmpty(objectName))
                {
                    Debug.WriteLine("GetSignedReadUrl: objectName is null or empty");
                    return null;
                }

                Debug.WriteLine($"Getting signed URL for object: {objectName}");

                // Set up the request
                var request = new HttpRequestMessage(HttpMethod.Post, AppConfig.GenerateReadUrlEndpoint);
                
                // Add Firebase token if available
                if (!string.IsNullOrEmpty(Session.CurrentUserToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.CurrentUserToken);
                }
                
                // Create request body
                var requestBody = new
                {
                    objectName = objectName,
                    bucketName = _bucketName
                };
                
                var jsonContent = JsonConvert.SerializeObject(requestBody);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(responseContent);
                    var signedUrl = responseObject["signedUrl"].ToString();
                    
                    Debug.WriteLine($"Generated signed URL: {signedUrl}");
                    return signedUrl;
                }
                else
                {
                    Debug.WriteLine($"Error getting signed read URL: {response.StatusCode}");
                    MessageBox.Show($"Error getting file access: {response.StatusCode}", 
                        "File Access Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting signed URL: {ex.Message}");
                MessageBox.Show($"Error getting file access: {ex.Message}", 
                    "File Access Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Gets a signed URL for uploading an object to Cloud Storage via Cloud Function
        /// </summary>
        /// <param name="objectName">The name to give the object in Cloud Storage</param>
        /// <returns>A signed URL that can be used to upload the object</returns>
        public async Task<string> GetSignedUploadUrl(string objectName, string contentType = "application/octet-stream")
        {
            try
            {
                if (string.IsNullOrEmpty(objectName))
                {
                    Debug.WriteLine("GetSignedUploadUrl: objectName is null or empty");
                    return null;
                }

                Debug.WriteLine($"Getting signed upload URL for object: {objectName}");

                // Set up the request
                var request = new HttpRequestMessage(HttpMethod.Post, AppConfig.GenerateUploadUrlEndpoint);
                
                // Add Firebase token if available
                if (!string.IsNullOrEmpty(Session.CurrentUserToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.CurrentUserToken);
                }
                
                // Create request body
                var requestBody = new
                {
                    objectName = objectName,
                    bucketName = _bucketName,
                    contentType = contentType
                };
                
                var jsonContent = JsonConvert.SerializeObject(requestBody);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(responseContent);
                    var signedUrl = responseObject["signedUrl"].ToString();
                    
                    Debug.WriteLine($"Generated signed upload URL: {signedUrl}");
                    return signedUrl;
                }
                else
                {
                    Debug.WriteLine($"Error getting signed upload URL: {response.StatusCode}");
                    MessageBox.Show($"Error getting upload access: {response.StatusCode}", 
                        "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting signed upload URL: {ex.Message}");
                MessageBox.Show($"Error getting upload access: {ex.Message}", 
                    "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Uploads a file to Cloud Storage via signed URL
        /// </summary>
        /// <param name="localFilePath">Path to the local file</param>
        /// <param name="objectName">Name to use for the object in Cloud Storage</param>
        /// <returns>The name of the uploaded object or null if upload failed</returns>
        public async Task<string> UploadFileViaSignedUrl(string localFilePath, string objectName)
        {
            try
            {
                if (string.IsNullOrEmpty(localFilePath) || string.IsNullOrEmpty(objectName))
                {
                    Debug.WriteLine("UploadFileViaSignedUrl: localFilePath or objectName is null or empty");
                    return null;
                }

                // Determine content type based on file extension
                string contentType = MimeMapping.GetMimeType(Path.GetExtension(localFilePath));
                
                // Get a signed URL for uploading
                string signedUrl = await GetSignedUploadUrl(objectName, contentType);
                if (string.IsNullOrEmpty(signedUrl))
                {
                    Debug.WriteLine("Failed to get signed upload URL");
                    return null;
                }

                // Read the file
                byte[] fileData = File.ReadAllBytes(localFilePath);
                
                // Upload to signed URL
                using (var content = new ByteArrayContent(fileData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    
                    // Use PutAsync to the upload URL
                    var response = await _httpClient.PutAsync(signedUrl, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("Upload to signed URL successful");
                        return objectName; // Return the object name for storage in database
                    }
                    else
                    {
                        Debug.WriteLine($"Upload failed with status code: {response.StatusCode}");
                        MessageBox.Show($"Upload failed: {response.StatusCode}", 
                            "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error uploading to signed URL: {ex.Message}");
                MessageBox.Show($"Upload error: {ex.Message}", 
                    "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        
        /// <summary>
        /// Uploads a profile picture for a user
        /// </summary>
        /// <param name="localFilePath">Path to the local file</param>
        /// <param name="userId">User's Firebase ID to use in the object name</param>
        /// <returns>The name of the uploaded object or null if upload failed</returns>
        public async Task<string> UploadProfilePictureAsync(string localFilePath, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Debug.WriteLine("UploadProfilePictureAsync: userId is null or empty");
                return null;
            }
            
            string extension = Path.GetExtension(localFilePath);
            string objectName = $"profile_pics/{userId}{extension}";
            
            return await UploadFileViaSignedUrl(localFilePath, objectName);
        }
    }
    
    /// <summary>
    /// Helper class to map file extensions to MIME types
    /// </summary>
    public static class MimeMapping
    {
        private static readonly Dictionary<string, string> _mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".png", "image/png"},
            {".gif", "image/gif"},
            {".bmp", "image/bmp"},
            {".pdf", "application/pdf"},
            {".doc", "application/msword"},
            {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {".xls", "application/vnd.ms-excel"},
            {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            {".ppt", "application/vnd.ms-powerpoint"},
            {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
            {".txt", "text/plain"},
            {".zip", "application/zip"},
            {".rar", "application/x-rar-compressed"},
            {".exe", "application/octet-stream"},
            {".mp3", "audio/mpeg"},
            {".mp4", "video/mp4"},
            {".avi", "video/x-msvideo"},
            {".wmv", "video/x-ms-wmv"},
            {".html", "text/html"},
            {".htm", "text/html"},
            {".css", "text/css"},
            {".js", "application/javascript"}
        };
        
        public static string GetMimeType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";
                
            if (!extension.StartsWith("."))
                extension = "." + extension;
                
            if (_mappings.TryGetValue(extension, out string mime))
                return mime;
                
            return "application/octet-stream"; // Default
        }
    }
}