using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TransferPiwigoToDigikam.Models;

namespace TransferPiwigoToDigikam.Services
{
    public class DigiKamExporter
    {
        private readonly string _outputDirectory;
        private readonly string _databasePath;

        public DigiKamExporter(string outputDirectory)
        {
            _outputDirectory = outputDirectory;
            _databasePath = Path.Combine(_outputDirectory, "digikam4.db");

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        public void InitializeDatabase()
        {
            if (!File.Exists(_databasePath))
            {
                SQLiteConnection.CreateFile(_databasePath);
            }

            using (var connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;"))
            {
                connection.Open();

                // Create Albums table
                var createAlbumsTable = @"
                    CREATE TABLE IF NOT EXISTS Albums (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        albumRoot INTEGER,
                        relativePath TEXT NOT NULL,
                        date DATE,
                        caption TEXT,
                        collection TEXT,
                        icon INTEGER
                    );";

                // Create Images table
                var createImagesTable = @"
                    CREATE TABLE IF NOT EXISTS Images (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        album INTEGER,
                        name TEXT NOT NULL,
                        status INTEGER,
                        category INTEGER,
                        modificationDate DATETIME,
                        fileSize INTEGER,
                        uniqueHash TEXT
                    );";

                // Create ImageInformation table
                var createImageInfoTable = @"
                    CREATE TABLE IF NOT EXISTS ImageInformation (
                        imageid INTEGER PRIMARY KEY,
                        rating INTEGER,
                        creationDate DATETIME,
                        digitizationDate DATETIME,
                        orientation INTEGER,
                        width INTEGER,
                        height INTEGER,
                        format TEXT,
                        colorDepth INTEGER,
                        colorModel INTEGER
                    );";

                // Create Tags table
                var createTagsTable = @"
                    CREATE TABLE IF NOT EXISTS Tags (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        pid INTEGER,
                        name TEXT NOT NULL,
                        icon INTEGER,
                        iconkde TEXT
                    );";

                // Create ImageTags table
                var createImageTagsTable = @"
                    CREATE TABLE IF NOT EXISTS ImageTags (
                        imageid INTEGER,
                        tagid INTEGER,
                        PRIMARY KEY (imageid, tagid)
                    );";

                // Create ImageComments table
                var createImageCommentsTable = @"
                    CREATE TABLE IF NOT EXISTS ImageComments (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        imageid INTEGER,
                        type INTEGER,
                        language TEXT,
                        author TEXT,
                        date DATETIME,
                        comment TEXT
                    );";

                // Create AlbumRoots table
                var createAlbumRootsTable = @"
                    CREATE TABLE IF NOT EXISTS AlbumRoots (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        label TEXT,
                        status INTEGER,
                        type INTEGER,
                        identifier TEXT,
                        specificPath TEXT
                    );";

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = createAlbumsTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createImagesTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createImageInfoTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createTagsTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createImageTagsTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createImageCommentsTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createAlbumRootsTable;
                    command.ExecuteNonQuery();

                    // Insert default album root if not exists
                    command.CommandText = "SELECT COUNT(*) FROM AlbumRoots";
                    var count = Convert.ToInt32(command.ExecuteScalar());

                    if (count == 0)
                    {
                        command.CommandText = @"INSERT INTO AlbumRoots (label, status, type, identifier, specificPath) 
                                               VALUES ('Piwigo Import', 0, 1, 'volumeid:?uuid=' || lower(hex(randomblob(16))), '/')";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public string SaveImage(PiwigoImage image, byte[] imageData, string categoryPath)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
            }

            try
            {
                // Clean category path - remove invalid characters
                var relativePath = SanitizePath(categoryPath.Replace(" / ", "/").Replace("/", Path.DirectorySeparatorChar.ToString()));
                var targetDir = Path.Combine(_outputDirectory, relativePath);

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Determine file name and sanitize it
                var fileName = !string.IsNullOrEmpty(image.File) ? image.File : $"image_{image.Id}.jpg";
                fileName = SanitizeFileName(fileName);

                // Handle file name conflicts
                var filePath = Path.Combine(targetDir, fileName);
                filePath = GetUniqueFilePath(filePath);

                // Save image file
                File.WriteAllBytes(filePath, imageData);

                // Add to database
                AddImageToDatabase(image, Path.GetFileName(filePath), relativePath, imageData);

                return filePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save image {image.Id}: {ex.Message}", ex);
            }
        }

        private string SanitizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "Uncategorized";
            }

            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in invalidChars)
            {
                path = path.Replace(c, '_');
            }

            // Also replace some commonly problematic characters
            path = path.Replace(":", "_").Replace("*", "_").Replace("?", "_")
                       .Replace("\"", "_").Replace("<", "_").Replace(">", "_")
                       .Replace("|", "_");

            return path;
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "unnamed.jpg";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            // Ensure file has an extension
            if (!Path.HasExtension(fileName))
            {
                fileName += ".jpg";
            }

            // Trim and ensure it's not too long
            if (fileName.Length > 200)
            {
                var ext = Path.GetExtension(fileName);
                fileName = fileName.Substring(0, 200 - ext.Length) + ext;
            }

            return fileName;
        }

        private string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return filePath;
            }

            var directory = Path.GetDirectoryName(filePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            int counter = 1;
            string newFilePath;

            do
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}");
                counter++;
            } while (File.Exists(newFilePath) && counter < 1000);

            if (counter >= 1000)
            {
                throw new Exception($"Too many file name conflicts for: {filePath}");
            }

            return newFilePath;
        }

        private void AddImageToDatabase(PiwigoImage image, string fileName, string relativePath, byte[] imageData)
        {
            using (var connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;"))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get or create album
                        var albumId = GetOrCreateAlbum(connection, relativePath);

                        // Calculate unique hash
                        var uniqueHash = CalculateHash(imageData);

                        // Insert image
                        var insertImage = @"INSERT INTO Images (album, name, status, modificationDate, fileSize, uniqueHash) 
                                           VALUES (@album, @name, 1, @modDate, @fileSize, @uniqueHash)";

                        long imageId;
                        using (var cmd = new SQLiteCommand(insertImage, connection))
                        {
                            cmd.Parameters.AddWithValue("@album", albumId);
                            cmd.Parameters.AddWithValue("@name", fileName);
                            cmd.Parameters.AddWithValue("@modDate", DateTime.Now);
                            cmd.Parameters.AddWithValue("@fileSize", imageData.Length);
                            cmd.Parameters.AddWithValue("@uniqueHash", uniqueHash);
                            cmd.ExecuteNonQuery();

                            imageId = connection.LastInsertRowId;
                        }

                        // Insert image information
                        var insertImageInfo = @"INSERT INTO ImageInformation (imageid, creationDate, width, height, format) 
                                               VALUES (@imageid, @creationDate, @width, @height, @format)";

                        using (var cmd = new SQLiteCommand(insertImageInfo, connection))
                        {
                            cmd.Parameters.AddWithValue("@imageid", imageId);
                            cmd.Parameters.AddWithValue("@creationDate", image.DateCreation != DateTime.MinValue ? image.DateCreation : DateTime.Now);
                            cmd.Parameters.AddWithValue("@width", image.Width);
                            cmd.Parameters.AddWithValue("@height", image.Height);
                            cmd.Parameters.AddWithValue("@format", Path.GetExtension(fileName).TrimStart('.'));
                            cmd.ExecuteNonQuery();
                        }

                        // Add tags
                        foreach (var tagName in image.Tags)
                        {
                            var tagId = GetOrCreateTag(connection, tagName);
                            var insertImageTag = @"INSERT OR IGNORE INTO ImageTags (imageid, tagid) VALUES (@imageid, @tagid)";
                            using (var cmd = new SQLiteCommand(insertImageTag, connection))
                            {
                                cmd.Parameters.AddWithValue("@imageid", imageId);
                                cmd.Parameters.AddWithValue("@tagid", tagId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Add comment if exists
                        if (!string.IsNullOrEmpty(image.Comment))
                        {
                            var insertComment = @"INSERT INTO ImageComments (imageid, type, language, date, comment) 
                                                 VALUES (@imageid, 1, 'x-default', @date, @comment)";
                            using (var cmd = new SQLiteCommand(insertComment, connection))
                            {
                                cmd.Parameters.AddWithValue("@imageid", imageId);
                                cmd.Parameters.AddWithValue("@date", DateTime.Now);
                                cmd.Parameters.AddWithValue("@comment", image.Comment);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private long GetOrCreateAlbum(SQLiteConnection connection, string relativePath)
        {
            var checkAlbum = "SELECT id FROM Albums WHERE relativePath = @relativePath";
            using (var cmd = new SQLiteCommand(checkAlbum, connection))
            {
                cmd.Parameters.AddWithValue("@relativePath", "/" + relativePath.Replace("\\", "/"));
                var result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt64(result);
                }
            }

            var insertAlbum = @"INSERT INTO Albums (albumRoot, relativePath, date) 
                               VALUES (1, @relativePath, @date)";
            using (var cmd = new SQLiteCommand(insertAlbum, connection))
            {
                cmd.Parameters.AddWithValue("@relativePath", "/" + relativePath.Replace("\\", "/"));
                cmd.Parameters.AddWithValue("@date", DateTime.Now);
                cmd.ExecuteNonQuery();

                return connection.LastInsertRowId;
            }
        }

        private long GetOrCreateTag(SQLiteConnection connection, string tagName)
        {
            var checkTag = "SELECT id FROM Tags WHERE name = @name";
            using (var cmd = new SQLiteCommand(checkTag, connection))
            {
                cmd.Parameters.AddWithValue("@name", tagName);
                var result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt64(result);
                }
            }

            var insertTag = "INSERT INTO Tags (name) VALUES (@name)";
            using (var cmd = new SQLiteCommand(insertTag, connection))
            {
                cmd.Parameters.AddWithValue("@name", tagName);
                cmd.ExecuteNonQuery();

                return connection.LastInsertRowId;
            }
        }

        private string CalculateHash(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(data);
                var sb = new StringBuilder();
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
