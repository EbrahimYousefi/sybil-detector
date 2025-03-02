using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using SybilDetection.UI.ViewModels.AppSettings;
using SybilDetection.UI.Helper.ReadData.CsvReaders;
using SybilDetection.UI.Helper.ReadData.FileMapper;
using SybilDetection.UI.Helper.ReadData.ListConverter;
using SybilDetection.UI.Helper.ReadData.StartupServices;
using SybilDetection.UI.Helper.APIHelper.ENS;
using SybilDetection.UI.Helper.TaskHelper;
using SybilDetection.UI.Helper.APIHelper.Dune;

namespace SybilDetection.UI
{
    public class Startup
    {
        public Startup(
            IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.Configure<AppSettingsViewModel>(Configuration);

            services.AddHostedService<MidnightTaskService>();

            services.AddSingleton<IStartupServicesAppHelper, StartupServicesAppHelper>();
            services.AddTransient<ICsvReaderHelper, CsvReaderHelper>();
            services.AddTransient<IFileMapperHelper, FileMapperHelper>();
            services.AddTransient<IAddressListConverter, AddressListConverter>();
            services.AddTransient<IENSServices, ENSServices>();
            services.AddTransient<IDuneApiService, DuneApiService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            app.Use(async (ctx, next) =>
            {
                await next();

                if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
                {
                    string originalPath = ctx.Request.Path.Value;
                    ctx.Items["originalPath"] = originalPath;
                    ctx.Request.Path = "/error/404";
                    await next();
                }
            });

            using (var scope = serviceProvider.CreateScope())
            {
                var runAppService = scope.ServiceProvider.GetRequiredService<IStartupServicesAppHelper>();

                runAppService.StartApp();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "scrollCheckerRouting",
                    "scroll/sybil-checker",
                    defaults: new { controller = "Scroll", action = "Index" });
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "scrollOverviewRouting",
                    "scroll/overview",
                    defaults: new { controller = "Scroll", action = "Overview" });
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}