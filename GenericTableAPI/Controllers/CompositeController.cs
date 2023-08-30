using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace GenericTableAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",BasicAuthentication")]
    [Route("api/composite")]
    [ApiController]
    public class CompositeController : ControllerBase
    {
        private readonly ILogger<DapperController> _logger;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _client = new HttpClient();

        private Dictionary<string, string> ReturnedParameters = new();

        public CompositeController(ILogger<DapperController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CompositeRequestModel values)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"POST request to \"{HttpContext.Request.Path}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\" with values: {JsonConvert.SerializeObject(values)}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            string AllResponses = "";
            HttpRequestMessage httpRequest = new();

            try
            {
                foreach (var request in values.Requests) {
                    timestamp = DateTimeOffset.UtcNow;
                    requestInfo = $"{request.Method.ToUpper()} request to \"{request.Endpoint}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\" with values: {JsonConvert.SerializeObject(request.Parameters)}. Timestamp: {timestamp}";
                    _logger.LogInformation(requestInfo);

                    //Preparing the request
                    httpRequest = new HttpRequestMessage
                    {
                        Method = new HttpMethod(AddParameters(request.Method, request.Parameters)),
                        RequestUri = new Uri(AddParameters(request.Endpoint, request.Parameters))
                    };

                    if (request.Parameters != null)
                        httpRequest.Content = JsonContent.Create(AddParameters(request.Parameters));

                    if (HttpContext.Request.Headers.Authorization != StringValues.Empty)
                    {
                        string[] authorizationHeader = HttpContext.Request.Headers.Authorization.ToString().Split(' ');
                        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(authorizationHeader[0], authorizationHeader[1]);
                    }
                    
                    //Getting the response
                    var response = _client.Send(httpRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Response returned from \"{httpRequest.RequestUri}\" with status code {response.StatusCode}. Timestamp: {timestamp}");
                        dynamic? content = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                        AllResponses += $"[SUCCESS] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" ended up with {(int)response.StatusCode} {response.StatusCode} \n";
                        if(request.Returns != null)
                        {
                            foreach (var requestReturn in request.Returns)
                            {
                                try
                                {
                                    string contentPathValue;
                                    IEnumerable<JToken> obj = content.SelectTokens(AddParameters(requestReturn.Value, request.Parameters));
                                        
                                    if(obj.Count() > 1)
                                    {
                                        contentPathValue = "[";
                                        foreach (var item in obj)
                                        {
                                            contentPathValue += "\"" + item.ToString() + "\", ";
                                        }
                                        contentPathValue = contentPathValue.Remove(contentPathValue.Length - 2, 2) + "]";
                                    }
                                    else
                                    {
                                        contentPathValue = obj.First().ToString();
                                    }
                                    
                                    ReturnedParameters.Add(requestReturn.Key, contentPathValue);
                                    AllResponses += $"[INFO] Returned parameter: {requestReturn.Key} = {contentPathValue}\n";
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Returned parameter: {requestReturn.Value} not found!");
                                    AllResponses += $"[ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" Could not find {AddParameters(requestReturn.Value, request.Parameters)} Reason: {ex.Message}\n";
                                    return StatusCode(StatusCodes.Status400BadRequest, AllResponses);
                                }
                                
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError($"Response returned from \"{httpRequest.RequestUri}\" with status code {response.StatusCode}. Timestamp: {timestamp}");
                        AllResponses += $"[ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" ended up with {(int)response.StatusCode} {response.StatusCode}! \n";
                        return StatusCode(StatusCodes.Status500InternalServerError, AllResponses);
                    }
                }

                return Ok(AllResponses);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing request: {Request}. Timestamp: {TimeStamp}", requestInfo, timestamp);
                AllResponses += $"[FATAL ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" thrown an exception: {exception.Message}\n";
                return StatusCode(StatusCodes.Status500InternalServerError, AllResponses);
            }
            finally
            {
                string responseInfo = $"Response returned from \"{HttpContext.Request.Path}\" with status code {Response.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation(responseInfo);
            }
        }

        private Dictionary<string, string>? AddParameters(Dictionary<string, string>? parameters)
        {
            if (parameters == null)
                return parameters;

            foreach (var item in parameters)
            {
                parameters[AddParameters(item.Key)] = AddParameters(item.Value);
            }

            return parameters;
        }

        private string AddParameters(string String, Dictionary<string, string>? parameters = null)
        {
            foreach (var parameter in ReturnedParameters)
            {
                String = String.Replace("{" + parameter.Key + "}", parameter.Value);
            }

            if (parameters == null)
                return String;
            foreach (var parameter in parameters)
            {
                String = String.Replace("{" + parameter.Key + "}", parameter.Value);
            }

            return String;
        }
    }
}
