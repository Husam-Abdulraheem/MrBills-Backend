using BillMate.Models;

namespace BillMate.Interfaces
{
    public interface IEmailService
    {
        Task ConfirmEmailAsync(User user);
        Task ForgotPasswordAsync(User user);
    }
}
