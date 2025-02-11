using System.Net;
using System.Text;

public class SwaggerAuthenticationMiddleware
{
    private readonly RequestDelegate next;
    private readonly IConfiguration config;

    public SwaggerAuthenticationMiddleware(RequestDelegate next, IConfiguration config)
    {
        this.next = next;
        this.config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            string authHeader = context.Request.Headers["Authorization"];
            if (authHeader != null && context.User.Identity != null && !context.User.Identity.IsAuthenticated)
            {
                await next.Invoke(context);
                return;
            }

            context.Response.Headers["WWW-Authenticate"] = "Basic";

            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await next.Invoke(context);
        }
    }

    private bool IsLocalRequest(HttpContext context)
    {
        if (context.Request.Host.Value.StartsWith("localhost:"))
            return true;

        //Handle running using the Microsoft.AspNetCore.TestHost and the site being run entirely locally in memory without an actual TCP/IP connection
        if (context.Connection.RemoteIpAddress == null && context.Connection.LocalIpAddress == null)
            return true;

        if (context.Connection.RemoteIpAddress != null && context.Connection.RemoteIpAddress.Equals(context.Connection.LocalIpAddress))
            return true;

        return IPAddress.IsLoopback(context.Connection.RemoteIpAddress);
    }
}