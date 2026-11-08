using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Models;
using BillMate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace BillMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }



        [HttpGet("GetMyNotifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _notificationService.GetMyNotifications(userId);

            return Ok(result);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("BroadcastNotification")]
        public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationDTO broadcast)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _notificationService.BroadcastNotification(broadcast);

            return Ok(result);
        }
    }
}
