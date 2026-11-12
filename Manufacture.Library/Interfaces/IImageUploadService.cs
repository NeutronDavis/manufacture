namespace Manufacture.Library.Interfaces
{
    /// <summary>
    /// Stores an uploaded image under wwwroot and returns its public relative URL.
    /// </summary>
    public interface IImageUploadService
    {
        Task<string> UploadAsync(
            Stream content,
            string fileName,
            string folder,
            CancellationToken cancellationToken = default);
    }
}
