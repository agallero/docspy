using CommunityToolkit.WinUI.Controls;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocSpy
{
    internal class Server
    {
        public const string RootUrl = "http://localhost:5000";
        private WebApplication? host;
        
        public Action? UpdateUIServerStopped { get; set; }
        public Action? UpdateUIServerStarted { get; set; }
        public bool Stopped { get; set; } = true;

        public static Server Instance { get; } = new Server();

        public async Task StopServe()
        {

            if (host != null)
            {
                await host.StopAsync();
                host = null;
            }
            Stopped = true;
            UpdateUIServerStopped?.Invoke();
        }

        public async Task Serve()
        {
            await StopServe();
            var builder = WebApplication.CreateBuilder();
            host = builder.Build();

            host.MapGet("/ping", async context => await context.Response.WriteAsync("Hello World!"));
            host.MapGet("/not_found/index.html", async context => await context.Response.WriteAsync("Server not found!"));

            host.UseDefaultFiles(); // serves index.html by default
            foreach (var root in Config.Instance.Roots)
            {
                if (Directory.Exists(root.WebFolder))
                {
                    host.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(root.WebFolder),
                        RequestPath = "/" + root.Name
                    });
                }
                else
                {
                    host.MapGet("/" + root.Name, async context => await context.Response.WriteAsync($"{root.WebFolder} doesn't exist."));
                }
            }

            var serverTask = Task.Run(() =>
            {
                host.RunAsync(RootUrl);
            });
            Stopped = false;
            UpdateUIServerStarted?.Invoke();

        }


    }
}
