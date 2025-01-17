namespace GenericTableAPI
{
    public class ControllerPriorityConstraint(string controllerName) : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            return string.Equals(values["controller"]?.ToString(), controllerName, StringComparison.OrdinalIgnoreCase);
        }
    }
}