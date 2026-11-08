using BillMate.Data;
using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Middleware;
using BillMate.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace BillMate.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthService> _logger;
        private readonly EmailService _emailService;
        private readonly SignInManager<User> _signInManager;

        public AuthService(ApplicationDbContext db, UserManager<User> userManager, ILogger<AuthService> logger, TokenService tokenService, EmailService emailService, SignInManager<User> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _tokenService = tokenService;
            _logger = logger;
            _emailService = emailService;
            _signInManager = signInManager;            
        }

        // Login Method
        public async Task<LoginResponse> Login(LoginDTO loginDTO)
        {
            try
            {
                var response = new LoginResponse();
                if (loginDTO.Email == null || loginDTO.Password == null)
                    return response;
                // Check User and Password
                var user = await _userManager.FindByEmailAsync(loginDTO.Email);
                if (user?.UserName == null) 
                    return (new LoginResponse { Message= "Invalid email or password" });


                var signInResult = await _signInManager.PasswordSignInAsync(user.UserName,loginDTO.Password, false,lockoutOnFailure: false);

                // Add resend email verification if not verified
                if (signInResult.IsNotAllowed)
                {
                    await _emailService.ConfirmEmailAsync(user);
                    return (new LoginResponse { Message= "We have sent a link to your email to re-verify your email." });
                }
                if (!signInResult.Succeeded)
                    return new LoginResponse { Message = "Invalid email or password" };

                response.Group = user.GroupId != null;
                response.Success = true;
                response.Message = "You have successfully logged in.";
                response.Token = _tokenService.GenerateJwtToken(user.IsAdmin, user);
                //response.RefreshToken = _tokenService.GenerateRefreshToken();

                //user.RefreshToken = response.RefreshToken;
                //user.RefreshTokenExpiry = DateTime.Now.AddMinutes(15);
                //await _userManager.UpdateAsync(user);


                return response;
            }
            catch (Exception ex)
            {

                throw new Exception("Error Login" + ex);
            }
        }


        //public async Task<LoginResponse> RefreshToken(RefreshTokenModel model)
        //{

        //    var principal = _tokenService.ValidateToken(model.TokenJwt);
        //    var response = new LoginResponse();
        //    if (principal?.Identity?.Name is null)
        //        return response;
        //    var user =await _userManager.FindByNameAsync(principal.Identity.Name);

        //    if(user is null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiry > DateTime.Now)
        //        return response;



        //    response.Success = true;
        //    response.JwtToken = _tokenService.GenerateJwtToken(user.IsAdmin, user);
        //    response.RefreshToken = _tokenService.GenerateRefreshToken();

        //    user.RefreshToken = response.RefreshToken;
        //    user.RefreshTokenExpiry = DateTime.Now.AddMinutes(15);
        //    await _userManager.UpdateAsync(user);
        //    return response;
        //}

        public async Task<ResponseDTO> Register(RegisterDTO registerDTO)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                if (registerDTO.Email is null || registerDTO.Password is null || registerDTO.Password != registerDTO.ConfirmPassword)

                    return (new ResponseDTO { Message = "Password and confirmPassword are not same" });
                

                // Check User 
                var registeredUser = await _userManager.FindByEmailAsync(registerDTO.Email);

                if (registeredUser != null)
                    return (new ResponseDTO{ Message = " This User alrady registerd" });

                var user = new User()
                {
                    FullName = registerDTO.FullName,
                    Email = registerDTO.Email,
                    UserName = registerDTO.Email
                };

                await _userManager.CreateAsync(user, registerDTO.Password);
                await _userManager.AddToRoleAsync(user, "User");



                // Email Service for generate link and send email
                await _emailService.ConfirmEmailAsync(user);
                await transaction.CommitAsync();

                return (new ResponseDTO {Message = "Check your email", Success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("You do not meet all registration requirements." + ex);
            }
        }



        public async Task<ResponseDTO> EmailConfirmationAsync(string? userId, string? code)
        {
            if (userId == null || code == null)
                return (new ResponseDTO { Message = "Null" });
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return (new ResponseDTO { Message = "Null" });
            //var decodedCode = System.Net.WebUtility.UrlDecode(code);
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
                return (new ResponseDTO { Message = "Email has been successfully verified.", Success = true });

            return (new ResponseDTO { Message = "Email not verified" });

        }

       

        public async Task<ResponseDTO> ForgotPassword(ForgotPasswordDTO? forgotPasswordDTO)
        {

            if (forgotPasswordDTO?.Email == null)
                return (new ResponseDTO { Message = "null value" });

            var user = await _userManager.FindByEmailAsync(forgotPasswordDTO.Email);
            if (user == null)
                return (new ResponseDTO { Message = "Email is not registered" });
             await _emailService.ForgotPasswordAsync(user);

            return (new ResponseDTO { Message = "We have sent a link to your email to Reset your Password.", Success = true });

        }

        public async Task<ResponseDTO> ResetPassword(ResetPasswordDTO resetPasswordDTO)
        {
            if (resetPasswordDTO.UserId == null)
                return new ResponseDTO { Message = "User is not exists"};
            var user = await _userManager.FindByIdAsync(resetPasswordDTO.UserId);


            var result = await _userManager.ResetPasswordAsync(user!, resetPasswordDTO.Code, resetPasswordDTO.NewPassword);
            if (result.Succeeded)
            return (new ResponseDTO { Message= "Password changed successfully", Success= true });

            return new ResponseDTO { Message = "An error occurred while changing the password." };

        }


        public async Task<ResponseDTO> Delete(string email)
        {
            if(email == null)
                return (new ResponseDTO { Message = "No email there" });

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return (new ResponseDTO { Message = "This user does not exists" });
            
            var debts = await _db.Debts.Where(d => d.FromUserId == user.Id || d.ToUserId == user.Id).ToListAsync();
            if (debts != null)
                _db.RemoveRange(debts);

            var bills = await _db.Bills.Where(b=>b.OwnerId == user.Id).ToListAsync();
            if (bills != null)
                _db.RemoveRange(bills);

            var groupUser = await _db.GroupUsers.FirstOrDefaultAsync(u=>u.UserId == user.Id);
            if(groupUser != null)
                _db.Remove(groupUser);

            var group = await _db.Groups.FirstOrDefaultAsync(g=>g.ManagerId == user.Id);

            if (group != null)
            {
                var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s=>s.GroupId == group.Id);
                if(subscription != null)
                _db.Remove(subscription);
                _db.Remove(group);
            }

            await _userManager.DeleteAsync(user);
            return (new ResponseDTO { Message = "Deleted successfully", Success = true });
        }

 
    }
}
