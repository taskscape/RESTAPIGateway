using Newtonsoft.Json;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace GenericTableAPI.Utilities
{
    public class CompositeResponseBuilder
    {
        StringBuilder stringBuilder = new();
        public bool ShowDebug = false;

        public CompositeResponseBuilder(bool showDebug = false)
        {
            ShowDebug = showDebug;
        }

        public void AppendResponseObject(object? template, Dictionary<string, string>? variables = null)
        {
            if (ShowDebug)
                stringBuilder.AppendLine("[RESPONSE]");

            if (template == null)
            {
                Dictionary<string, object> parsedVariables = new();

                foreach (var variable in variables)
                {
                    try
                    {
                        object? parsedValue = JsonConvert.DeserializeObject(variable.Value);
                        parsedVariables[variable.Key] = parsedValue ?? variable.Value;
                    }
                    catch
                    {
                        parsedVariables[variable.Key] = variable.Value;
                    }
                }
                stringBuilder.AppendLine(JsonConvert.SerializeObject(parsedVariables, Formatting.Indented));
                return;
            }
                

            var templateStr = template.ToString();
            foreach (var variable in variables)
            {
                templateStr = templateStr.Replace($"\"{{{variable.Key}}}\"", variable.Value) // Replace wrapped in quotes
                                         .Replace($"{{{variable.Key}}}", variable.Value);// Replace regular
            }
            stringBuilder.AppendLine(templateStr);
        }

        public void AppendDebugLine(string text)
        {
            if(ShowDebug)
                stringBuilder.AppendLine(text);
        }

        public override string ToString()
        {
            return stringBuilder.ToString();
        }
    }
}
