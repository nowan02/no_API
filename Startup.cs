using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace no_API
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/upload", async context =>
                {
                    string FileName = "";

                    Random Rnd = new Random();
                    for(int i = 0; i < 10; i++) 
                    {
                        FileName += $"{Rnd.Next(0,101)}";
                    }
                    
                    _ = context.Response.WriteAsync($"Filename of upload: {FileName}\n");

                    Task<string> Body;
                    using(StreamReader sr = new StreamReader(context.Request.Body, true))
                    {
                        Body = sr.ReadLineAsync();
                    }

                    Task Writer = Body.ContinueWith(async Writing => {
                        using(StreamWriter sw = new StreamWriter(FileName))
                        {
                            _ = sw.WriteLineAsync($"{await Body}");
                        }
                    });   
                });
            });
        }
    }
}
