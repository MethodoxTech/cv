
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Linq;

namespace ChangeVersion.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<ApiKeyAuthenticationOptions>("ApiKey", builder.Configuration.GetSection("ApiKeySettings"));
            builder.Services.AddAuthentication("ApiKey")
                .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });
            builder.Services.AddAuthorization();
            builder.Services.Configure<JsonOptions>(opts =>
            {
                opts.SerializerOptions.WriteIndented = true;
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            WebApplication app = builder.Build();

            string storageRoot = builder.Configuration["Storage:RootFolder"]!;
            Directory.CreateDirectory(storageRoot);

            // app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            // List all files
            app.MapGet("/files", (HttpContext ctx) =>
            {
                System.Collections.Generic.List<string> files = Directory
                    .EnumerateFiles(storageRoot, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(storageRoot, f).Replace("\\", "/"))
                    .OrderBy(p => p)
                    .ToList();
                return Results.Ok(files);
            })
            .RequireAuthorization();

            // Download a file
            app.MapGet("/files/{**path}", (string path) =>
            {
                string abs = Path.Combine(storageRoot, path);
                if (!File.Exists(abs)) return Results.NotFound();
                return Results.File(File.OpenRead(abs), contentType: "application/octet-stream");
            })
            .RequireAuthorization();

            // Upload or overwrite a file
            app.MapPut("/files/{**path}", async (HttpRequest req, string path) =>
            {
                string abs = Path.Combine(storageRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
                await using FileStream fs = File.Create(abs);
                await req.Body.CopyToAsync(fs);
                return Results.Ok();
            })
            .RequireAuthorization();

            // (Optional) Delete a file
            app.MapDelete("/files/{**path}", (string path) =>
            {
                string abs = Path.Combine(storageRoot, path);
                if (File.Exists(abs))
                {
                    File.Delete(abs);
                    return Results.NoContent();
                }
                return Results.NotFound();
            })
            .RequireAuthorization();

            app.Run();
        }
    }
}
