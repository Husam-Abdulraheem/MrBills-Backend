using BillMate.DTOs;

namespace BillMate.Interfaces
{
    public interface IFileService
    {
        public Task<ResponseDTO> ProcessReceipt(IFormFile file, string type);

    }
}
