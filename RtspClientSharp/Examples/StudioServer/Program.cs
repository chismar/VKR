using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StudioServer
{
    public class Program
    {
        public static void Main(string[] args)
        {

            DownloadController.SController = new GStreamerController();
            CreateHostBuilder(args).Build().Run();
        }
        public static void RunServer(IStreamerController controller)
        {
            DownloadController.SController = controller;
            CreateHostBuilder(new string[] { }).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
    [Route("api/download_file")]
    public class DownloadController : Controller
    {
      
        public static IStreamerController SController;
        [HttpGet]
        public async Task<IActionResult> Download([FromQuery] string fileName)
        {
            var files = await SController.GetFilesForSession(SController.CurrentSession);
            var path = files.FirstOrDefault(x => x.Contains(fileName));
            if (path == null)
                return Accepted();
            //var path = @"C:\Users\Виталий\AppData\Local\Packages\CanonicalGroupLimited.UbuntuonWindows_79rhkp1fndgsc\LocalState\rootfs\home\studio\videos\studio-2020-01-27-16#003A56#003A43-0-TestSession.avi";
            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return File(memory, GetMimeTypes()[ext], fileName + ".mkv"/* Path.GetFileName(path)*/);
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
        {
            {".txt", "text/plain"},
            {".pdf", "application/pdf"},
            {".doc", "application/vnd.ms-word"},
            {".docx", "application/vnd.ms-word"},
            {".png", "image/png"},
            {".jpg", "image/jpeg"},
            {".avi", "video/avi" },
            {".mkv", "video/mkv" }
        };
        }
    }
}
