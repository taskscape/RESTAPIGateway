using GenericTableAPI.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace GenericTableAPI.Services
{
    public class CompositeService(IHttpClientFactory httpClientFactory, ILogger<CompositeService> logger)
    {
        private readonly Dictionary<string, string> _returnedParameters = new();

        public StringValues? AuthorizationHeader { get; set; }

        public async Task<StringResponse> RunCompositeRequest(CompositeRequest compositeRequest)
        {
            if (compositeRequest.Requests == null || compositeRequest.Requests.Count == 0)
            {
                return new StringResponse(StatusCodes.Status400BadRequest, "Invalid input for composite request.");
            }

            StringBuilder allResponses = new();
            HttpRequestMessage httpRequest = new();
            try
            {
                foreach (ApiRequest request in compositeRequest.Requests)
                {
                    if (string.IsNullOrEmpty(request.Method) ||
                        string.IsNullOrEmpty(request.Endpoint))
                    {
                        logger.LogError("Invalid input for composite request.");
                        allResponses.AppendLine("[ERROR] Invalid input for composite request.");
                        return new StringResponse(StatusCodes.Status400BadRequest, allResponses.ToString());
                    }

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            $"{request.Method?.ToUpper() ?? "UNKNOWN"} request to \"{request.Endpoint}\" with parameters: {(request.Parameters != null ? JsonConvert.SerializeObject(request.Parameters) : "null")}. Timestamp: {DateTimeOffset.UtcNow}");
                    }

                    if(!string.IsNullOrWhiteSpace(request.Foreach) && _returnedParameters.TryGetValue(request.Foreach, out string? originalValue))
                    {
                        //Handle 'foreach' request
                        string[] returnedParameterArray;
                        try
                        {
                            returnedParameterArray = JsonConvert.DeserializeObject<string[]>(originalValue);
                        }
                        catch (JsonSerializationException)
                        {
                            returnedParameterArray = [originalValue];
                        }
                        
                        foreach (string currentParameter in returnedParameterArray)
                        {
                            _returnedParameters[request.Foreach] = currentParameter;
                            httpRequest = PrepareHttpRequest(request);
                            await SendHttpRequest(httpRequest, request, allResponses);
                        }

                        _returnedParameters[request.Foreach] = originalValue;
                    }
                    else
                    {
                        //Standard request. No 'foreach' variable
                        httpRequest = PrepareHttpRequest(request);
                        await SendHttpRequest(httpRequest, request, allResponses);
                    }
                }

                return new StringResponse(StatusCodes.Status200OK, allResponses.ToString());
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    $"Error while processing request: {httpRequest.Method} {httpRequest.RequestUri} thrown an exception: {exception.Message}");
                allResponses.AppendLine(
                    $"[FATAL ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" thrown an exception: {exception.Message}");
                return new StringResponse(StatusCodes.Status500InternalServerError, allResponses.ToString());
            }
        }


        private async Task SendHttpRequest(HttpRequestMessage httpRequest, ApiRequest request, StringBuilder allResponses)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            HttpClient client = httpClientFactory.CreateClient();
            HttpResponseMessage response = await client.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    $"Response returned from \"{httpRequest.RequestUri}\" with status code {response.StatusCode}. Timestamp: {timestamp}");
                dynamic? content =
                    JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                allResponses.AppendLine(
                $"[SUCCESS] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" ended up with {(int)response.StatusCode} {response.StatusCode}");

                if (request.Returns == null) return;
                foreach (KeyValuePair<string, string> requestReturn in request.Returns)
                {
                    try
                    {
                        string? contentPathValue = null;
                        IEnumerable<JToken>? obj =
                            content?.SelectTokens(ReplaceUrlParameters(requestReturn.Value,
                                request.Parameters));

                        if (obj != null)
                        {
                            if (obj.Count() > 1)
                            {
                                contentPathValue = obj.Aggregate("[",
                                    (current, item) => current + ("\"" + item + "\", "));
                                contentPathValue = contentPathValue.Remove(contentPathValue.Length - 2, 2) +
                                                   "]";
                            }
                            else
                            {
                                contentPathValue = obj.FirstOrDefault()?.ToString();
                            }
                        }

                        if (contentPathValue == null) continue;

                        if (!string.IsNullOrWhiteSpace(request.Foreach) && _returnedParameters.TryGetValue(requestReturn.Key, out string? val) && val != null)
                        {
                            //TODO: Add [" ... "] as an array. Currently the output looks like this:
                            //{ object1 }, { object2 }...
                            //Should look like this:
                            //["{ object1 }, { object2 }... "]
                            //Fixed probably?
                            allResponses.AppendLine(
                            //$"[INFO] Returned parameter: {requestReturn.Key} = {val},\n{contentPathValue}");//Agragated. for Debug.
                            $"[INFO] Returned parameter: {requestReturn.Key} = {contentPathValue}");

                            // Check if 'val' is already in the format ["..."]
                            if (val.StartsWith("[\"") && val.EndsWith("\"]"))
                                //TODO: Test this solution
                                _returnedParameters[requestReturn.Key] = val.TrimEnd(']').TrimEnd() + $", \"{contentPathValue}\"]";
                            else
                                _returnedParameters[requestReturn.Key] = $"[\"{val},\n{contentPathValue}\"]";
                            //_returnedParameters[requestReturn.Key] = $"{val},\n{contentPathValue}";
                        } 
                        else
                        {
                            allResponses.AppendLine(
                            $"[INFO] Returned parameter: {requestReturn.Key} = {contentPathValue}");
                            _returnedParameters.Add(requestReturn.Key, contentPathValue);
                        }  
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Returned parameter: {requestReturn.Value} not found!");
                        allResponses.AppendLine(
                        $"[ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" Could not find {ReplaceUrlParameters(requestReturn.Value, request.Parameters)} Reason: {ex.Message}");
                        throw new Exception(allResponses.ToString());
                        //return new StringResponse(StatusCodes.Status400BadRequest, allResponses.ToString());//TODO: Handle this commented line
                    }
                }
            }
            else
            {
                logger.LogError(
                    $"Response returned from \"{httpRequest.RequestUri}\" with status code {response.StatusCode}. Timestamp: {timestamp}");
                allResponses.AppendLine(
                    $"[ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" ended up with {(int)response.StatusCode} {response.StatusCode}!");
                //return new StringResponse(StatusCodes.Status500InternalServerError, allResponses.ToString());//TODO: Handle this commented line
            }
        }

        private HttpRequestMessage PrepareHttpRequest(ApiRequest request)
        {
            if (request.Method == null) throw new InvalidOperationException("Request method missing");
            HttpRequestMessage httpRequest = new()
            {
                Method = new HttpMethod(ReplaceUrlParameters(request.Method, request.Parameters)),
            };

            if (request.Endpoint != null && Uri.TryCreate(ReplaceUrlParameters(request.Endpoint, request.Parameters),
                    UriKind.Absolute, out Uri? uriResult))
            {
                httpRequest.RequestUri = uriResult;
            }
            else
            {
                throw new InvalidOperationException("Invalid URI");
            }

            if (request.Parameters != null)
            {
                httpRequest.Content = JsonContent.Create(ReplaceContentParameters(request.Parameters));
            }

            AddAuthorizationHeader(httpRequest);

            return httpRequest;
        }

        private void AddAuthorizationHeader(HttpRequestMessage httpRequest)
        {
            if (!AuthorizationHeader.HasValue || string.IsNullOrEmpty(AuthorizationHeader.Value)) return;
            {
                string[] authHeaderString = AuthorizationHeader.Value[0].Split(' ');
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue(authHeaderString[0], authHeaderString[1]);
            }
        }

        private Dictionary<string, string>? ReplaceContentParameters(Dictionary<string, string>? parameters)
        {
            if (parameters == null)
                return null;

            Dictionary<string, string> modifiedParameters = new(parameters);
            foreach (KeyValuePair<string, string> item in parameters)
            {
                modifiedParameters[ReplaceUrlParameters(item.Key)] = ReplaceUrlParameters(item.Value);
            }

            return modifiedParameters;
        }

        private string ReplaceUrlParameters(string template, Dictionary<string, string>? parameters = null)
        {
            template = _returnedParameters.Aggregate(template,
                (current, parameter) => current.Replace("{" + parameter.Key + "}", parameter.Value));

            return parameters == null
                ? template
                : parameters.Aggregate(template,
                    (current, parameter) => current.Replace("{" + parameter.Key + "}", parameter.Value));
        }
    }
}
