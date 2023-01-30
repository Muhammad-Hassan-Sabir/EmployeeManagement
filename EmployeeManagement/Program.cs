using EmployeeManagement.Models;
using EmployeeManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddRazorPages();

//log configuration
builder.Logging.ClearProviders();
builder.Logging.AddNLog();
//Add Authorization Policy Globally
builder.Services.AddMvc(options =>
{
    var policy = new AuthorizationPolicyBuilder()
               .RequireAuthenticatedUser()
               .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EmployeeDBConnection"))
);
//configure password setting for user register
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredUniqueChars = 3;
    options.Password.RequiredLength = 10;
})
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthorization(option =>
{
    option.AddPolicy("DeleteRolePolicy", policy => policy.RequireClaim("Delete Role","true")
                                                                                /*.RequireClaim("Create Role")*/);

    option.AddPolicy("EditRoleClaimPolicy", policy => policy.RequireClaim("Edit Role", "true"));

    //option.AddPolicy("EditRolePolicy", policy => policy.RequireClaim("Edit Role","true"));

   //custom authorization policy using func
   //option.AddPolicy("EditRolePolicy", policy
   //                => policy.RequireAssertion(context =>
   //                (context.User.IsInRole("Admin") &&
   //                context.User.HasClaim(claim => claim.Type == "Edite Role" && claim.Value == "true")) ||
   //                context.User.IsInRole("Super Admin")));

   //Custom authorization requirement and handler

   option.AddPolicy("EditRolePolicy", policy =>
    {
        policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement());
    });


    option.AddPolicy("AdminRolePolicy", policy => policy.RequireRole("Admin"));

    //If you do not want the rest of the handlers to be called, 
    //when a failure is returned, set InvokeHandlersAfterFailure property to false.The default is true.
    //option.InvokeHandlersAfterFailure = false;

});
builder.Services.AddSingleton<IAuthorizationHandler,CanEditOnlyOtherAdminRolesAndClaimsHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();
builder.Services.AddAuthentication().AddGoogle(option =>
{
    option.ClientId = "XXXXXXXX";
    option.ClientSecret = "XXXXXX";
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseExceptionHandler("/Error");
    //// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
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