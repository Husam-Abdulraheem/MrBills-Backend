using BillMate.Data;
using BillMate.Interfaces;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Cms;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class MailerSendService : IMailerSendService
{
    private readonly string apiKey = Environment.GetEnvironmentVariable("MAILERSEND_API_KEY");
    private readonly string apiUrl = "https://api.mailersend.com/v1/email";
    private readonly ApplicationDbContext _db;
    private readonly HttpClient _client;
    private readonly ILogger<MailerSendService> _logger;


    public MailerSendService(ApplicationDbContext db, HttpClient client, ILogger<MailerSendService> logger)
    {
        _db = db;
        _client = client;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _logger = logger;
    }
    public async Task SendEmailAsync()
    {
        var recipients = await _db.Users.Select(r=> new { r.Email, Name = r.FullName}).ToListAsync();



        foreach (var recipient in recipients)
        {
            var payload = new
            {
                from = new { email = "no-reply@mister-bills.com", name = "MrBills" },
                to = new[] { new { email = recipient.Email, name = recipient.Name } },
                template_id = "jpzkmgqyk5vl059v",
                subject = $"عزيزي {recipient.Name} إكتشف تحديثات حسابك الجديدة على مستر فواتير",
                personalization = new[]
                {
                    new
                    {

                        email = recipient.Email,
                        data = new Dictionary<string, string>
                        {
                            { "name", recipient.Name }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Failed to send email to {recipient.Email}. Error: {error}");
            }
            else
            {
                _logger.LogInformation($"Email sent successfully to {recipient.Email}");
            }
        }
    
    



    }
}