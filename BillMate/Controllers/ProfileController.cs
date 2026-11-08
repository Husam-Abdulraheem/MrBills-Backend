using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Models;
using BillMate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BillMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [Route("profile")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetProfileAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _profileService.GetProfileAsync(userId);
            return Ok(result);
        }



        [Route("update-profile")]
        [HttpPatch]
        [Authorize]
        public async Task<IActionResult> GetProfileAsync([FromBody]JsonPatchDocument<UpdateUserDTO> dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            if (dto == null)
                return Ok(new ResponseDTO { Message = $"Error when update user with id: {userId}" });

            var result = await _profileService.UpdateProfile(userId, dto);
            return Ok(result);
        }



        [Route("exit-group")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ExitGroup()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _profileService.ExitGroup(userId);
            return Ok(result);
        }

    }
}
