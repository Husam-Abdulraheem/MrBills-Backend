using BillMate.Data;
using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sprache;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BillMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly IGooglePlayBillingService _billingService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IShopierService _shopierService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SubscriptionController> _logger;


        public SubscriptionController(IGooglePlayBillingService billingService, ISubscriptionService subscriptionService, IShopierService shopierService, ApplicationDbContext db, ILogger<SubscriptionController> logger)
        {

            _billingService = billingService;
            _subscriptionService = subscriptionService;
            _shopierService = shopierService;
            _db = db;
            _logger = logger;
        }




        [Route("Create-subscription")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSubscription([FromBody] SubscriptionDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _subscriptionService.CreateSubscription(dto, userId);

            if (result.HtmlContent != null)
            {
                return Content(result.HtmlContent, "text/html");
            }
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }



        [Route("renew-subscription")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RenewSubscription([FromBody] SubscriptionDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _subscriptionService.RenewSubscription(userId, dto);
            if (result.HtmlContent != null)
            {
                return Content(result.HtmlContent, "text/html");
            }
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result);

        }



        [Route("check-subscription")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CheckSubscriptionForManager()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authorized." });

            var result = await _subscriptionService.CheckSubscriptionForManager(userId);
            return Ok(result);
        }







        [HttpPost("shopier-callback")]
        public async Task<IActionResult> ShopierCallback([FromForm] ShopierCallbackDTO dto)
        {

            try
            {
            if (string.IsNullOrEmpty(dto.Signature))
                return BadRequest(new { error = "Invalid callback data" });

                bool success = await _shopierService.ProcessCallbackAsync(dto);

                return Redirect(success
                   ? "https://app.mister-bills.com/order-success"
                   : "https://app.mister-bills.com/order-failed");

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Shopier callback error");
                return Redirect( "https://app.mister-bills.com/order-failed" );
            }
          
        }




        [HttpPost("shopier-osb-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> ShopierOsbCallback([FromForm] string res, [FromForm] string hash)
        {

            string username = Environment.GetEnvironmentVariable("OSB_USER_NAME");
            string key = Environment.GetEnvironmentVariable("OSB_PASSWORD");


            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(res + username));
            var expectedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();


            string jsonResult;
            jsonResult = Encoding.UTF8.GetString(Convert.FromBase64String(res));
       

            ShopierOsbData data;

            data = JsonSerializer.Deserialize<ShopierOsbData>(jsonResult);
 
 
            bool success = await _shopierService.ProcessOsbCallbackAsync(data);

            return Content("success");

        }


    }

}
