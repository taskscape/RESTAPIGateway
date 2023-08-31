using Azure;
using GenericTableAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using GenericTableAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Azure.Core;

namespace GenericTableAPI.Routines
{
    public class CompositeService
    {
        private readonly ILogger<DapperController> _logger;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _client = new HttpClient();

        private Dictionary<string, string> ReturnedParameters = new();

        public StringValues? AuthorizationHeader { get; set; }

        public CompositeService(ILogger<DapperController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public async Task<StringResponseModel> RunCompositeRequest(CompositeRequestModel values)
        {
            string AllResponses = "";
            HttpRequestMessage httpRequest = new();
            try
            {
                foreach (var request in values.Requests)
                {
                    DateTimeOffset timestamp = DateTimeOffset.UtcNow;
                    string requestInfo = $"{request.Method.ToUpper()} request to \"{request.Endpoint}\" with values: {JsonConvert.SerializeObject(request.Parameters)}. Timestamp: {timestamp}";
                    _logger.LogInformation(requestInfo);

                    //Preparing the request
                    httpRequest = new HttpRequestMessage
                    {
                        Method = new HttpMethod(AddParameters(request.Method, request.Parameters)),
                        RequestUri = new Uri(AddParameters(request.Endpoint, request.Parameters))
                    };

                    if (request.Parameters != null)
                        httpRequest.Content = JsonContent.Create(AddParameters(request.Parameters));

                    if (AuthorizationHeader.HasValue)
                    {
                        string[] splittedHeader = AuthorizationHeader.ToString().Split(' ');
                        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(splittedHeader[0], splittedHeader[1]);
                    }

                    //Getting the response
                    var response = _client.Send(httpRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Response returned from \"{httpRequest.RequestUri}\" with status code {response.StatusCode}. Timestamp: {timestamp}");
                        dynamic? content = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                        AllResponses += $"[SUCCESS] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" ended up with {(int)response.StatusCode} {response.StatusCode} \n";
                        
                        if (request.Returns != null)
                        {
                            foreach (var requestReturn in request.Returns)
                            {
                                try
                                {
                                    string contentPathValue;
                                    IEnumerable<JToken> obj = content.SelectTokens(AddParameters(requestReturn.Value, request.Parameters));

                                    if (obj.Count() > 1)
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
                                    return new StringResponseModel(StatusCodes.Status400BadRequest, AllResponses);
                                }

                            }
                        }
                    }
                    else
                    {
                        _logger.LogError($"Response returned from \"{httpRequest.RequestUri}\" with status code {response.StatusCode}. Timestamp: {timestamp}");
                        AllResponses += $"[ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" ended up with {(int)response.StatusCode} {response.StatusCode}! \n";
                        return new StringResponseModel(StatusCodes.Status500InternalServerError, AllResponses);
                    }
                }

                return new StringResponseModel(StatusCodes.Status200OK, AllResponses);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while processing request: {httpRequest.Method} {httpRequest.RequestUri} thrown an exception: {exception.Message}");
                AllResponses += $"[FATAL ERROR] \"{httpRequest.Method}\" \"{httpRequest.RequestUri}\" thrown an exception: {exception.Message}\n";
                return new StringResponseModel(StatusCodes.Status500InternalServerError, AllResponses);
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
