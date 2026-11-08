using BillMate.Data;
using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BillMate.Services
{
    public class ShopierService : IShopierService
    {
        private readonly string _apiSecret;
        private readonly string _apiKey;
        private readonly ApplicationDbContext _db;

        public ShopierService(ApplicationDbContext db)
        {
            _apiSecret = Environment.GetEnvironmentVariable("SHOPIER_API_SECRET");
            _apiKey = Environment.GetEnvironmentVariable("SHOPIER_API_KEY");
            if (string.IsNullOrEmpty(_apiSecret) || string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Shopier API Key or Secret is not configured in environment variables.");
            }
            _db = db;
        }



        private string GetProductNameFromPackage(SubscriptionPackage? package)
        {
            return package switch
            {
                SubscriptionPackage.Small_3Users => "Small_3Users",
                SubscriptionPackage.Medium_5Users => "Medium_5Users",
                SubscriptionPackage.Large_7Users => "Large_7Users",
                _ => "Unknown Package"
            };
        }

        public string GenerateShopierForm(int groupId, SubscriptionDTO data, string userId)
        {
            var user = _db.User.Where(u => u.Id == userId).FirstOrDefault();
            if (user == null)
            {
                throw new Exception("User not found.");
            }



            string apiKey = _apiKey;
            string secret = _apiSecret;
            string userRegistered = DateTime.UtcNow.ToString("yyyy.MM.dd");
            int buyerAccountAge = 0;
            var orderId = "ORD-" + Guid.NewGuid().ToString("N").Substring(0, 8);


            int currency = 0;
            string productInfo = GetProductNameFromPackage(data.Package);
            int productType = 1;
            int currentLan = 0;
            string modulVersion = "1.0.4";
            Random rnd = new Random();
            int randomNumber = rnd.Next(1000000, 9999999);

            string amount = "0";
            switch (productInfo)
            {
                case "Small_3Users":

                    amount = "30";
                    break;
                case "Medium_5Users":

                    amount = "50";
                    break;
                case "Large_7Users":
                    amount = "70";
                    break;
                default:
                    // Handle unknown package
                    throw new Exception("Invalid subscription package.");
            }

            var subscription = _db.Subscriptions.FirstOrDefault(s => s.GroupId == groupId && s.PaymentMethod == PaymentMethod.Shopier);

            if (subscription == null)
            {

                subscription = new Subscription()
                {
                    IsTrial = true,
                    PaymentState = 0,
                    OrderId = orderId,
                    GroupId = groupId,
                    Period = data.Period,
                    Package = data.Package,
                    StartTime = DateTime.UtcNow,
                    Type = SubscriptionType.Paid,
                    PriceAmount = decimal.Parse(amount),
                    PaymentMethod = PaymentMethod.Shopier,
                    RandomNumber = randomNumber.ToString(),
                    PriceCurrencyCode = currency.ToString(),
                    ExpiryTime = DateTime.UtcNow,
                };

                _db.Subscriptions.Add(subscription);
            }
            else if (subscription.PaymentState == 1)
            {
                subscription.IsTrial = false;
            }
            
                _db.SaveChanges();

            var args = new Dictionary<string, string>
            {
                ["API_key"] = apiKey,
                ["website_index"] = "2",
                ["platform_order_id"] = orderId,
                ["product_name"] = productInfo,
                ["product_type"] = productType.ToString(),
                ["buyer_name"] = user.FullName,
                ["buyer_surname"] = user.FullName,
                ["buyer_email"] = user.Email,
                ["buyer_account_age"] = buyerAccountAge.ToString(),
                ["buyer_id_nr"] = "0",
                ["buyer_phone"] = "null",
                ["billing_address"] = "Default Address",
                ["billing_city"] = "null City",
                ["billing_country"] = "TR",
                ["billing_postcode"] = "",
                ["shipping_address"] = "null Address",
                ["shipping_city"] = "null City",
                ["shipping_country"] = "TR",
                ["shipping_postcode"] = "",
                ["total_order_value"] = amount,
                ["currency"] = currency.ToString(),
                ["platform"] = "0",
                ["is_in_frame"] = "1",
                ["current_language"] = currentLan.ToString(),
                ["modul_version"] = modulVersion,
                ["random_nr"] = randomNumber.ToString()
            };

            string signatureData = args["random_nr"] + args["platform_order_id"] + args["total_order_value"] + args["currency"];
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
            byte[] hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signatureData));
            string signature = Convert.ToBase64String(hashBytes);
            args["signature"] = signature;

            var inputs = string.Join("", args.Select(kvp =>
                $"<input type='hidden' name='{kvp.Key}' value='{System.Net.WebUtility.HtmlEncode(kvp.Value)}'/>"
            ));

            string html = @$"
        <html>
        <!doctype html>
        <head>
            <meta charset='UTF-8'>
            <meta content='True' name='HandheldFriendly'>
            <meta http-equiv='X-UA-Compatible' content='IE=edge'>
            <meta name='robots' content='noindex, nofollow, noarchive' />
            <meta name='viewport' content='width=device-width, initial-scale=1, user-scalable=0' />
            <title>Güvenli Ödeme Sayfası</title>
        </head>
        <body>
            <form action='https://www.shopier.com/ShowProduct/api_pay4.php' method='post' id='shopier_payment_form'>
                {inputs}
            </form>
            <script>document.getElementById('shopier_payment_form').submit();</script>
        </body>
        </html>";

            return html;
        }





        public async Task<bool> ProcessCallbackAsync(ShopierCallbackDTO dto)
        {

            if (dto.Status.ToLower() != "success")
                return false;

            return true;
        }





        public async Task<bool> ProcessOsbCallbackAsync(ShopierOsbData data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.email))
                {
                    return false;
                }
                var user = await _db.Users.FirstOrDefaultAsync(u=> u.Email == data.email);

                if (user == null)
                    return false;

                var subscription = await _db.Subscriptions
                    .FirstOrDefaultAsync(s => s.GroupId == user.GroupId);

                if (subscription == null) return false;


  
                subscription.IsActive = true;
                subscription.PaymentState = 1; // paid State
                subscription.ExpiryTime = subscription.Period == BillingPeriod.Monthly ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddYears(1);
                subscription.OrderNumber = data.orderid;



                if (decimal.TryParse(data.price, out var price))
                {
                    subscription.PriceAmount = price;
                }

                subscription.PriceCurrencyCode = GetCurrencyCode(data.currency);

                if (subscription.StartTime == default)
                {
                    subscription.StartTime = DateTime.UtcNow;
                }

                // حفظ التغييرات في قاعدة البيانات
                int changesCount = await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string GetCurrencyCode(int currencyCode)
        {
            return currencyCode switch
            {
                0 => "TRY", // الليرة التركية
                1 => "USD", // الدولار الأمريكي
                2 => "EUR", // اليورو
                _ => "TRY"  // افتراضي
            };
        }

    }
}
