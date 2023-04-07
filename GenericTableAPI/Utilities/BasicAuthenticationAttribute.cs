using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class BasicAuthenticationAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.ContainsKey("Authentication"))
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new JsonResult("Permission denied!");
            return;
        }
        
        // Implement your user validation logic here (e.g., check the database)
        bool isValidUser = true;

        if (isValidUser) return;
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.HttpContext.Response.Headers.Add("WWW-Authenticate", "Basic Scheme='yourRealm' location='yourUrl'");
        context.HttpContext.ForbidAsync();
    }
}
