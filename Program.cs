using Tezgah.Data;
using Tezgah.Repositories;
using Tezgah.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireClaim("IsSuperAdmin", "true"));
    options.AddPolicy("CanCreateProjects", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim("IsSuperAdmin", "true") ||
            ctx.User.HasClaim("CanCreateProjects", "true")));
    options.AddPolicy("CanManageTasks", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim("IsSuperAdmin", "true") ||
            ctx.User.HasClaim("CanManageTasks", "true")));
});

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<GroupRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<TaskRepository>();
builder.Services.AddScoped<ProjectMemberRepository>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<CurrentUserService>();

var app = builder.Build();

// Init DB
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DapperContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    DbInitializer.Initialize(context, config);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
