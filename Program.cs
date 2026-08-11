using Microsoft.EntityFrameworkCore;
using RescueLink.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// GET all cases
app.MapGet("/api/cases", async (AppDbContext db) =>
{
    var cases = await db.Cases.ToListAsync();
    return Results.Ok(cases);
});

// GET a single case by id
app.MapGet("/api/cases/{id}", async (int id, AppDbContext db) =>
{
    var singleCase = await db.Cases.FindAsync(id);
    return singleCase is not null ? Results.Ok(singleCase) : Results.NotFound();
});

// POST a new case
app.MapPost("/api/cases", async (Case newCase, AppDbContext db) =>
{
    newCase.CreatedAt = DateTime.UtcNow;
    newCase.Status = "Open";
    db.Cases.Add(newCase);
    await db.SaveChangesAsync();
    return Results.Created($"/api/cases/{newCase.Id}", newCase);
});

// PUT: claim a case
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