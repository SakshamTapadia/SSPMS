using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/upload")]
[Authorize(Roles = "Admin,Trainer")]
public class UploadController : BaseController
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".heic", ".heif", ".webp", ".bmp" };

    private static readonly Dictionary<string, string> MimeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg",  "image/jpeg" }, { ".jpeg", "image/jpeg" },
            { ".png",  "image/png"  }, { ".gif",  "image/gif"  },
            { ".heic", "image/heic" }, { ".heif", "image/heif" },
            { ".webp", "image/webp" }, { ".bmp",  "image/bmp"  }
        };

    private const long MaxBytes = 20 * 1024 * 1024; // 20 MB

    private readonly IImageService? _images;

    public UploadController(IServiceProvider sp)
        => _images = sp.GetService<IImageService>();

    [HttpGet("image/status")]
    [AllowAnonymous]
    public IActionResult GetImageUploadStatus()
        => Ok(new { available = _images != null });

    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBytes)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (_images == null)
            return StatusCode(503, new { message = "Image upload is not configured on this server. Please contact the administrator." });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        if (file.Length > MaxBytes)
            return BadRequest(new { message = "File exceeds the 20 MB limit." });

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = $"File type '{ext}' is not supported. Allowed: jpg, jpeg, png, gif, heic, heif, webp, bmp." });

        var mime = MimeTypes.TryGetValue(ext, out var m) ? m : "application/octet-stream";

        try
        {
            using var stream = file.OpenReadStream();
            var url = await _images.UploadAsync(stream, file.FileName, mime);
            return Ok(new { imageUrl = url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Upload failed: {ex.Message}" });
        }
    }
}
