namespace TrelloApi.Helpers;

/// <summary>
/// Handles local file system storage for task attachments.
/// Files are stored in wwwroot/Uploads/{taskId}/ with GUID-based names.
/// </summary>
public class FileHelper
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public FileHelper(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Validates and saves an uploaded file. Returns the stored file path.
    /// </summary>
    public async Task<(string storedName, string filePath)> SaveFileAsync(
        IFormFile file,
        long taskId,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var uploadFolder = GetUploadDirectory(taskId);
        Directory.CreateDirectory(uploadFolder);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(uploadFolder, storedName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return (storedName, fullPath);
    }

    /// <summary>
    /// Deletes a file from the local file system.
    /// </summary>
    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private void ValidateFile(IFormFile file)
    {
        var maxSize = _configuration.GetValue<long>("FileStorage:MaxFileSizeBytes");
        if (file.Length > maxSize)
            throw new InvalidOperationException($"File size exceeds the maximum allowed size of {maxSize / 1_048_576} MB.");

        var allowed = _configuration.GetSection("FileStorage:AllowedExtensions")
                                    .Get<string[]>() ?? Array.Empty<string>();
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowed.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");
    }

    private string GetUploadDirectory(long taskId)
    {
        var basePath = _configuration["FileStorage:UploadPath"] ?? "Uploads";
        return Path.Combine(_environment.ContentRootPath, basePath, taskId.ToString());
    }
}
