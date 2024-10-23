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

                    DateTimeOffset timestamp = DateTimeOffset.UtcNow;
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            $"{request.Method?.ToUpper() ?? "UNKNOWN"} request to \"{request.Endpoint}\" with parameters: {(request.Parameters != null ? JsonConvert.SerializeObject(request.Parameters) : "null")}. Timestamp: {timestamp}");
                    }

                    httpRequest = PrepareHttpRequest(request);

                    //Getting the response
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

                        if (request.Returns == null) continue;
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
                                _returnedParameters.Add(requestReturn.Key, contentPathValue);
                                allResponses.AppendLine(
                                    $"[INFO] Returned parameter: {requestReturn.Key} = {contentPathValue}");
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"Returned parameter: {requestReturn.Value} not found!");
                                allResponses.AppendLine(
                                    $"[ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" Could not find {ReplaceUrlParameters(requestReturn.Value, request.Parameters)} Reason: {ex.Message}");
                                return new StringResponse(StatusCodes.Status400BadRequest, allResponses.ToString());
                            }

                        }
                    }
                    else
                    {
                        logger.LogError(
                            $"Response returned from \"{httpRequest.RequestUri}\" with status code {response.StatusCode}. Timestamp: {timestamp}");
                        allResponses.AppendLine(
                            $"[ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" ended up with {(int)response.StatusCode} {response.StatusCode}!");
                        return new StringResponse(StatusCodes.Status500InternalServerError, allResponses.ToString());
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
