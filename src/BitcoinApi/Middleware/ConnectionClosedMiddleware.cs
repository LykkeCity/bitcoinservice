using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.AspNetCore.Http;

namespace BitcoinApi.Middleware
{
    public class ConnectionClosedMiddleware
    {
        private readonly RequestDelegate _next;

        public ConnectionClosedMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers.Add("Connection", "close");
            await _next(context);
        }
    }
}
