using BillMate.Data;
using BillMate.Interfaces;
using BillMate.Middleware;
using BillMate.Models;
using BillMate.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using DotNetEnv;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

Env.Load();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions{
    Args = args,
    EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
});

if (!builder.Environment.IsDevelopment())
{
builder.WebHost.UseUrls("http://*:3000");

}



builder.Services.AddHealthChecks();
//builder.Configuration.AddEnvironmentVariables();

//var cors = Environment.GetEnvironmentVariable("CORS");
//var databaseUrl = builder.Configuration["ConnectionStrings:DefaultConnection"];
//var jwtKey = builder.Configuration["Jwt:Key"];
//var smtpPassword = builder.Configuration["Smtp:Password"];
//var issuer = builder.Configuration["Jwt:Issuer"];




var serviceAccountJsonPath = builder.Configuration["GooglePlay:ServiceAccountJsonPath"];
var fullPath = Path.Combine(AppContext.BaseDirectory, serviceAccountJsonPath);
var packageName = "com.mrbills.app";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                      policy.WithOrigins($"https://app.mister-bills.com", "http://localhost:5173", "https://localhost")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                      });
});



// Add services to the container.
builder.Services.AddScoped<IGooglePlayBillingService>(provider =>
{
    var db = provider.GetRequiredService<ApplicationDbContext>();
    var logger = provider.GetRequiredService<ILogger<GooglePlayBillingService>>();
    return new GooglePlayBillingService(fullPath, packageName, db, logger);
});
builder.Services.AddScoped<IShopierService, ShopierService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddHostedService<BackgroundServices>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSignalR();
builder.Services.AddHttpClient<IMailerSendService, MailerSendService>();


var connectionString= "";

if (builder.Environment.IsDevelopment())
{
    connectionString = Environment.GetEnvironmentVariable("DATABASE_URL_STAGE");
}

if (builder.Environment.IsStaging())
{
    connectionString = Environment.GetEnvironmentVariable("DATABASE_URL_STAGE");
}

if (builder.Environment.IsProduction())
{
    connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DATABASE_URL' is not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));


builder.Services.AddControllers().AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;


})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
        IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET"))),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        
    };
    //o.Events = new JwtBearerEvents
    //{
    //    OnMessageReceived = context =>
    //    {
    //        context.Token = context.Request.Cookies["token"];
    //        return Task.CompletedTask;
    //    }
    //};
});

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    });

// Swagger configuration

builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "MrBills App", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10MB
});

// Add order rate
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(5),
                QueueLimit = 10 
            }
        ));
});


var app = builder.Build();

app.MapHealthChecks("/health");


app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    try
    {
        await DataSeeder.SeedRolesAsync(roleManager);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding roles.");
    }
}
// how can i use the DataSeeder class in the Program.cs file?
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseStaticFiles();

var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}


app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
