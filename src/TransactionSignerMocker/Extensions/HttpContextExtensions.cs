﻿using System;
using Microsoft.AspNetCore.Http;

namespace TransactionSignerMocker.Extensions
{
    public static class HttpContextExtensions
    {
        public static Uri GetUri(this HttpRequest request)
        {
            var hostComponents = request.Host.ToUriComponent().Split(':');

            var builder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = hostComponents[0],
                Path = request.Path,
                Query = request.QueryString.ToUriComponent()
            };

            if (hostComponents.Length == 2)
            {
                builder.Port = Convert.ToInt32(hostComponents[1]);
            }

            return builder.Uri;
        }

	    public static string GetUserAgent(this HttpRequest request)
	    {
		    return request.Headers["User-Agent"].ToString();
	    }
    }
}
