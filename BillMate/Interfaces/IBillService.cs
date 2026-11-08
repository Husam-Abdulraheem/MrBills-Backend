using BillMate.DTOs;
using BillMate.Middleware;
using BillMate.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace BillMate.Interfaces
{
    public interface IBillService
    {
        Task<ResponseDTO> CreateBill(string userId, CreateBillDTO billDTO);
        Task<ResponseDTO> MyBills(string userId);
        Task<ResponseDTO> Bills(string userId);
        Task<ResponseDTO> EditBill(string userId, EditBillDTO editBillDTO);
        Task<ResponseDTO> PayBill(string userId, PayBillDTO dto);
        Task<ResponseDTO> SettlementBill(string userId, SettlementDTO dto);
        Task<ResponseDTO> DeleteBill(int billId, string userId);
        // Archive Methods
        Task<ResponseDTO> ArchivedBills(string userId);
        Task CleanupArchivedBillsAsync();






    }
}
