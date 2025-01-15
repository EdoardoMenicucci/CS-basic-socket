using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class WebSocketHandler
{
    private readonly WebSocket _webSocket;
    private readonly ILogger _logger;
    private readonly GeminiApiClient _geminiApiClient;

    public WebSocketHandler(WebSocket webSocket, ILogger logger, GeminiApiClient geminiApiClient)
    {
        _webSocket = webSocket;
        _logger = logger;
        _geminiApiClient = geminiApiClient;
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
                    _logger.LogInformation($"User's message: {receivedMessage}");

                    try
                    {       
                        //REGISTER USER MESSAGE
                        var userMessage = WebSocketMessage.Parse(receivedMessage);
                            
                        var userMessageJson = JsonSerializer.Serialize(userMessage);
                        var userMessageBytes = Encoding.UTF8.GetBytes(userMessageJson);

                        await _webSocket.SendAsync(
                            new ArraySegment<byte>(userMessageBytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);


                        //GET GEMINI RESPONSE
                        var responseMessage = await _geminiApiClient.GetResponseAsync(userMessage);

                      

                        var responseJson = JsonSerializer.Serialize(responseMessage);
                        var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                        await _webSocket.SendAsync(
                            new ArraySegment<byte>(responseBytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);

                        _logger.LogInformation($"Received response: {responseJson}");
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