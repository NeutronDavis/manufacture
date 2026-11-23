using Manufacture.Library.Interfaces;

namespace Manufacture.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private readonly IWebHostEnvironment _environment;

        public ImageUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> UploadAsync(
            Stream content,
            string fileName,
            string folder,
            CancellationToken cancellationToken = default)
        {
            if (content == null || content.Length == 0)
            {
                throw new InvalidOperationException("Select a valid image file.");
            }

            if (content.Length > MaxImageBytes)
            {
                throw new InvalidOperationException("Image size cannot exceed 5 MB.");
            }

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Only JPG, PNG, WEBP, and GIF images are supported.");
            }

            var safeFolder = string.Join('/', (folder ?? "uploads")
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => new string(segment.Where(char.IsLetterOrDigit).ToArray())))
                .Trim('/');

            if (string.IsNullOrWhiteSpace(safeFolder))
            {
                safeFolder = "uploads";
            }

            var webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var targetDirectory = Path.Combine(webRoot, safeFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(targetDirectory);

            var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var targetPath = Path.Combine(targetDirectory, storedName);

            await using (var output = File.Create(targetPath))
            {
                await content.CopyToAsync(output, cancellationToken);
            }

            return "/" + safeFolder + "/" + storedName;
        }
    }
}
