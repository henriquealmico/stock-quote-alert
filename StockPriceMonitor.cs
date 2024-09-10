using Microsoft.Extensions.Logging;
using StockPricesAlert.Configurations;
using System.Net.WebSockets;
using System.Text;

namespace StockPricesAlert
{
    public class StockPriceMonitor(Config config, ILogger logger)
    {
        private readonly EmailService _emailService = new(config, logger);
        private bool _takeProfitEmailSent;
        private bool _stopLossEmailSent;
        private bool _midZoneEmailSent;

        public async Task MonitorStockPriceAsync(string stockSymbol, decimal takeProfit, decimal stopLoss)
        {
            int attempt = 0;
            const int maxRetries = 5;

            while (true)
            {
                using var webSocket = new ClientWebSocket();
                using var cts = new CancellationTokenSource();
                var uri = new Uri(config.WebSocketUri);
                logger.LogInformation($"Connecting to {uri}");

                try
                {
                    await ConnectAndSubscribeAsync(webSocket, uri, stockSymbol, cts.Token);
                    await ReceiveMessagesAsync(webSocket, stockSymbol, takeProfit, stopLoss, cts.Token);
                    attempt = 0;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during WebSocket communication:");
                    if (++attempt >= maxRetries)
                    {
                        logger.LogError("Max retries reached. Exiting...");
                        return;
                    }
                }
                finally
                {
                    await CloseWebSocketAsync(webSocket);
                }

                await DelayBeforeReconnectAsync(attempt, cts.Token);
            }
        }

        private async Task ConnectAndSubscribeAsync(ClientWebSocket webSocket, Uri uri, string stockSymbol, CancellationToken token)
        {
            await webSocket.ConnectAsync(uri, token);
            logger.LogDebug("Connected to WebSocket.");

            string subscribeMessage = $"{{\"subscribe\":[\"{stockSymbol}\"]}}";
            byte[] bytesToSend = Encoding.UTF8.GetBytes(subscribeMessage);

            await webSocket.SendAsync(new ArraySegment<byte>(bytesToSend), WebSocketMessageType.Text, true, token);
            logger.LogDebug("Subscribe message sent.");
        }

        private async Task CloseWebSocketAsync(ClientWebSocket webSocket)
        {
            logger.LogDebug("Closing WebSocket connection...");
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            logger.LogInformation("WebSocket connection closed.");
        }

        private async Task DelayBeforeReconnectAsync(int attempt, CancellationToken token)
        {
            int delay = (int)Math.Pow(2, attempt) * 1000;
            logger.LogInformation("Waiting to reconnect");
            await Task.Delay(delay, token);
        }

        private async Task ReceiveMessagesAsync(ClientWebSocket webSocket, string stockSymbol, decimal takeProfit, decimal stopLoss, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1024 * 8];

            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        decimal stockPrice = ParseStockPrice(message);

                        logger.LogInformation($"Current price of {stockSymbol}: {stockPrice}");
                        await HandlePriceAlertsAsync(stockSymbol, stockPrice, takeProfit, stopLoss, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error receiving message");
                }
            }
        }

        private async Task HandlePriceAlertsAsync(string stockSymbol, decimal stockPrice, decimal takeProfit, decimal stopLoss, CancellationToken cancellationToken)
        {
            if (stockPrice >= takeProfit && !_takeProfitEmailSent)
            {
                logger.LogInformation($"Take profit triggered at {stockPrice}");
                await Task.Run(() => _emailService.SendEmailAlertAsync(stockSymbol, stockPrice,
                                                            "Take Profit"), cancellationToken);
                _takeProfitEmailSent = true;
                _midZoneEmailSent = false;
            }
            else if (stockPrice <= stopLoss && !_stopLossEmailSent)
            {
                logger.LogInformation($"Stop loss triggered at {stockPrice}");
                await Task.Run(() => _emailService.SendEmailAlertAsync(stockSymbol, stockPrice,
                                                            "Stop Loss"), cancellationToken);

                _stopLossEmailSent = true;
                _midZoneEmailSent = false;
            }
            else if (stockPrice < takeProfit && stockPrice > stopLoss && !_midZoneEmailSent)
            {
                logger.LogInformation($"Price back to no-trade zone at {stockPrice}");
                await Task.Run(() => _emailService.SendEmailAlertAsync(stockSymbol, stockPrice,
                                                            "No-trade Zone"), cancellationToken);
                _midZoneEmailSent = true;
                _takeProfitEmailSent = false;
                _stopLossEmailSent = false;
            }
        }

        private decimal ParseStockPrice(string message)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(message);
                PricingData pricingData = PricingData.Parser.ParseFrom(bytes);

                logger.LogInformation($"{pricingData.Id}: {pricingData.Price}, " +
                                      $"at {DateTimeOffset.FromUnixTimeMilliseconds(pricingData.Time).TimeOfDay}");
                return (decimal)pricingData.Price;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse stock price");
                return -1m;
            }
        }
    }
}
