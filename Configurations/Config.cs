namespace StockPricesAlert.Configurations
{
    public class Config
    {
        public required string WebSocketUri { get; set; }
        public required EmailConfig EmailSettings { get; set; }
        public int ReconnectionDelayMs { get; set; }
    }

    public class EmailConfig
    {
        public required string FromEmail { get; set; }
        public required string ToEmail { get; set; }
        public required string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public required string EmailPassword { get; set; }
    }
}
