using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Automatically downloads Tesseract OCR trained data files.
    /// Uses tessdata_fast for optimized speed.
    /// </summary>
    public static class TessdataDownloader
    {
        private const string TessdataFastBaseUrl = "https://github.com/tesseract-ocr/tessdata_fast/raw/main/";

        private static readonly string DefaultTessDataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "tessdata");

        /// <summary>
        /// Ensures the specified language data file exists, downloading if necessary.
        /// </summary>
        /// <param name="language">Language code (default: "eng" for English)</param>
        /// <param name="tessDataPath">Path to tessdata folder (null for default)</param>
        /// <returns>True if the file exists or was downloaded successfully</returns>
        public static async Task<bool> EnsureLanguageDataExistsAsync(string language = "eng", string tessDataPath = null)
        {
            string dataPath = tessDataPath ?? DefaultTessDataPath;
            string trainedDataFile = Path.Combine(dataPath, $"{language}.traineddata");

            // If file already exists, we're good
            if (File.Exists(trainedDataFile))
            {
                return true;
            }

            // Create directory if it doesn't exist
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            // Download the file
            return await DownloadTrainedDataAsync(language, trainedDataFile);
        }

        /// <summary>
        /// Synchronous version of EnsureLanguageDataExistsAsync.
        /// </summary>
        public static bool EnsureLanguageDataExists(string language = "eng", string tessDataPath = null)
        {
            return EnsureLanguageDataExistsAsync(language, tessDataPath).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Downloads the trained data file for the specified language.
        /// </summary>
        private static async Task<bool> DownloadTrainedDataAsync(string language, string destinationPath)
        {
            string url = $"{TessdataFastBaseUrl}{language}.traineddata";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5); // Large file, give it time

                    // Add user agent to avoid being blocked
                    client.DefaultRequestHeaders.Add("User-Agent", "ToonTown-Rewritten-Bot");

                    System.Diagnostics.Debug.WriteLine($"Downloading OCR data from: {url}");

                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var data = await response.Content.ReadAsByteArrayAsync();

                    await File.WriteAllBytesAsync(destinationPath, data);

                    System.Diagnostics.Debug.WriteLine($"OCR data downloaded successfully: {destinationPath} ({data.Length / 1024}KB)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to download OCR data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the path to the tessdata folder.
        /// </summary>
        public static string GetTessDataPath(string customPath = null)
        {
            return customPath ?? DefaultTessDataPath;
        }

        /// <summary>
        /// Checks if the language data file exists.
        /// </summary>
        public static bool LanguageDataExists(string language = "eng", string tessDataPath = null)
        {
            string dataPath = tessDataPath ?? DefaultTessDataPath;
            string trainedDataFile = Path.Combine(dataPath, $"{language}.traineddata");
            return File.Exists(trainedDataFile);
        }
    }
}
