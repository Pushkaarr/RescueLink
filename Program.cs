using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RescueLink.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ---------- AUTH ENDPOINTS ----------

app.MapPost("/api/auth/signup", async (SignupRequest request, AppDbContext db) =>
{
    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (existingUser is not null)
        return Results.BadRequest("A user with this email already exists.");

    var user = new User
    {
        FullName = request.FullName,
        Email = request.Email,
        PhoneNumber = request.PhoneNumber,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Role = "User",
        CreatedAt = DateTime.UtcNow
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", new { user.Id, user.FullName, user.Email, user.Role });
});

app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddDays(7),
        signingCredentials: creds
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new { token = tokenString, user.Id, user.FullName, user.Email, user.Role });
});

// ---------- CASE ENDPOINTS ----------

app.MapGet("/api/cases", async (AppDbContext db) =>
{
    var cases = await db.Cases.ToListAsync();
    return Results.Ok(cases);
});

app.MapGet("/api/cases/{id}", async (int id, AppDbContext db) =>
{
    var singleCase = await db.Cases.FindAsync(id);
    return singleCase is not null ? Results.Ok(singleCase) : Results.NotFound();
});

app.MapPost("/api/cases", async (Case newCase, AppDbContext db) =>
{
    newCase.CreatedAt = DateTime.UtcNow;
    newCase.Status = "Open";
    db.Cases.Add(newCase);
    await db.SaveChangesAsync();
    return Results.Created($"/api/cases/{newCase.Id}", newCase);
});

app.MapPut("/api/cases/{id}/claim", async (int id, string claimedByUserId, AppDbContext db) =>
{
    var existingCase = await db.Cases.FindAsync(id);
    if (existingCase is null) return Results.NotFound();

    if (existingCase.Status == "Claimed")
        return Results.BadRequest("This case is already claimed.");

    existingCase.Status = "Claimed";
    existingCase.ClaimedByUserId = claimedByUserId;
    existingCase.ClaimedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(existingCase);
});

app.Run();

record SignupRequest(string FullName, string Email, string PhoneNumber, string Password);
record LoginRequest(string Email, string Password);