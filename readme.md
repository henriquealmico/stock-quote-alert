# Stock Prices Alert

This is a console application that monitors stock prices and sends email alerts when certain conditions are met (take profit or stop loss). The application connects to Yahoo WebSocket server to receive real-time stock price updates.

The data.proto was obtained from this repository in Github: [YahooFinanceWebSocket](https://github.com/izzeww/YahooFinanceWebSocket)

## Features

- Monitors worldwide financial assets prices in real-time using WebSocket.
- Sends email alerts for take profit, stop loss, and no-trade zone conditions.
- Configurable via a JSON configuration file.

## Usage

- `<STOCK_SYMBOL>`: The stock symbol to monitor (e.g., `PRIO3`).
- `<TAKE_PROFIT>`: The price at which to trigger a take profit alert.
- `<STOP_LOSS>`: The price at which to trigger a stop loss alert.
- `[NON_BR]`: Optional. If provided, the stock symbol will not have `.SA` appended. In general, brazilian stocks require this flag.

### Examples

- Monitor PETR4 with take profit at 40 and stop loss at 35:

    ```bash
    dotnet run PETR4 40 35
    ```

- Monitor ^BVSP with take profit at 135000, stop loss at 134000, and no `.SA` suffix:

    ```bash
    dotnet run ^BVSP 135000 134000 NON_BR
    ```
  
## Configuration

The application requires a configuration file located at `./Configurations/config.json`. Below is an example configuration file:

- `WebSocketUri`: The WebSocket URI to connect to for stock price updates.
- `EmailSettings`: Configuration for the email service.
  - `SmtpServer`: The SMTP server address.
  - `SmtpPort`: The SMTP server port.
  - `SenderEmail`: The email address to send alerts from.
  - `SenderPassword`: The password for the sender email.
  - `RecipientEmail`: The email address to send alerts to.
- ~~`ReconnectionDelayMs`: The delay in milliseconds before attempting to reconnect to the WebSocket server.~~

## Contact

Email: <henriquealmico@poli.ufrj.br>

