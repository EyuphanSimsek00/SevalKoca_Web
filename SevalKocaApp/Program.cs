var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Klasik oturum (Session) hafızasını aktif ediyoruz
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(20); // 20 dakika işlem yapılmazsa oturum kapanır
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// 1. Rota mekanizması çalışır
app.UseRouting();

// 2. OTURUM (SESSION) BURADA DEVREYE GİRER (Kritik Ekleme)
app.UseSession();

// 3. Yetki kontrolü yapılır
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();