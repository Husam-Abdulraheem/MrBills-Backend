using BillMate.DTOs.PubSub;
using BillMate.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BillMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GooglePlayController : ControllerBase
    {

        private readonly ILogger<GooglePlayController> _logger;
        private readonly IGooglePlayBillingService _googlePlayService;

        public GooglePlayController(ILogger<GooglePlayController> logger, IGooglePlayBillingService googlePlayService)
        {
            _logger = logger;
            _googlePlayService = googlePlayService;
        }



        [HttpPost("notifications")]
        public async Task<IActionResult> ReceiveNotification([FromBody] PubSubMessage message)
        {
            try
            {
                // 1. Decoded Data Base64
                var decodedData = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(message.Message.Data));

                _logger.LogInformation("Google Play Notification: {Data}", decodedData);

                // 2. Change text to object
                var notification = JsonSerializer.Deserialize<GooglePlayNotification>(decodedData);

                // 3. Update db
                await _googlePlayService.HandleNotificationAsync(notification);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google Play notification");
                return Ok();
            }
        }




    }
}
