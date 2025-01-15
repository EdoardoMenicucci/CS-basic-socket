using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class WebSocketHandler
{
    private readonly WebSocket _webSocket;
    private readonly ILogger _logger;

    public WebSocketHandler(WebSocket webSocket, ILogger logger)
    {
        _webSocket = webSocket;
        _logger = logger;
    }

    public async Task HandleConnectionAsync()
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation($"Received message: {receivedMessage}");

                    try
                    {
                        var jsonDocument = JsonDocument.Parse(receivedMessage);
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
                                var text = textElement.GetString();
                                var role = roleElement.GetString();

                                var response = new WebSocketMessage
                                {
                                    text = text,
                                    role = "ia"
                                };

                                var responseJson = JsonSerializer.Serialize(response);
                                var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                                await _webSocket.SendAsync(
                                    new ArraySegment<byte>(responseBytes),
                                    WebSocketMessageType.Text,
                                    true,
                                    CancellationToken.None);

                                _logger.LogInformation($"Sent response: {responseJson}");
                            }
                            else
                            {
                                throw new InvalidOperationException("The JSON object does not contain the required properties 'text' and 'role' of type 'String'.");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("The requested operation requires an element of type 'Object', but the target element has type 'String'.");
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"Error parsing JSON message: {ex.Message}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError($"Error in WebSocket connection: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in WebSocket connection: {ex.Message}");
        }
    }
}
