using HomeWork4Products.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Налаштування культури
var defaultCulture = new CultureInfo("uk-UA");
var supportedCultures = new[]
{
    new CultureInfo("uk-UA"),
    new CultureInfo("en-US")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Налаштовуємо підключення до бази даних
builder.Services.AddDbContext<ProductContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("productContext")
        ?? throw new InvalidOperationException("Connection string 'productContext' not found.")));

builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("productContext")));

// Додаємо Identity з підтримкою ролей та бази даних
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true; // Підтвердження email
    options.Password.RequireDigit = false;      // Мінімальні вимоги до паролю
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<UserContext>()
.AddDefaultTokenProviders();

// Додаємо JWT аутентифікацію для API
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
})
.AddCookie(options =>
{
    options.LoginPath = "/User/Auth";
    options.LogoutPath = "/User/Logout";
    options.AccessDeniedPath = "/Products/Index";
});


// Налаштування CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("*", policy =>
    {
        policy.WithOrigins("http://localhost:4200")  // Дозволяє запити з будь-якого домену
              .AllowAnyHeader()  // Дозволяє будь-які заголовки
              .AllowAnyMethod();  // Дозволяє будь-які HTTP методи

    });
});



// Реєструємо сервіс для продуктів
builder.Services.AddScoped<IServiceProducts, ServiceProducts>();

// Додаємо підтримку контролерів і переглядів
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Ініціалізуємо базу даних
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
SeedData.Initialize(services);

// Налаштування pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Products/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization(localizationOptions);

// Додаємо підтримку CORS перед маршрутизацією
app.UseCors("*");
app.UseRouting();

// Додаємо автентифікацію та авторизацію
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

app.Run();
