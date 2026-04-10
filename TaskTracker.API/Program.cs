using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;
using TaskTracker.Infrastructure.Repositories;
using TaskTracker.Application.Features.Tasks.Commands.CreateTask;
using TaskTracker.Application.Mappings;
using TaskTracker.Application.Options;
using MediatR;
using FluentValidation;
using TaskTracker.Application.Behaviors;
using TaskTracker.API.Middlewares;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

//Adding EF core PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Options ───────────────────────────────────────────────────────────────────
// Binds "TaskDateRules" section from appsettings.json to TaskDateRulesOptions.
// IOptions<TaskDateRulesOptions> is now injectable anywhere in the application.
builder.Services.Configure<TaskDateRulesOptions>(
    builder.Configuration.GetSection(TaskDateRulesOptions.SectionName));

// Register MediatR handlers from Application Layer 
// take the assembly where CreateTaskCommand exists, and scan that whole assembly
// Since CreateTaskCommand is in TaskTracker.Application, MediatR scans the entire Application project assembly.
builder.Services.AddMediatR(typeof(CreateTaskCommand).Assembly);

// Register FluentValidation validators from Application Layer
builder.Services.AddValidatorsFromAssembly(typeof(CreateTaskCommand).Assembly);
//here CreateTaskCommand is used as a marker type to indicate the assembly where the validators are located. You can replace it with any other type from the same assembly if needed.
//since CreateTaskCommand is in the Application layer, it will ensure that all validators defined in that assembly are registered with the dependency injection container.

// Register pipeline behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

//Adding Mapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

//Register Domain Interfaces -> Infrastructure  implementation
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//cors configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();


app.UseSerilogRequestLogging(); // logs HTTP requests
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


try
{
    Log.Information("Application starting");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
