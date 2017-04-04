﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudyAspDotNetCore.Middleware
{
    public class RequestIPMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestIPMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<RequestIPMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("哈哈哈开始了 User IP: " + context.Connection.RemoteIpAddress.ToString());
            await _next.Invoke(context);
            _logger.LogInformation("哈哈哈结束了");
        }
    }
}
