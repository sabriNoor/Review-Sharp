using ReviewSharp.Interfaces;
using ReviewSharp.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register code review services and parser
builder.Services.AddScoped<ICodeParserService, CodeParserService>();
builder.Services.AddScoped<ICodeReviewService, NamingConventionService>();
builder.Services.AddScoped<ICodeReviewOrchestratorService, CodeReviewOrchestratorService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/CodeReview/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
