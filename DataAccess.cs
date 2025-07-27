namespace EmStocksService
{
    internal class DataAccess
    {
        public Settings GetSettings()
        {
            return new Settings(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(150), TimeSpan.FromSeconds(60), 20);
        }

        public List<PricesStreamInfo> GetPricesStreamsInfo()
        {
            return new List<PricesStreamInfo>();
        }
    }
}