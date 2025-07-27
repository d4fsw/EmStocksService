namespace EmStocksService
{
    internal class PricesPublisher
    {
        /// <summary>
        /// This method is to be called for publishing the selected price for each stock to
        /// the interested subscribers. Assume that the implementation is in place, there should 
        /// be a single PricesPublisher instance and that the Publish method is safe to be called
        /// simultaneously from multiple threads. 
        /// </summary>
        /// <param name="stockName"></param>
        /// <param name="price"></param>

        private readonly Dictionary<string, decimal> _lastPublishedPrices = new();
        private DateTime _lastPublishedTime = DateTime.MinValue;
        private readonly object _lockObject = new object();
        private readonly Settings _settings;

        public PricesPublisher(Settings settings)
        {
            _lastPublishedPrices = new Dictionary<string, decimal>();
            _lastPublishedTime = DateTime.MinValue;
            _settings = settings;
        }
        /// <summary>
        /// This method checks if the price should be published, based on the configured time threshold
        /// and whether the price has changed compared to the last published price.
        /// </summary>
        private bool ShouldPublish(string streamId, string stockName, decimal price, DateTime priceTime)
        {
            bool priceChanged = !_lastPublishedPrices.ContainsKey(streamId) || _lastPublishedPrices[streamId] != price;
            bool timeElapsed = (priceTime - _lastPublishedTime).TotalMilliseconds >= _settings.MaxRateTimePeriod.TotalMilliseconds;  // max 50ms rate limiting
            return priceChanged && timeElapsed;
        }

        /// <summary>
        /// This method publishes the selected price to subscribers if the price is eligible based on the
        /// rules (change in price and time delay).
        /// </summary>
        public void Publish(string streamId, string stockName, decimal price, DateTime priceTime)
        {
            lock (_lockObject)
            {
                if (ShouldPublish(streamId, stockName, price, priceTime))
                {
                    _lastPublishedPrices[streamId] = price;
                    _lastPublishedTime = priceTime;
                }
            }
        }
    }
    
}
