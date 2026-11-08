using BillMate.Data;
using BillMate.DTOs;
using BillMate.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BillMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }


        [Route("view-groups")]
        [HttpGet]
        [Authorize(Policy = "Admin")]

        public async Task<IActionResult> GroupView()
        {
            var groups = await _adminService.ViewGroups();
            return Ok(groups);
        }

        [Route("subscriptions")]
        [HttpGet]
        [Authorize(Policy = "Admin")]


        public async Task<IActionResult> Subscriptions()
        {
            var result = await _adminService.Subscriptions();
            return Ok(result);
        }


        [Route("send-email-for-inActive-subscriptions")]
        [HttpGet]
        public async Task<IActionResult> SendEmailForInActiveSubscriptions()
        {
            var result = await _adminService.SendEmailForInActiveSubscriptions();
            return Ok(result.Data);
        }
    }
}
