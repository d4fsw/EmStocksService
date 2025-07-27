namespace EmStocksService
{
    internal class PriceReceivedEventArgs
    {
        public string StreamId { get; }
        public string StockName { get; }
        public decimal Price { get; }
        public DateTime PriceTime { get; }

        public PriceReceivedEventArgs(string streamId, string stockName, decimal price, DateTime priceTime)
        {
            StreamId = streamId;
            StockName = stockName;
            Price = price;
            PriceTime = priceTime;
        }
    }
}