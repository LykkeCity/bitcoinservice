using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.AspNetCore.Http;

namespace BitcoinApi.Middleware
{
    public class GlobalLogRequestsMiddleware
    {
        private readonly List<string> _ignorePathes = new List<string>
        {
            "swagger",
            "isalive"
        };

        private readonly ILog _log;
        private readonly RequestDelegate _next;

        public GlobalLogRequestsMiddleware(RequestDelegate next, ILog log)
        {
            _log = log;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var dt = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();

            var request = await ReadRequest(context.Request.Body);

            if (_ignorePathes.Any(o => context.Request.Path.Value.Contains(o)))
            {
                await _next.Invoke(context);
                sw.Stop();
                return;
            }

            string response = await ReadResponse(context);

            sw.Stop();

            await _log.WriteInfoAsync("GlobalLogRequestsMiddleware", context.Request.Path, $"{sw.ElapsedMilliseconds}ms {response}", request, dt);
        }

        private async Task<string> ReadResponse(HttpContext context)
        {
            using (var buffer = new MemoryStream())
            {
                var stream = context.Response.Body;
                try
                {
                    context.Response.Body = buffer;

                    await _next.Invoke(context);

                    buffer.Seek(0, SeekOrigin.Begin);
                    using (var bufferReader = new StreamReader(buffer))
                    {
                        string body = await bufferReader.ReadToEndAsync();
                        buffer.Seek(0, SeekOrigin.Begin);
                        await buffer.CopyToAsync(stream);
                        return body;
                    }
                }
                finally
                {
                    context.Response.Body = stream;
                }
            }
        }

        private async Task<string> ReadRequest(Stream body)
        {
            using (var ms = new MemoryStream())
            {
                await body.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                body.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
