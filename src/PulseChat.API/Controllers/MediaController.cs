using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Domain.Common;

namespace PulseChat.API.Controllers;

public record MediaUploadResponse(string Url, string FileName, long Size);

public class UploadMediaRequest
{
    public IFormFile? File { get; set; }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MediaController : ControllerBase
{
    private readonly IStorageService _storageService;

    public MediaController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Result<MediaUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<MediaUploadResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromForm] UploadMediaRequest request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
        {
            return BadRequest(Result.Failure<MediaUploadResponse>(ErrorCodes.ValidationFailed, "Dosya seçilmedi."));
        }

        // Limit file size to 25MB
        if (file.Length > 25 * 1024 * 1024)
        {
            return BadRequest(Result.Failure<MediaUploadResponse>(ErrorCodes.ValidationFailed, "Dosya boyutu 25MB'ı aşamaz."));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var url = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

            var response = new MediaUploadResponse(url, file.FileName, file.Length);
            return Ok(Result.Success(response));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result.Failure<MediaUploadResponse>(ErrorCodes.FileUploadFailed, $"Dosya yüklenirken hata oluştu: {ex.Message}"));
        }
    }
}
