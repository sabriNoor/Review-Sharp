using ReviewSharp.Interfaces;
using ReviewSharp.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register code review services and parser
builder.Services.AddScoped<ICodeParserService, CodeParserService>();
builder.Services.AddScoped<ICodeReviewOrchestratorService, CodeReviewOrchestratorService>();
builder.Services.AddScoped<ICodeReviewService, NamingConventionService>();
builder.Services.AddScoped<ICodeReviewService, SyntaxCheckService>();
builder.Services.AddScoped<ICodeReviewService, DiViolationService>();
builder.Services.AddScoped<ICodeReviewService, AsyncMethodNamingService>();
builder.Services.AddScoped<ICodeReviewService, DuplicateCodeService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/CodeReview/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Urls.Add("https://localhost:7054");
app.Urls.Add("http://localhost:5071");
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=CodeReview}/{action=Upload}/{id?}")
    .WithStaticAssets();


// Add route for CodeReview
app.MapControllerRoute(
    name: "codeReview",
    pattern: "CodeReview/{action=Upload}/{id?}");

app.Run();
