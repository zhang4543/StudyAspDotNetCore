using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace StudyAspDotNetCore.Extensions
{


//     小技巧：Asp.net Core如何获取访问者的IP地址：
// 如果asp.net core应用放在Jexus上运行，根据不同的托管方式，有如下办法获取客户端IP地址：
// 一，以AppHost方式运行：这种方式与在IIS上运行asp.net是相同的，用HttpContext.Connection.RemoteIpAddress就能得到访问者的地址，如果不行，可以从httpContext.Request.Headers["X-Original-For"].ToArray[0]中得到一个由“Ip:Port”组成的字串，从字串中可以得到客户端IP地址和端口；
// 二，以反代方式运行：这种方式，可以从Headers["X-Forwarded-For"] 或 Headers["X-Real-IP"]中获取。

// Headers["X-Forwarded-For"] || Headers["X-Real-IP"] || HttpContext.Connection.RemoteIpAddress || httpContext.Request.Headers["X-Original-For"].ToArray[0]
    /// <summary>
    /// 用于处理客户IP地址、端口的HostBuilder中间件
    /// </summary>
    public static class WebHostBuilderJexusExtensions
    {

        /// <summary>
        /// 启用JexusIntegration中间件
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseJexusIntegration(this IWebHostBuilder hostBuilder)
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            // 检查是否已经加载过了
            if (hostBuilder.GetSetting(nameof(UseJexusIntegration)) != null)
            {
                return hostBuilder;
            }


            // 设置已加载标记，防止重复加载
            hostBuilder.UseSetting(nameof(UseJexusIntegration), true.ToString());


            // 添加configure处理
            hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new JwsSetupFilter());
            });


            return hostBuilder;
        }

    }

    class JwsSetupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseMiddleware<JexusMiddleware>();
                next(app);
            };
        }
    }


    class JexusMiddleware
    {
        RequestDelegate _next;
        public JexusMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IISOptions> options)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var headers = httpContext.Request.Headers;

            try
            {
                if (headers != null && headers.ContainsKey("X-Original-For"))
                {
                    var ipaddAdndPort = headers["X-Original-For"].ToArray()[0];
                    var dot = ipaddAdndPort.IndexOf(":");
                    var ip = ipaddAdndPort;
                    var port = 0;
                    if (dot > 0)
                    {
                        ip = ipaddAdndPort.Substring(0, dot);
                        port = int.Parse(ipaddAdndPort.Substring(dot + 1));
                    }

                    httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
                    if (port != 0) httpContext.Connection.RemotePort = port;
                }
            }
            finally
            {
                await _next(httpContext);
            }

        }

    }



}
