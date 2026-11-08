using BillMate.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BillMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IMailerSendService _mailer;

        public EmailController(IMailerSendService mailer)
        {
            _mailer = mailer;
        }


        [HttpPost("send-emails-for-all-users")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> SendEmails()
        {
            await _mailer.SendEmailAsync();
            return Ok("Emails processed");
        }
    }
}
