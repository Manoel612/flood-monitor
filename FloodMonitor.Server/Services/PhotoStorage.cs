namespace FloodMonitor.Server.Services;

public class PhotoStorage
{
    public const long MaxSizeInBytes = 5 * 1024 * 1024;

    public string Folder { get; }

    public PhotoStorage(IWebHostEnvironment environment)
    {
        Folder = Path.Combine(environment.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(Folder);
    }

    public async Task<(string? FileName, string? Error)> SaveAsync(IFormFile photo, CancellationToken ct)
    {
        if (photo.Length == 0)
            return (null, "A foto enviada está vazia.");
        if (photo.Length > MaxSizeInBytes)
            return (null, "A foto deve ter no máximo 5 MB.");

        byte[] original;
        await using (var stream = photo.OpenReadStream())
        using (var buffer = new MemoryStream((int)photo.Length))
        {
            await stream.CopyToAsync(buffer, ct);
            original = buffer.ToArray();
        }

        if (!JpegMetadataRemover.IsJpeg(original))
            return (null, "Envie uma foto em JPEG.");

        var withoutMetadata = JpegMetadataRemover.Remove(original);
        if (withoutMetadata is null)
            return (null, "Não foi possível ler essa foto. Tente outra imagem.");

        var fileName = $"{Guid.NewGuid():N}.jpg";
        await File.WriteAllBytesAsync(Path.Combine(Folder, fileName), withoutMetadata, ct);
        return (fileName, null);
    }

    public void Delete(string fileName)
    {
        var path = Path.Combine(Folder, Path.GetFileName(fileName));
        if (File.Exists(path))
            File.Delete(path);
    }
}
