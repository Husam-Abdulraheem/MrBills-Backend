using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BillMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {

        private readonly IGroupService _groupService;
        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }


        [Route("getGroup")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetGroupAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _groupService.GetGroupAsync(userId);
            return Ok(result);
        }




        [Route("join-group")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> JoinGroup([FromBody]JoinDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _groupService.JoinGroup(userId, dto.Code);
            return Ok(result);
        }




        


        [Route("kick-group-member")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> KickGroupMember(string kickedMember)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _groupService.KickGroupMember(userId, kickedMember);
            return Ok(result);
        }



        [Route("subscription")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Subscription()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _groupService.Subscription(userId);
            return Ok(result);
        }


    }
}
