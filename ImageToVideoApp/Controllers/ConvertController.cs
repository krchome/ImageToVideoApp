using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("api/convert")]
public class ConvertController : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/videos");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        string filePath = Path.Combine(uploadsFolder, file.FileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Simulate generating a 10-second animated video
        await Task.Delay(3000);

        string outputFilePath = "/videos/output.mp4"; // Replace with actual generated file path
        return Ok(outputFilePath);
    }
}
