using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.IO;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public UploadController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        // Validate size (10 MB max)
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds 10 MB limit" });

        // Validate extension
        var extension = Path.GetExtension(file.FileName).ToLower();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Invalid file format. Allowed: JPG, PNG, PDF" });

        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            var base64String = Convert.ToBase64String(fileBytes);
            
            // Determine MIME type
            var mimeType = file.ContentType;
            if (string.IsNullOrEmpty(mimeType))
            {
                mimeType = "image/jpeg";
                if (extension == ".png") mimeType = "image/png";
                if (extension == ".gif") mimeType = "image/gif";
                if (extension == ".webp") mimeType = "image/webp";
            }
            
            var base64Url = $"data:{mimeType};base64,{base64String}";
            return Ok(new { url = base64Url });
        }
    }
}
