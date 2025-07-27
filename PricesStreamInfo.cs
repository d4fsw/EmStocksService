namespace EmStocksService
{
    internal class PricesStreamInfo
    {
        public string StreamId { get; }
        public string StockName { get; }
        public int PreferenceOrder { get; }

        public PricesStreamInfo(string streamId, string stockName, int preferenceOrder)
        {
            StreamId = streamId;
            StockName = stockName;
            PreferenceOrder = preferenceOrder;
        }

        public override string ToString()
        {
            return $"{nameof(StreamId)}: {StreamId}, {nameof(StockName)}: {StockName}, {nameof(PreferenceOrder)}: {PreferenceOrder}";
        }
    }
}
