using InstaShareFileService.Models;
using InstaShareFileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InstaShareFileService.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
        private readonly FileService _fileService;

        public FileController(FileService fileService)
        {
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<FileMetadata>> GetFiles()
        {
            var files = await _fileService.GetFiles();
            return files;
        }

        [HttpGet("GetFileId/{id}")]
        [Authorize]
        public async Task<FileMetadata> GetFileById(int id)
        {
            var fileid = await _fileService.GetFilesId(id);
            return fileid;
        }

        [HttpGet("GetFileUser")]
        [Authorize]
        public async Task<IEnumerable<FileMetadata>> GetFilesUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var files = await _fileService.GetFilesUser(userId);
            return files;
        }

        [HttpGet("downloadfile/{fileName}")]
        [Authorize]
        public async Task<IActionResult> Download([FromRoute] string fileName)
        {
            var result = await _fileService.DownloadFile(fileName);
            return result;
        }

        [HttpPut("renamefile")]
        [Authorize]
        public async Task<IActionResult> Rename(string oldName,  string newName)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _fileService.RenameFile(userId, oldName, newName);
            return Ok("File renamed successfully.");
        }

        [HttpDelete("deletefile")]
        [Authorize]
        public async Task<IActionResult> Delete(string fileName)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _fileService.DeleteFile(userId, fileName);
            return Ok("File deleted successfully.");
        }
    }
}
