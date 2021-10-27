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
    public class JsonObject 
    {
        public string Taskbody;
        public string Upload;
        public static List<JsonObject> Cache = new List<JsonObject>();
    }

    public class Startup
    {
        public static void AddToCache(JsonObject Json)
        {
            JsonObject.Cache.Capacity = 5;
            foreach(JsonObject J in JsonObject.Cache)
            {
                if(J.Equals(Json))
                {
                    Console.WriteLine("Nothing replaced in cache, continue.");
                    return;
                }
            }
            JsonObject.Cache.Insert(0, Json);
            JsonObject.Cache.TrimExcess();
            Console.WriteLine("Cache updated.");
        }
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
                endpoints.MapPost("/api/upload", async context =>
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
                        using(StreamWriter sw = new StreamWriter($"uploads/{FileName}.json"))
                        {
                            _ = sw.WriteLineAsync("{\n" + 
                                                  "\"Taskbody\":" + $"\"{await Body}\",\n" +
                                                  "\"Upload\":" + $"\"{DateTime.Now}\"\n" +
                                                  "}");
                        }
                    });   
                });

                endpoints.MapGet("/api/tasks/{file:required}", async context => {
                    var FileName = context.Request.RouteValues["file"];
                    Task<string> Contains;
                    try
                    {
                        using(StreamReader sr = new ($"uploads/{FileName}.json"))
                        {
                            Contains = sr.ReadToEndAsync();
                            _ = context.Response.WriteAsJsonAsync($"{await Contains}");
                        }
                    }
                    catch
                    {
                       _ = context.Response.WriteAsync($"No file exists with name '{FileName}'");
                    }
                });

                endpoints.MapGet("/api/tasks/{file:required}/{key:required}", async context => {
                    var FileName = context.Request.RouteValues["file"];
                    var searchKey = context.Request.RouteValues["key"];
                    try
                    {
                        using(StreamReader sr = new ($"uploads/{FileName}.json"))
                        {
                            string Contains = await sr.ReadToEndAsync();
                            JsonObject Deserialised = JsonSerializer.Deserialize<JsonObject>(Contains);
                            _ = context.Response.WriteAsync($"{Deserialised.Taskbody} {Deserialised.Upload}");
                            AddToCache(Deserialised);
                        }
                    }
                    catch
                    {
                       _ = context.Response.WriteAsync($"No file exists with name '{FileName}'");
                    }
                });

                endpoints.MapDelete("/api/tasks/{file:required}", async context => {
                    var FileName = context.Request.RouteValues["file"];
                    _ = context.Response.WriteAsync($"Trying to delete '{FileName}'...");
                    try
                    {
                        File.Delete($"uploads/{FileName}");
                        _ = context.Response.WriteAsync($"'{FileName}' was successfully deleted.");
                    }
                    catch
                    {
                       _ = context.Response.WriteAsync($"No file exists with name '{FileName}'.");
                    }
                });
            });
        }
    }
}
