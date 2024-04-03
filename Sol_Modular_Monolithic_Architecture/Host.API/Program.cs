// श्री कृष्ण
// Developer Name: Kishor Naik
// Designation: Sr.Software Architect
// Co-Founder: Kishor Naik
using Frameworks.Aspnetcore.Library.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Read ConnectionString

// Add services to the container.
builder.AddSeriLogger(dbName: ConstantValue.SeriLogDbName);
builder.Services.AddControllers();
builder.Services.AddRequestTimeouts();

builder.Services.AddGzipResponseCompression(System.IO.Compression.CompressionLevel.Fastest);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddJwtInSwagger();
});

builder.Services.AddCustomCors("CORS");
builder.Services.AddCustomHealthChecks(builder.Configuration);

builder.Services.AddRequestDecompression();

builder.Services.AddHttpLogging(config =>
{
    config.LoggingFields = HttpLoggingFields.All;
});

builder.Services.AddCustomApiVersion();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

builder.Services.AddAntiforgery();

builder.Services.AddCustomSqlDistributedCache(builder.Configuration, ConstantValue.SqlCacheDbName, "dbo", "DbCache");

builder.Services.AddHangFireBackgroundJob(builder.Configuration, name: ConstantValue.HangFireDbName);

builder.Services.AddCustomRateLimit(RateLimitAlgorithmsEnum.SlidingWindow, "sliding",
    new RateLimitOptions(10, TimeSpan.FromSeconds(10), System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst, 30, 2)
);

builder.Services.AddCustomCoravel();

builder.Services.Configure<JwtAppSetting>(builder.Configuration.GetSection("JWT"));
JwtAppSetting jwtAppSetting = builder.Configuration.GetSection("JWT").Get<JwtAppSetting>();
builder.Services.AddJwtToken(jwtAppSetting, JwtPolicyRegisterExtension.GetRegisterPolicy());

// [FromRoute][FromQuery][FromBody] in one Model.
builder.Services.Configure<ApiBehaviorOptions>((options) => options.SuppressInferBindingSourcesForParameters = true);

//builder.Services.AddDistributedRedisCache(builder.Environment, "Test", new RedisConfig()
//{
//    DefaultDatabase = 0,
//    Password = "",
//    EndPoints = new List<RedisEndPoints> {
//       new RedisEndPoints()
//       {
//           Host="",
//           Port=1
//       },
//       new RedisEndPoints()
//       {
//           Host="",
//           Port=1
//       }
//   }
//});

builder.AddModules();

var app = builder.Build();

app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRequestDecompression();

app.UseCors("CORS");

app.UseCustomeExceptionHandler();

app.UseResponseCompression();

app.MapHealthChecks("/health", new HealthCheckOptions()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseApiKeyMiddleware(builder.Configuration);

app.UseSecurityHeadersMiddleware();

app.UseAntiforgery();

app.UseAuthorizeExceptionMiddleware();
app.UseJwtToken();

app.UseRateLimiter();

app.UseRequestTimeouts();

app.UseHangfireDashboard();
app.MapHangfireDashboard("/hangfire");

app.MapControllers();

app.Run();