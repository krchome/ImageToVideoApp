using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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

        string imagePath = Path.Combine(uploadsFolder, file.FileName);

        // Save uploaded file
        using (var stream = new FileStream(imagePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Generate unique filename to prevent conflicts
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputFilePath = Path.Combine(uploadsFolder, $"output_{timestamp}.mp4");

        // FFmpeg Command
        string ffmpegArgs = $"-y -loop 1 -i \"{imagePath}\" -c:v libx264 -t 10 -pix_fmt yuv420p -vf \"scale=1280:720\" \"{outputFilePath}\"";

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Capture errors
            string stderr = await process.StandardError.ReadToEndAsync();
            string stdout = await process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();

            // Add a brief delay to ensure the file is fully written
            await Task.Delay(500);

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"FFmpeg Error: {stderr}");
                return StatusCode(500, $"FFmpeg failed: {stderr}");
            }

            // Return the URL of the generated video
            return Ok(new { message = "Video generated successfully!", videoUrl = $"/videos/output_{timestamp}.mp4" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            return StatusCode(500, $"Exception: {ex.Message}");
        }
    }
}
