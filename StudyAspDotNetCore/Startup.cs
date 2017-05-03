using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudyAspDotNetCore.Extensions;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;

namespace StudyAspDotNetCore
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            //目录浏览
            services.AddDirectoryBrowser();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //解决控制台输入乱码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //日志
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            //ip中间件
            app.UseRequestIP();



            //启用默认页
            //UseDefaultFiles 必须在 UseStaticFiles 之前调用。UseDefaultFiles 只是重写了 URL，而不是真的提供了这样一个文件。你必须开启静态文件中间件（UseStaticFiles）来提供这个文件。
            DefaultFilesOptions option = new DefaultFilesOptions();
            option.DefaultFileNames.Clear();
            option.DefaultFileNames.Add("MyDefault.html");
            app.UseDefaultFiles(option);

            //启用静态文件
            app.UseStaticFiles();


            //MIME映射修改
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".cs"] = "text/plain";//wwwroot下的.cs可以访问了
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot")),
                ContentTypeProvider = provider
            });


            //开放wwwroot以外的文件夹,访问静态文件,但是访问不了里面的.cs文件.?
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), @"MyStaticFiles")),
                RequestPath = new PathString("/StaticFiles"),
                ServeUnknownFileTypes = true,//开启不能识别的文件访问,不加默认404,这样指定文件夹下.cs文件可以作为文本浏览了
                DefaultContentType = "text/plain"

            });

            //允许直接浏览目录
            app.UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"MyStaticFiles")),
                RequestPath = new PathString("/StaticFiles")
            });

            #region snippet_AppRun
            app.Run(async (context) =>
            {
                if (context.Request.Query.ContainsKey("throw"))
                {
                    throw new Exception("Exception triggered!");
                }
                var builder = new StringBuilder();
                builder.AppendLine("<html><body>Hello World!");
                builder.AppendLine("<ul>");
                builder.AppendLine("<li><a href=\"/?throw=true\">Throw Exception</a></li>");
                builder.AppendLine("<li><a href=\"/missingpage\">Missing Page</a></li>");
                builder.AppendLine("</ul>");
                builder.AppendLine("</body></html>");

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(builder.ToString());
            });
            #endregion

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });


        }
    }
}
