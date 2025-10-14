using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Server.DAL;
using ChatApp.Server.Services;
using ChatApp.Server.Models;
using ChatApp.Server.Filters;
using ChatApp.Server.Middlewares;
using ChatApp.Server.Hub;


var builder = WebApplication.CreateBuilder(args);

// --- DAL & Services ---
var connectionString = builder.Configuration.GetConnectionString("ChatDb") 
    ?? throw new InvalidOperationException("Connection string 'ChatDb' not found.");
    
var dal = new SqlServerDAL(connectionString);
builder.Services.AddSingleton(dal);
builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<ChatHubService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<JwtAuthorizationFilter>();

// allows JwtService access the same key from JwtSettings used for authentication
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// customize how model validation errors are returned
builder.Services.Configure<ApiBehaviorOptions>(option =>
{
    // runs when model validation fails
    option.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values  // each value corresponds to a field
        .SelectMany(v => v.Errors)              // flattens multiple errors per field
        .Select(e => e.ErrorMessage)            // extract the error message string
        .ToArray();

        // return custom 400 bad request response in JSON format
        return new BadRequestObjectResult(new
        {
            message = "Request Failed",
            errors
        });
    };
});


// register the custom hub exception filter 
builder.Services.AddSignalR(options =>
{
    options.AddFilter<HubExceptionFilter>();
});


// --- CORS ---
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins("http://localhost:4173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// --- Controllers & SignalR ---
builder.Services.AddControllers();

var app = builder.Build();

// --- Middlewares ---
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthorization();


app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();
