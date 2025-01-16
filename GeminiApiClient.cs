using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class GeminiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly List<WebSocketMessage> _messageHistory;

    public GeminiApiClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _messageHistory = new List<WebSocketMessage>();
    }

    public async Task<WebSocketMessage> GetResponseAsync(WebSocketMessage userMessage)
    {
        _messageHistory.Add(userMessage);

        var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = _messageHistory.ConvertAll(message => new
            {
                role = message.role,
                parts = new[]
                {
                    new { text = message.text }
                }
            })
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsStringAsync();

        var responseJsonDocument = JsonDocument.Parse(responseData);
        var responseRootElement = responseJsonDocument.RootElement;

        if (responseRootElement.TryGetProperty("candidates", out var candidatesElement) && candidatesElement.ValueKind == JsonValueKind.Array)
        {
            var firstCandidate = candidatesElement[0];
            if (firstCandidate.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.Object)
            {
                if (contentElement.TryGetProperty("parts", out var partsElement) && partsElement.ValueKind == JsonValueKind.Array)
                {
                    var firstPart = partsElement[0];
                    if (firstPart.TryGetProperty("text", out var responseTextElement) && responseTextElement.ValueKind == JsonValueKind.String)
                    {
                        var responseText = responseTextElement.GetString();

                        var responseMessage = new WebSocketMessage
                        {
                            text = responseText,
                            role = "model"
                        };

                        _messageHistory.Add(responseMessage);

                        return responseMessage;
                    }
                }
            }
        }

        throw new InvalidOperationException("Invalid response format from Gemini API.");
    }
    public void ResetMessageHistory()
    {
        _messageHistory.Clear();
    }
    public IReadOnlyList<WebSocketMessage> GetMessageHistory() => _messageHistory.AsReadOnly();
}
