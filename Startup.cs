using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// Testing Namespaces
using System.Reflection;

namespace no_API
{
    /// <summary>
    /// Class <c>JsonObject</c>: 
    /// The object the API uses to deserialise
    /// json files into, it includes the cache Dictionary
    /// <c>JsonObject.Cache</c>
    /// </summary>

    public class JsonObject 
    {
        /// <summary>
        /// <c>JsonObject.TaskBody</c>:
        /// The string representation of the task.
        /// </summary>
        public string Taskbody {get; set;}
        /// <summary>
        /// <c>JsonObject.Upload</c>
        /// The string representation of when the json was uploaded.
        /// </summary>
        public string Upload {get; set;}
        public static Dictionary<string,JsonObject> Cache = new Dictionary<string, JsonObject>();
    }

    public class Startup
    {
        ///<summary>
        /// <c>Startup.DeserialiserAsync()</c>:
        /// Asyncronous function that returns the deserialised json file as a 
        /// <c>JsonObject</c>
        ///</summary>.

        public static async Task<JsonObject> DeserialiserAsync(object File)
        {
            Task<string> Contains;

            using(StreamReader sr = new ($"uploads/{File}.json"))
            {
                Contains = sr.ReadToEndAsync();
                return JsonSerializer.Deserialize<JsonObject>($"{await Contains}");
            }
        }

        /// <summary>
        /// <c>Startup.SearchCacheAsync</c>:
        /// Checks the Cache for already deserialised jsons
        /// If its not in the cache, it deserialises using
        /// <c>Startup.DeserialiseAsync</c>
        /// then adds the new element.
        /// Since the capacity is specified, it is then 
        /// trimmed to remove the oldest JsonObject.
        /// If a file could not be deserialised or doesn't exist
        /// it returns an empty 
        /// <c>JsonObject</c>.
        /// </summary>

        public static async Task<JsonObject> SearchCacheAsync(object File)
        {
            try
            {
                JsonObject.Cache.EnsureCapacity(5);
                if(JsonObject.Cache.ContainsKey($"{File}"))
                {
                    Console.WriteLine("File already in cache, continue");
                    return JsonObject.Cache[$"{File}"];
                }

                JsonObject Deser = await DeserialiserAsync($"{File}");

                JsonObject.Cache.Add($"{File}", Deser);
                JsonObject.Cache.TrimExcess();
                Console.WriteLine("Cache updated.");
                return Deser;
            }
            catch
            {
                JsonObject error = new JsonObject();
                return error;
            }
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddApplicationPart(Assembly.Load("no_API"));
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

                endpoints.MapGet("/api/tasks/{file:required}", async context => 
                {
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

                endpoints.MapGet("/api/tasks/{file:required}/{key:required}", async context => 
                {
                    
                    var FileName = context.Request.RouteValues["file"];
                    var SearchKey = context.Request.RouteValues["key"];

                    JsonObject Json = await SearchCacheAsync(FileName);
                    if(Json.Taskbody == null)
                    {
                        _ = context.Response.WriteAsync($"No file named {FileName} exists");
                        return;
                    }

                    switch(SearchKey)
                    {
                        default:
                            _ = context.Response.WriteAsync($"No keys named {SearchKey} exist in the file.");
                            break;
                        case "Taskbody":
                            _ = context.Response.WriteAsync($"The searched key's value holds: {Json.Taskbody}");
                            break;
                        case "Upload":
                            _ = context.Response.WriteAsync($"The searched key's value holds: {Json.Upload}");
                            break;
                    }
                });

                endpoints.MapDelete("/api/tasks/{file:required}", async context => 
                {
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

                endpoints.MapControllers();
            });
        }
    }
}
