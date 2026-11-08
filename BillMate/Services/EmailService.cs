using BillMate.Interfaces;
using BillMate.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Sprache;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;

namespace BillMate.Services
{
    public class EmailService : IEmailService
    {
        private readonly UserManager<User> _userManager;
        public EmailService(UserManager<User> userManager)
        {

            _userManager = userManager;
        }



        public async Task SendEmailTemplateAsync(string toEmail, string toName, string fromName, string subject, string templateId, Dictionary<string, object> variables)
        {
            var mailerSendToken = Environment.GetEnvironmentVariable("MAILERSEND_API_KEY");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mailerSendToken);

            var payload = new
            {
                from = new
                {
                    email = "no-reply@mister-bills.com",
                    name = fromName
                },
                to = new[]
                {
            new
            {
                email = toEmail,
                name = toName
            }
        },
                template_id = templateId,
                subject = subject,
                personalization = new[]
                {
            new
            {
                email = toEmail,
                data = variables
            }
        }
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.mailersend.com/v1/email", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send email: {error}");
            }
        }



        public async Task ConfirmEmailAsync(User user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = System.Net.WebUtility.UrlEncode(code);

            var confirmationLink = $"https://app.mister-bills.com/email_confirmation/{user.Id}/{encodedCode}";

            var templateId = Environment.GetEnvironmentVariable("MAILERSEND_EMAIL_CON_TEMPLATE_ID");

            var variables = new Dictionary<string, object>
    {
        { "name", user.FullName },
        { "confirmation_link", confirmationLink }
    };

            await SendEmailTemplateAsync(user.Email, user.FullName, "MrBills", "Confirm your email", templateId, variables);
        }


        // I commit this code becuase I have not accsess for two template

        //    public async Task ForgotPasswordAsync(User user)
        //    {
        //        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        //        var encodedCode = System.Net.WebUtility.UrlEncode(code);

        //        var resetLink = $"https://app.mister-bills.com/reset_password/{user.Id}/{encodedCode}";

        //        var templateId = Environment.GetEnvironmentVariable("MAILERSEND_PASSWORD_RESET_TEMPLATE_ID");

        //        var variables = new Dictionary<string, object>
        //{
        //    { "name", user.FullName },
        //    { "reset_link", resetLink }
        //};

        //        await SendEmailTemplateAsync(user.Email, user.FullName, "MrBills", "Reset your password", templateId, variables);
        //    }


        // bellow code I am sending direct html code to your useing mailerSend without template
        public async Task ForgotPasswordAsync(User user)
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedCode = System.Net.WebUtility.UrlEncode(code);

            var resetLink = $"https://app.mister-bills.com/reset_password/{user.Id}/{encodedCode}";

            var subject = "Reset Your Password - MrBills";
            var htmlBody = BuildPasswordResetHtml(user.FullName, resetLink);

            await SendEmailAsync(user.Email, subject, htmlBody);
        }

        private string BuildPasswordResetHtml(string fullName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html lang='en' dir='ltr'>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <meta name='viewport' content='width=device-width, initial-scale=1, user-scalable=yes'>
    <meta name='format-detection' content='telephone=no, date=no, address=no, email=no, url=no'>
    <meta name='x-apple-disable-message-reformatting'>

    <style>
        /* Reset */
        html, body {{
            margin: 0 !important;
            padding: 0 !important;
            width: 100% !important;
            background-color: #f9f9f9;
            font-family: 'Inter', sans-serif;
        }}
        table {{ border-collapse: collapse; }}
        img {{ display: block; border: none; }}

        /* Mobile */
        @media all and (max-width:639px) {{
            .container {{ width: 100% !important; padding: 0 20px !important; }}
            .img-responsive {{ width: 100% !important; height: auto !important; }}
            .fullwidth-btn {{ width: 100% !important; }}
        }}
    </style>
</head>

<body>
    <div class='document' style='max-width: 640px; margin: auto; background: #ffffff;'>

        <!-- Header -->
        <table width='100%' bgcolor='#ffc107'>
            <tr>
                <td style='padding: 40px 50px 20px 50px; text-align: left;'>
                    <h1 style='margin: 0; font-size: 42px; color: #111; font-weight: bold;'>Forgot your</h1>
                    <h1 style='margin: 0; font-size: 42px; color: #111; font-weight: bold;'>password?</h1>
                </td>
            </tr>
        </table>

        <!-- Body -->
        <table width='100%' bgcolor='#ffffff'>
            <tr>
                <td class='container' style='padding: 40px 50px; text-align: center;'>
                    <img src='https://bucket.mailersendapp.com/z3m5jgr8emldpyo6/eqvygm0ozydl0p7w/images/9f7120f9-e2a0-4927-9b9c-c59dbe204e48.png' width='180' class='img-responsive' alt='Reset Icon'>
                    
                    <p style='font-size: 16px; color: #111; line-height: 1.6; margin-top: 20px;'>
                        Hello <strong>{fullName}</strong>,<br>
                        If you requested a password reset, click the button below.<br>
                        If you didn’t request this, you can safely ignore this email.
                    </p>

                    <a href='{resetLink}' 
                       style='display: inline-block; background: #ffc107; color: #111; padding: 14px 28px; 
                              border-radius: 6px; text-decoration: none; font-weight: 600; margin-top: 20px;' 
                       class='fullwidth-btn'>
                        Reset Password
                    </a>
                </td>
            </tr>
        </table>

        <!-- Footer -->
        <table width='100%' bgcolor='#f9f9f9'>
            <tr>
                <td style='padding: 30px 50px; text-align: center; color: #777; font-size: 12px;'>
                    © Mister Bills — All rights reserved.
                </td>
            </tr>
        </table>

    </div>
</body>
</html>";
        }


        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var apiKey = Environment.GetEnvironmentVariable("MAILERSEND_API_KEY");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var payload = new
            {
                from = new { email = "no-reply@mister-bills.com", name = "MrBills" },
                to = new[] { new { email = toEmail } },
                subject = subject,
                html = htmlBody
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            await client.PostAsync("https://api.mailersend.com/v1/email", content);
        }

        // the code I use it end here 

    }
}
