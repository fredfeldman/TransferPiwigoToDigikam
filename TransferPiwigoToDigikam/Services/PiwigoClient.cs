using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using TransferPiwigoToDigikam.Models;

namespace TransferPiwigoToDigikam.Services
{
    public class PiwigoClient
    {
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;
        private CookieContainer _cookies;
        private bool _isLoggedIn;

        public PiwigoClient(string baseUrl, string username, string password)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _username = username;
            _password = password;
            _cookies = new CookieContainer();
            _isLoggedIn = false;
        }

        public bool Login()
        {
            try
            {
                var loginUrl = $"{_baseUrl}/ws.php?format=json";
                var postData = $"method=pwg.session.login&username={Uri.EscapeDataString(_username)}&password={Uri.EscapeDataString(_password)}";

                var response = MakeRequest(loginUrl, postData);
                var serializer = new JavaScriptSerializer();
                dynamic result = serializer.DeserializeObject(response);

                // Check if Piwigo returned an error
                if (result.ContainsKey("stat") && result["stat"] == "fail")
                {
                    var errorMessage = result.ContainsKey("message") ? result["message"].ToString() : "Unknown error";
                    throw new Exception($"Piwigo API error: {errorMessage}");
                }

                if (result.ContainsKey("stat") && result["stat"] == "ok")
                {
                    _isLoggedIn = true;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to login to Piwigo: {ex.Message}", ex);
            }
        }

        public List<PiwigoCategory> GetAllCategories()
        {
            if (!_isLoggedIn)
                throw new InvalidOperationException("Not logged in to Piwigo");

            try
            {
                var url = $"{_baseUrl}/ws.php?format=json&method=pwg.categories.getList&recursive=true&fullname=true";
                var response = MakeRequest(url, null);

                var serializer = new JavaScriptSerializer();
                dynamic result = serializer.DeserializeObject(response);

                // Check if Piwigo returned an error
                if (result.ContainsKey("stat") && result["stat"] == "fail")
                {
                    var errorMessage = result.ContainsKey("message") ? result["message"].ToString() : "Unknown error";
                    throw new Exception($"Piwigo API error: {errorMessage}");
                }

                var categories = new List<PiwigoCategory>();

                if (result.ContainsKey("result") && result["result"].ContainsKey("categories"))
                {
                    foreach (var cat in result["result"]["categories"])
                    {
                        // Skip categories with missing required data
                        if (cat["id"] == null || cat["name"] == null)
                            continue;

                        categories.Add(new PiwigoCategory
                        {
                            Id = int.Parse(cat["id"].ToString()),
                            Name = cat["name"],
                            FullPath = cat.ContainsKey("name_display") && cat["name_display"] != null 
                                ? cat["name_display"] 
                                : cat["name"],
                            ParentId = cat.ContainsKey("id_uppercat") && cat["id_uppercat"] != null 
                                ? (int?)int.Parse(cat["id_uppercat"].ToString()) 
                                : null
                        });
                    }
                }

                return categories;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get categories: {ex.Message}", ex);
            }
        }

        public List<PiwigoImage> GetImagesFromCategory(int categoryId, int page = 0, int perPage = 100)
        {
            if (!_isLoggedIn)
                throw new InvalidOperationException("Not logged in to Piwigo");

            try
            {
                var url = $"{_baseUrl}/ws.php?format=json&method=pwg.categories.getImages&cat_id={categoryId}&per_page={perPage}&page={page}";
                var response = MakeRequest(url, null);

                var serializer = new JavaScriptSerializer();
                dynamic result = serializer.DeserializeObject(response);

                // Check if Piwigo returned an error
                if (result.ContainsKey("stat") && result["stat"] == "fail")
                {
                    var errorMessage = result.ContainsKey("message") ? result["message"].ToString() : "Unknown error";
                    var errorCode = result.ContainsKey("err") ? result["err"].ToString() : "";
                    throw new Exception($"Piwigo API error: {errorMessage} (Error code: {errorCode})");
                }

                var images = new List<PiwigoImage>();

                if (result.ContainsKey("result") && result["result"].ContainsKey("images"))
                {
                    foreach (var img in result["result"]["images"])
                    {
                        // Skip images with missing required data
                        if (img["id"] == null)
                            continue;

                        var image = new PiwigoImage
                        {
                            Id = int.Parse(img["id"].ToString()),
                            Name = img.ContainsKey("name") && img["name"] != null ? img["name"] : "",
                            File = img.ContainsKey("file") && img["file"] != null ? img["file"] : "",
                            ElementUrl = img.ContainsKey("element_url") && img["element_url"] != null ? img["element_url"] : "",
                            Comment = img.ContainsKey("comment") && img["comment"] != null ? img["comment"] : "",
                            Width = img.ContainsKey("width") && img["width"] != null ? int.Parse(img["width"].ToString()) : 0,
                            Height = img.ContainsKey("height") && img["height"] != null ? int.Parse(img["height"].ToString()) : 0
                        };

                        if (img.ContainsKey("date_creation") && img["date_creation"] != null)
                        {
                            DateTime.TryParse(img["date_creation"], out DateTime dateCreation);
                            image.DateCreation = dateCreation;
                        }

                        if (img.ContainsKey("categories") && img["categories"] != null)
                        {
                            foreach (var cat in img["categories"])
                            {
                                if (cat != null)
                                {
                                    image.Categories.Add(cat.ContainsKey("name") && cat["name"] != null ? cat["name"] : "");
                                }
                            }
                        }

                        if (img.ContainsKey("tags") && img["tags"] != null)
                        {
                            foreach (var tag in img["tags"])
                            {
                                if (tag != null)
                                {
                                    image.Tags.Add(tag.ContainsKey("name") && tag["name"] != null ? tag["name"] : "");
                                }
                            }
                        }

                        images.Add(image);
                    }
                }

                return images;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get images from category {categoryId}: {ex.Message}", ex);
            }
        }

        public byte[] DownloadImage(string imageUrl)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(imageUrl);
                request.CookieContainer = _cookies;
                request.Method = "GET";

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download image from {imageUrl}: {ex.Message}", ex);
            }
        }

        public void Logout()
        {
            if (_isLoggedIn)
            {
                try
                {
                    var url = $"{_baseUrl}/ws.php?format=json&method=pwg.session.logout";
                    MakeRequest(url, null);
                }
                catch
                {
                    // Ignore logout errors
                }
                finally
                {
                    _isLoggedIn = false;
                    _cookies = new CookieContainer();
                }
            }
        }

        private string MakeRequest(string url, string postData)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = _cookies;

            if (!string.IsNullOrEmpty(postData))
            {
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                var data = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            else
            {
                request.Method = "GET";
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var responseText = reader.ReadToEnd();

                    // Validate response is not empty or just whitespace
                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        throw new Exception("Server returned empty response");
                    }

                    // Basic JSON validation - should start with { or [
                    var trimmed = responseText.TrimStart();
                    if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
                    {
                        throw new Exception($"Server returned invalid JSON response: {responseText.Substring(0, Math.Min(100, responseText.Length))}");
                    }

                    return responseText;
                }
            }
        }
    }
}
