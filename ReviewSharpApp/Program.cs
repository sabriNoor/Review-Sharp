using ReviewSharp.Interfaces;
using ReviewSharp.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register code review services and parser
builder.Services.AddScoped<ICodeParserService, CodeParserService>();
builder.Services.AddScoped<ICodeReviewService, NamingConventionService>();
builder.Services.AddScoped<ICodeReviewOrchestratorService, CodeReviewOrchestratorService>();
builder.Services.AddScoped<ICodeReviewService, SyntaxCheckService>();
builder.Services.AddScoped<ICodeReviewService, DuplicateCodeService>();
builder.Services.AddScoped<ICodeReviewService, UnusedSymbolService>();
builder.Services.AddScoped<ICodeReviewService, SwitchStatementService>();
builder.Services.AddScoped<ICodeReviewService, DiViolationService>();
builder.Services.AddScoped<ICodeReviewService, AsyncMethodNamingService>();
builder.Services.AddScoped<ICodeReviewService, NullCheckStyleService>();
builder.Services.AddScoped<ICodeReviewService, EmptyCatchService>();
builder.Services.AddScoped<ICodeReviewService, HardcodedSecretsService>();
builder.Services.AddScoped<ICodeReviewService, StringConcatInLoopService>();
builder.Services.AddScoped<ICodeReviewService, AsyncMethodBestPracticesService>();
builder.Services.AddScoped<ICodeReviewService, ClassAndMethodLengthService>();
builder.Services.AddScoped<ICodeReviewService, LinqInsufficientCheckService>();
builder.Services.AddScoped<ICodeReviewService, ManyMethodParametersService>();
builder.Services.AddScoped<ICodeReviewService, EmptyFinallyBlockService>();
builder.Services.AddScoped<ICodeReviewService, DefaultSwitchCaseMissingService>();
builder.Services.AddScoped<ICodeReviewService, PossibleNullReferenceService>();
builder.Services.AddScoped<ICodeReviewService, DuplicateLiteralService>();
builder.Services.AddScoped<ICodeReviewService, UnreachableCodeService>();
builder.Services.AddScoped<ICodeReviewSemanticService, BoxingUnboxingService>();
builder.Services.AddScoped<ICodeReviewSemanticService, BoxingUnboxingService>();
builder.Services.AddScoped<ICodeReviewSemanticService, UnusedUsingService>();
builder.Services.AddScoped<ICodeReviewService, FileNameMatchClassService>();
builder.Services.AddScoped<ICodeReviewService, NestedBlockDepthService>();
builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();
builder.Services.AddSingleton<ReviewSharp.Services.ReviewResultStorageService>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/CodeReview/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

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
