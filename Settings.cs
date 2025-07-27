namespace EmStocksService
{
    internal class Settings
    {
        public TimeSpan MaxRateTimePeriod { get; }
        public TimeSpan SameValueSkipTimePeriod { get; }
        public TimeSpan MaxConsecutivePricesDelayTimePeriod { get; }
        public int MaxConsecutivePricesDifferencePercentage { get; }

        public Settings(TimeSpan maxRateTimePeriod, TimeSpan sameValueSkipTimePeriod, TimeSpan maxConsecutivePricesDelayTimePeriod, int maxConsecutivePricesDifferencePercentage)
        {
            MaxRateTimePeriod = maxRateTimePeriod;
            SameValueSkipTimePeriod = sameValueSkipTimePeriod;
            MaxConsecutivePricesDelayTimePeriod = maxConsecutivePricesDelayTimePeriod;
            MaxConsecutivePricesDifferencePercentage = maxConsecutivePricesDifferencePercentage;
        }
    }
}
