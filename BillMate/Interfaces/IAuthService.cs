using BillMate.DTOs;
using BillMate.Models;

namespace BillMate.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginDTO loginDTO);
        Task<ResponseDTO> Register(RegisterDTO registerDTO);
        //Task<LoginResponse> RefreshToken(RefreshTokenModel model);
        Task<ResponseDTO> EmailConfirmationAsync(string userId, string code);
        Task<ResponseDTO> ForgotPassword(ForgotPasswordDTO forgotPasswordDTO);
        Task<ResponseDTO> ResetPassword(ResetPasswordDTO resetPasswordDTO);
        Task<ResponseDTO> Delete(string email);

    }
}
