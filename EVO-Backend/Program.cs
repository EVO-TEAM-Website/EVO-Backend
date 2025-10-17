using EVO_Backend.Data;
using EVO_Backend.Models;
using EVO_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DataProtection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("EVO-Backend");

// Mail
builder.Services.Configure<MailOptions>(builder.Configuration.GetSection("Mail"));
builder.Services.AddScoped<IEmailService, EmailService>();

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!));
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.Zero
    };
});

// Identity (Guid)
builder.Services.AddIdentityCore<ApplicationUser>(opt =>
{
    opt.Password.RequiredLength = 6;
    opt.Password.RequireDigit = true;
    opt.Password.RequireLowercase = true;
    opt.Password.RequireUppercase = false;
    opt.Password.RequireNonAlphanumeric = false;
    opt.User.RequireUniqueEmail = true;
    opt.SignIn.RequireConfirmedEmail = true;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// (لو عندك FallbackPolicy مفعّل، اتركه — السطرين تحت سيتجاوزان التفويض على swagger.json)
builder.Services.AddAuthorization();

builder.Services.AddControllers();

// CORS لواجهة Angular
const string AngularClient = "AngularClient";
builder.Services.AddCors(o => o.AddPolicy(AngularClient, p =>
    p.WithOrigins("http://localhost:4200", "https://localhost:4200")
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()
));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "EVO-Backend", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "أدخل: Bearer {token}"
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }}, Array.Empty<string>() }
    });
});

var app = builder.Build();

// ✅ فعّل swagger.json كـ Endpoint واسمح بالوصول له دون تفويض
app.MapSwagger().AllowAnonymous();

// UI
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EVO-Backend v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors(AngularClient);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seeding
await DbSeeder.SeedAsync(app.Services, builder.Configuration);

app.Run();
