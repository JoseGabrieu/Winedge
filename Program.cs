using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Winedge.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ================================================
// IGNORAR CERTIFICADOS SSL SEMPRE (DEV + PRODUÇÃO)
// ================================================
HttpClientHandler insecureHandler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};
// ================================================


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;

}).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Error/Forbidden";
    options.Cookie.Name = "Auth";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

}).AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.CallbackPath = builder.Configuration["Keycloak:CallbackPath"];

    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    // NUNCA VALIDAR HTTPS** (DEV + PRODUÇÃO)
    options.RequireHttpsMetadata = false;
    options.BackchannelHttpHandler = insecureHandler;

    options.UsePkce = false;
    options.TokenValidationParameters.ValidateIssuer = false;

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles"
    };

    options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            var identity = context.Principal.Identity as System.Security.Claims.ClaimsIdentity;
            var accessToken = context.TokenEndpointResponse?.AccessToken;

            Console.WriteLine("\n========== TOKENS RECEBIDOS DO KEYCLOAK ==========\n");

            if (identity != null && !string.IsNullOrEmpty(accessToken))
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(accessToken);

                foreach (var claim in jwt.Claims)
                    Console.WriteLine($" - {claim.Type}: {claim.Value}");

                var realmAccessClaim = jwt.Claims.FirstOrDefault(c => c.Type == "realm_access");

                if (realmAccessClaim != null)
                {
                    var realmAccess = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);

                    if (realmAccess.RootElement.TryGetProperty("roles", out var rolesElement))
                    {
                        foreach (var r in rolesElement.EnumerateArray())
                        {
                            var roleName = r.GetString();
                            if (roleName != null)
                            {
                                identity.AddClaim(new System.Security.Claims.Claim(
                                    options.TokenValidationParameters.RoleClaimType,
                                    roleName
                                ));
                            }
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    };
});

// ================================================
// Database
// ================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// ================================================
// Pipeline
// ================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
