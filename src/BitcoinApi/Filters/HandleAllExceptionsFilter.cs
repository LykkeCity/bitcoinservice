using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BitcoinApi.Filters
{
    public class HandleAllExceptionsFilter : IExceptionFilter
    {
        private readonly ILog _logger;

        public HandleAllExceptionsFilter(ILog logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var controller = context.RouteData.Values["controller"];
            var action = context.RouteData.Values["action"];

            ApiException ex;

            var statusCode = 500;
            var exception = context.Exception as BackendException;
            if (exception != null)
            {
                ex = new ApiException
                {
                    Error = new ApiError
                    {
                        Code = exception.Code,
                        Message = exception.Text
                    }
                };
                statusCode = 400;
            }
            else
            {
                _logger.WriteErrorAsync("ApiException", "BitcoinService", $"Controller: {controller}, action: {action}", context.Exception);
                ex = new ApiException
                {
                    Error = new ApiError
                    {
                        Code = ErrorCode.Exception,
                        Message = "Internal server error. Try again."
                    }
                };
            }

            context.Result = new ObjectResult(ex)
            {
                StatusCode = statusCode,
                DeclaredType = typeof(ApiException)
            };
        }
    }

    public class ApiException
    {
        public ApiError Error { get; set; }
    }

    public class ApiError
    {
        public ErrorCode Code { get; set; }
        public string Message { get; set; }
    }
}
