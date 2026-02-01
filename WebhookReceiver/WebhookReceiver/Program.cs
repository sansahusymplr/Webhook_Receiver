using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5102");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<HashSet<string>>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapPost("/webhooks/payment-succeeded", async (HttpContext context, HashSet<string> processedEvents) =>
{
    if (!context.Request.Headers.TryGetValue("X-Event-Id", out var eventId) || string.IsNullOrEmpty(eventId))
        return Results.BadRequest();

    lock (processedEvents)
    {
        if (processedEvents.Contains(eventId!))
            return Results.Ok(new { status = "duplicate_ignored" });
    }

    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var bodyBytes = Encoding.UTF8.GetBytes(body);

    if (!context.Request.Headers.TryGetValue("X-Signature", out var signature) || string.IsNullOrEmpty(signature))
        return Results.Unauthorized();

    var secret = Encoding.UTF8.GetBytes("shared-secret");
    var hash = HMACSHA256.HashData(secret, bodyBytes);
    var computedSignature = Convert.ToHexString(hash).ToLower();

    if (signature != computedSignature)
        return Results.Unauthorized();

    Console.WriteLine($"Event ID: {eventId}");
    Console.WriteLine($"Payload: {body}");

    lock (processedEvents)
    {
        processedEvents.Add(eventId!);
    }

    return Results.Ok();
});

app.Run();
