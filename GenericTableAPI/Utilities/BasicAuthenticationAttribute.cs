using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
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
            context.Result = new JsonResult("Permission denined!");
            return;
        }
        
        // Implement your user validation logic here (e.g., check the database)
        bool isValidUser = true;

        if (!isValidUser)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.HttpContext.Response.Headers.Add("WWW-Authenticate", "Basic Scheme='yourRealm' location='yourUrl'");
            context.HttpContext.ForbidAsync();
            return;
        }
    }
}
