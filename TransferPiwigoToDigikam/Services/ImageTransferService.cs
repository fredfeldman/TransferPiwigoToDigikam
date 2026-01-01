using System;
using System.Collections.Generic;
using System.Linq;
using TransferPiwigoToDigikam.Models;

namespace TransferPiwigoToDigikam.Services
{
    public class ImageTransferService
    {
        private readonly PiwigoClient _piwigoClient;
        private readonly DigiKamExporter _digiKamExporter;

        public event EventHandler<TransferProgressEventArgs> ProgressChanged;
        public event EventHandler<string> StatusChanged;

        public ImageTransferService(string piwigoUrl, string username, string password, string outputDirectory)
        {
            _piwigoClient = new PiwigoClient(piwigoUrl, username, password);
            _digiKamExporter = new DigiKamExporter(outputDirectory);
        }

        public void TransferAllImages()
        {
            try
            {
                OnStatusChanged("Connecting to Piwigo...");
                if (!_piwigoClient.Login())
                {
                    throw new Exception("Failed to login to Piwigo. Please check your credentials.");
                }

                OnStatusChanged("Initializing DigiKam database...");
                _digiKamExporter.InitializeDatabase();

                OnStatusChanged("Retrieving categories from Piwigo...");
                var categories = _piwigoClient.GetAllCategories();
                OnStatusChanged($"Found {categories.Count} categories");

                var allImages = new List<Tuple<PiwigoImage, string>>();

                // Collect all images from all categories
                foreach (var category in categories)
                {
                    OnStatusChanged($"Retrieving images from category: {category.FullPath}");
                    
                    var page = 0;
                    var hasMoreImages = true;

                    while (hasMoreImages)
                    {
                        var images = _piwigoClient.GetImagesFromCategory(category.Id, page, 100);
                        
                        if (images.Count == 0)
                        {
                            hasMoreImages = false;
                        }
                        else
                        {
                            foreach (var image in images)
                            {
                                allImages.Add(new Tuple<PiwigoImage, string>(image, category.FullPath));
                            }
                            page++;
                        }
                    }
                }

                OnStatusChanged($"Total images to transfer: {allImages.Count}");

                // Transfer all images
                var totalImages = allImages.Count;
                var processedImages = 0;
                var successCount = 0;
                var failureCount = 0;
                var skippedCount = 0;

                foreach (var imageData in allImages)
                {
                    var image = imageData.Item1;
                    var categoryPath = imageData.Item2;

                    try
                    {
                        if (string.IsNullOrWhiteSpace(image.ElementUrl))
                        {
                            OnStatusChanged($"Skipping image {image.Id} ({image.Name ?? image.File}): No download URL available");
                            skippedCount++;
                            processedImages++;
                            continue;
                        }

                        OnStatusChanged($"Downloading image: {image.Name ?? image.File} ({processedImages + 1}/{totalImages})");

                        var imageBytes = _piwigoClient.DownloadImage(image.ElementUrl);

                        OnStatusChanged($"Saving image: {image.Name ?? image.File}");
                        _digiKamExporter.SaveImage(image, imageBytes, categoryPath);

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        OnStatusChanged($"Failed to transfer image {image.Id} ({image.Name ?? image.File}): {ex.Message}");
                        failureCount++;
                    }

                    processedImages++;
                    OnProgressChanged(new TransferProgressEventArgs
                    {
                        TotalImages = totalImages,
                        ProcessedImages = processedImages,
                        SuccessCount = successCount,
                        FailureCount = failureCount,
                        CurrentImageName = image.Name ?? image.File
                    });
                }

                OnStatusChanged($"Transfer completed! Success: {successCount}, Failed: {failureCount}, Skipped: {skippedCount}");
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Error: {ex.Message}");
                throw;
            }
            finally
            {
                _piwigoClient.Logout();
            }
        }

        protected virtual void OnProgressChanged(TransferProgressEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }
    }

    public class TransferProgressEventArgs : EventArgs
    {
        public int TotalImages { get; set; }
        public int ProcessedImages { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public string CurrentImageName { get; set; }

        public int PercentComplete
        {
            get
            {
                if (TotalImages == 0) return 0;
                return (int)((double)ProcessedImages / TotalImages * 100);
            }
        }
    }
}
