using InstaShareUploadFileService.Models;
using InstaShareUploadFileService.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace InstaShareUploadFileService.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class FileUploadController : ControllerBase
    {
        private readonly FileUploadService _fileUploadService;

        public FileUploadController(FileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<UploadedFile>> GetFilesUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var files = await _fileUploadService.GetFilesUser(userId);
            return files;
        }

        [HttpPost("uploadfile")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] FileUploadDto fileUploadDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _fileUploadService.UploadFileAsync(fileUploadDto, userId);
            return Ok("File uploaded successfully.");
        }
    }
}
