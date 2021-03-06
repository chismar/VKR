using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using StudioServer.Data;

namespace StudioServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var sc = new StudioControl() { StreamerController = DownloadController.SController };
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<StudioControl>(sc);
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection(); 
            app.UseStaticFiles();
            if (DownloadController.SController is GStreamerController)
                app.UseStaticFiles();
            else
            {
                if(Directory.Exists(Path.Combine(env.ContentRootPath, "..\\..\\..\\..\\..\\StudioServer")))
                    app.UseStaticFiles(new StaticFileOptions()
                    {
                        FileProvider = new CompositeFileProvider(new[] {
                            new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "..\\..\\..\\..\\..\\StudioServer", "wwwroot")),
                            env.WebRootFileProvider
                        }
                        ),
                    
                        //RequestPath = "/wwwroot"
                    });
                else
                    app.UseStaticFiles(new StaticFileOptions()
                    {
                        FileProvider = new CompositeFileProvider(new[] {
                        new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "wwwroot")),
                        env.WebRootFileProvider
                    }
                    ),

                        //RequestPath = "/wwwroot"
                    });
            }
           
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapControllers();
            });
        }
    }
}
