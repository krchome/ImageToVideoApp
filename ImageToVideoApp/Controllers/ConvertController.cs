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

        try
        {
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/videos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique filenames to prevent conflicts
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{timestamp}{Path.GetExtension(file.FileName)}";
            string imagePath = Path.Combine(uploadsFolder, uniqueFileName);
            string outputFilePath = Path.Combine(uploadsFolder, $"output_{timestamp}.mp4");

            Console.WriteLine($"Saving uploaded file to {imagePath}");

            // Save uploaded file
            using (var stream = new FileStream(imagePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Wait briefly to ensure file is fully written
            await Task.Delay(100);

            // Check if file was saved correctly
            if (!System.IO.File.Exists(imagePath))
            {
                return StatusCode(500, "Failed to save uploaded file");
            }

            Console.WriteLine($"File saved successfully. Size: {new FileInfo(imagePath).Length} bytes");

            // FFmpeg Command - simplified for maximum compatibility
            string ffmpegArgs = $"-y -loop 1 -i \"{imagePath}\" -c:v libx264 -t 10 -pix_fmt yuv420p \"{outputFilePath}\"";

            Console.WriteLine($"Running FFmpeg command: {ffmpegArgs}");

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
            await process.WaitForExitAsync();

            // Add a brief delay to ensure the file is fully written
            await Task.Delay(500);

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"FFmpeg Error: {stderr}");
                return StatusCode(500, $"FFmpeg failed: {stderr}");
            }

            // Check if the output file exists
            if (!System.IO.File.Exists(outputFilePath))
            {
                Console.WriteLine("Output file was not created");
                return StatusCode(500, "Failed to create output video file");
            }

            Console.WriteLine($"Video generated successfully at {outputFilePath}");

            // Return the URL of the generated video
            string videoUrl = $"/videos/output_{timestamp}.mp4";

            return Ok(new { message = "Video generated successfully!", videoUrl = videoUrl });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, $"Exception: {ex.Message}");
        }
    }
}