namespace SSPMS.Application.Interfaces;

public interface IImageService
{
    Task<string> UploadAsync(Stream imageStream, string fileName, string contentType);
}
