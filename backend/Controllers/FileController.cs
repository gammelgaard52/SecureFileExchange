using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SecureFileExchange.Controllers
{
    [Route("api/files")]
    [ApiController]
    public class FileController : ControllerBase
    {
        [HttpPost("upload")]
        public IActionResult UploadFile([FromForm] IFormFile file, [FromForm] string password)
        {
            if (file == null || string.IsNullOrEmpty(password))
                return BadRequest("File and password are required.");

            var fileService = new FileService(password); // ✅ Create instance per request

            var result = fileService.SaveFile(file, password);
            return Ok(result);
        }

        [HttpGet("list/{password}")]
        public IActionResult ListFiles(string password)
        {
            if (string.IsNullOrEmpty(password))
                return BadRequest("Password is required.");

            var fileService = new FileService(password); // ✅ Create instance per request

            var files = fileService.ListFiles(password);
            return Ok(files);
        }

        [HttpGet("download/{password}/{fileId}")]
        public IActionResult DownloadFile(string password, string fileId)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fileId))
                return BadRequest("Password and file ID are required.");

            var fileService = new FileService(password); // ✅ Create instance per request

            var fileData = fileService.GetFile(password, fileId);
            if (fileData == null)
                return NotFound("File not found or incorrect password.");

            return File(fileData.Value.FileContent, fileData.Value.ContentType, fileData.Value.FileName);
        }
    }
}
