using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Middleware;
using BillMate.Models;
using BillMate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BillMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<User> _userManager;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, UserManager<User> userManager)
        {
            _authService = authService;
            _logger = logger;
            _userManager = userManager;
        }

        [Route("login")]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid login attempt with invalid model state.");
                    return BadRequest(ModelState);
                }

                var user = await _authService.Login(loginDTO);

                if (!user.Success)
                {
                    _logger.LogWarning($"Invalid login attempt for email: {loginDTO.Email}");
                    return Ok(user);

                }
                return Ok(user);

            }
            catch (Exception ex)
            {

                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }

        //[Route("refreshToken")]
        //[HttpPost]
        //public async Task<IActionResult> RefreshToken(RefreshTokenModel model)
        //{
        //    var loginResult = await _authService.RefreshToken(model);
        //    if (loginResult.Success)
        //        return Ok(loginResult);
        //    return Ok(loginResult);
        //}

        [Route("register")]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid Register attempt with invalid model state.");
                    return Ok(new ResponseDTO { Message = "Invalid entry" });
                }
                var group = await _authService.Register(registerDTO);
                if (group == null)
                {
                    _logger.LogWarning($"Invalid Register attempt for email: {registerDTO.Email}");
                    return Ok(group);
                }
                return Ok(group);

            }
            catch (Exception ex)
            {

                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }

        [Route("confirm-email")]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
                return BadRequest("Invalid confirmation request.");

            var result = await _authService.EmailConfirmationAsync(userId, code);
            if(result.Success)            
            return Ok(result);

            return Ok(result);
        }

        [Route("ForgotPassword")]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO? forgotPasswordDTO)
        {
            if (forgotPasswordDTO == null || string.IsNullOrEmpty(forgotPasswordDTO.Email))
            {
                _logger.LogWarning("ForgotPasswordDTO is null or email is empty.");
                return BadRequest("Invalid request data.");
            }

            var result = await _authService.ForgotPassword(forgotPasswordDTO);
            if (result.Success)
                return Ok(result);
            return Ok(result);
        }


        [Route("ResetPassword")]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO resetPasswordDTO)
        {

            var result = await _authService.ResetPassword(resetPasswordDTO);
            if (result.Success)
                return Ok(result);
            return Ok(result);
        }



        [Route("deleteUser")]
        [HttpDelete]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Delete(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email is null or empty.");
                return BadRequest("Invalid email.");
            }
            
            var deleted = await _authService.Delete(email);
            return Ok(deleted);
        }


    }
}
