namespace GenericTableAPI
{
    public class ControllerPriorityConstraint : IRouteConstraint
    {
        private readonly string _controllerName;

        public ControllerPriorityConstraint(string controllerName)
        {
            _controllerName = controllerName;
        }

        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            return string.Equals(values["controller"].ToString(), _controllerName, StringComparison.OrdinalIgnoreCase);
        }
    }
}