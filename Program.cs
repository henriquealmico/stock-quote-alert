using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockPricesAlert.Configurations;
using System.Globalization;
using System.Text.Json;


namespace StockPricesAlert
{
    internal class Program
    {
        private static string _stockSymbol;
        private static decimal _takeProfit;
        private static decimal _stopLoss;
        private static Config _config;
        private static ILogger<Program> _logger;

        private static async Task Main(string[] args)
        {
            SetupLogging();

            _logger.LogDebug("Program started.");

            if (!ValidateInputs(args))
            {
                _logger.LogError("Invalid inputs provided. Usage: stock-quote-alert.exe <STOCK_SYMBOL> <TAKE_PROFIT> <STOP_LOSS> [NON_BR]");
                return;
            }

            LoadConfiguration();

            _logger.LogInformation("Stock Symbol: {StockSymbol}, Take Profit: {TakeProfit}, Stop Loss: {StopLoss}", _stockSymbol, _takeProfit, _stopLoss);

            var stockPriceMonitor = new StockPriceMonitor(_config, _logger);
            await stockPriceMonitor.MonitorStockPriceAsync(_stockSymbol, _takeProfit, _stopLoss);
        }

        private static void SetupLogging()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(configure => configure.AddConsole())
                .BuildServiceProvider();

            _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        }

        private static bool ValidateInputs(string[] args)
        {
            if (args.Length is < 3 or > 4) return false;

            _stockSymbol = args[0].ToUpperInvariant();

            if (args.Length == 3)
            {
                _stockSymbol += ".SA";
            }

            return decimal.TryParse(args[1], NumberStyles.AllowDecimalPoint,
                                    CultureInfo.InvariantCulture, out _takeProfit)
                                    && _takeProfit >= 0 &&
                   decimal.TryParse(args[2], NumberStyles.AllowDecimalPoint,
                                    CultureInfo.InvariantCulture, out _stopLoss)
                                    && _stopLoss >= 0;
        }

        private static void LoadConfiguration()
        {
            try
            {
                string configJson = File.ReadAllText("./Configurations/config.json");
                _config = JsonSerializer.Deserialize<Config>(configJson) ??
                          throw new InvalidOperationException("Configuration deserialization failed.");

                _logger.LogDebug("Configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration");
                Environment.Exit(1);
            }
        }
    }
}
