using TFB;
using TFB.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

TFB.Startup.GetConfigurations(builder.Services, builder.Configuration);

builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<PersonalitySheetsService>();
builder.Services.AddSingleton<IImportPersonalityService, ImportPersonalityService>();
builder.Services.AddSingleton<IPersonalityService, PersonalityService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();