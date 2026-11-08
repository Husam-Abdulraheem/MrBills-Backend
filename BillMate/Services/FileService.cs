using BillMate.DTOs;
using BillMate.Interfaces;

namespace BillMate.Services
{
    public class FileService : IFileService
    {

        private readonly IWebHostEnvironment _env;

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }
        public async Task<ResponseDTO> ProcessReceipt(IFormFile file, string type)
        {

            if (file == null || file.Length == 0)
            {
                return new ResponseDTO { Message = "The file is invalid." };
            }

            // Check allowed extensions (eg: PDF)
            var allowedExtensions = new[] {".pdf", ".jpg", ".png", ".jpeg", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return new ResponseDTO { Message = "File extension not supported." };
            }

            // Create a unique file name while preserving the extension.
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // Final path to save
            var uploadsPath = Path.Combine(_env.WebRootPath, $"uploads/{type}");
            var fullPath = Path.Combine(uploadsPath, uniqueFileName);

            // Create the folder if it does not exist
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // الحفظ على السيرفر

            try
            {
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {

                return new ResponseDTO
                {
                    Message = "Failed to save file",
                    Error = ex.Message
                };
            }
           

            return new ResponseDTO { Data= uniqueFileName, Success = true };
        }
    }
}
