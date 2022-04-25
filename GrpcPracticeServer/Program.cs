using GrpcPracticeServer;
using GrpcPracticeServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682



// Add services to the container.
builder.Services.AddGrpc();

/// 억세스 권한 부여 서비스 추가
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireClaim(ClaimTypes.Name);
    });
});

/// 인증 서비스 추가. JWT 전달자 처리기 추가
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateActor = false,
                ValidateLifetime = true,
                IssuerSigningKey = JwtHelper.SecurityKey //유효성 검사에 사용되는 암호화 토큰. 안전한 위치에 저장해야함.
            };
    });

var app = builder.Build();
//app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<StreamingService>();

app.MapGet("/generateJwtToken", async context =>
{
    await context.Response.WriteAsync(JwtHelper.GenerateJwtToken(context.Request.Query["name"]));
});

app.Run();

