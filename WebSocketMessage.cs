using System.Text.Json;

public class WebSocketMessage
{
    public string text { get; set; }
    public string role { get; set; }


    //parsing del messaggio ricevuto e controllo della sua struttura
    public static WebSocketMessage Parse(string json)
    {
        var jsonDocument = JsonDocument.Parse(json);
        var rootElement = jsonDocument.RootElement;

        if (rootElement.ValueKind == JsonValueKind.String)
        {
            var innerJson = rootElement.GetString();
            jsonDocument = JsonDocument.Parse(innerJson);
            rootElement = jsonDocument.RootElement;
        }

        if (rootElement.ValueKind == JsonValueKind.Object)
        {
            if (rootElement.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String &&
                rootElement.TryGetProperty("role", out var roleElement) && roleElement.ValueKind == JsonValueKind.String)
            {
                return new WebSocketMessage
                {
                    text = textElement.GetString(),
                    role = roleElement.GetString()
                };
            }
        }

        throw new InvalidOperationException("Invalid JSON format for WebSocketMessage.");
    }
}
