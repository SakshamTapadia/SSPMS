using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SSPMS.Application.Interfaces;

namespace SSPMS.Infrastructure.Services;

public class CloudinaryImageService : IImageService
{
    private readonly IHttpClientFactory _http;
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public CloudinaryImageService(IHttpClientFactory http, IConfiguration config)
    {
        _http      = http;
        _cloudName = config["Cloudinary:CloudName"]!.Trim();
        _apiKey    = config["Cloudinary:ApiKey"]!.Trim();
        _apiSecret = config["Cloudinary:ApiSecret"]!.Trim();
    }

    public async Task<string> UploadAsync(Stream imageStream, string fileName, string contentType)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string folder = "sspms-questions";

        // Cloudinary signed upload: SHA1( sorted_params + api_secret )
        var toSign    = $"folder={folder}&timestamp={timestamp}{_apiSecret}";
        var signature = ComputeSha1(toSign);

        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(imageStream);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        form.Add(fileContent,                    "file",      fileName);
        form.Add(new StringContent(_apiKey),     "api_key");
        form.Add(new StringContent(timestamp),   "timestamp");
        form.Add(new StringContent(folder),      "folder");
        form.Add(new StringContent(signature),   "signature");

        using var client   = _http.CreateClient();
        using var response = await client.PostAsync(
            $"https://api.cloudinary.com/v1_1/{_cloudName}/image/upload", form);

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Cloudinary upload failed ({(int)response.StatusCode}): {body}");

        using var doc      = JsonDocument.Parse(body);
        var secureUrl      = doc.RootElement.GetProperty("secure_url").GetString()!;

        // Insert f_auto,q_auto so Cloudinary auto-converts HEIC/WebP for all browsers
        const string uploadSegment = "/image/upload/";
        var idx = secureUrl.IndexOf(uploadSegment, StringComparison.Ordinal);
        if (idx >= 0)
            secureUrl = secureUrl.Insert(idx + uploadSegment.Length, "f_auto,q_auto/");

        return secureUrl;
    }

    private static string ComputeSha1(string input)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
