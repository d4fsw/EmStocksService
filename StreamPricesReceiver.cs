using System.IO;

namespace EmStocksService
{
    /// <summary>
    /// There should be one StreamPricesReceiver instance per each available prices stream and for each 
    /// StreamPricesReceiver instance there is a SEPARATE THREAD which listens to the corresponding prices
    /// stream and it calls the RaisePriceReceived method whenever a new price is received.
    /// Assume that there is implementation in place for the functionality that listens to the corresponding prices
    /// stream and whenever a price for a stock is received it calls the RaisePriceReceived method.     
    /// Assume that the information about the available price streams are in the database, in a table with
    /// a field of type string that identifies the prices stream and that the code to read the stream name identifiers 
    /// from the database is already implemented.
    /// </summary>
    internal class StreamPricesReceiver
    {
        private string _streamId;
        private readonly Dictionary<string, PricesStreamInfo> _stockStreamPreferences;
        private readonly Dictionary<string, DateTime> _lastReceivedTime;
        private readonly Dictionary<string, decimal> _lastReceivedPrice;
        private readonly Dictionary<string, StockStatistics> _stockStatistics;
        private readonly PricesPublisher _pricesPublisher;
        private readonly Settings _settings;
        private readonly object _lockObject = new object();

        public event EventHandler<PriceReceivedEventArgs>? PriceReceived;

        public StreamPricesReceiver(string streamId, PricesPublisher pricesPublisher, Settings settings)
        {
            _streamId = streamId;
            _stockStreamPreferences = new Dictionary<string, PricesStreamInfo>();
            _lastReceivedTime = new Dictionary<string, DateTime>();
            _lastReceivedPrice = new Dictionary<string, decimal>();
            _stockStatistics = new Dictionary<string, StockStatistics>();
            _pricesPublisher = pricesPublisher;
            _settings = settings;
        }

        /// <summary>
        /// This method will create the subscription to the prices stream so whenever a price is available
        /// the RaisePriceReceived method will be called. This method must be called during the service 
        /// initialization for each StreamPricesReceiver instance that will be created.
        /// Assume that the implementation of this method is in place.
        /// </summary>
        /// <param name="streamId"></param>



        /// <summary>
        /// This method subscribes the receiver to a particular price stream for a stock. This is part of the task where
        /// each price stream will be subscribed with its corresponding stock and preference order.
        /// </summary>
        public void Subscribe(string streamId, string stockName, int preferenceOrder)
        {
            if (!_stockStreamPreferences.ContainsKey(streamId))
            {
                _stockStreamPreferences[streamId] = new PricesStreamInfo(streamId, stockName, preferenceOrder);
            }
        }

        /// <summary>
        /// This method will remove the subscription from the the prices stream and 
        /// must be called during the service termination for each StreamPricesReceiver 
        /// instance that has been instantiated so as to release the held resources.
        /// Assume that the implementation of this method is in place.
        /// </summary>
        /// <param name="streamId"></param>


        /// <summary>
        /// This method unsubscribes the receiver from a price stream when the service terminates. It ensures that resources
        /// are released properly. This is a cleanup operation for each StreamPricesReceiver.
        /// </summary>
        public void Unsubscribe(string streamId)
        {
            if (_stockStreamPreferences.ContainsKey(streamId))
            {
                _stockStreamPreferences.Remove(streamId);
            }
        }
        
        /// <summary>
        /// This method is invoked when a price is received for a specific stock from a price stream.
        /// It processes the received price based on configured thresholds and updates the stock statistics.
        /// </summary>
        public void RaisePriceReceived(string streamId, string stockName, decimal price, DateTime priceTime, Settings settings)
        {
            lock (_lockObject)
            {
                //  Ελέγχουμε αν το stream είναι έγκυρο και υπάρχει στο σύστημα
                if (!_stockStreamPreferences.ContainsKey(streamId)) return;

                //  Αν είναι η πρώτη φορά που λαμβάνουμε τιμή για αυτό το stream, αρχικοποιούμε τις αποθηκευμένες τιμές
                if (!_lastReceivedTime.ContainsKey(streamId) || !_lastReceivedPrice.ContainsKey(streamId))
                {
                    _lastReceivedTime[streamId] = priceTime;
                    _lastReceivedPrice[streamId] = price;
                }

                //  Υπολογίζουμε τη χρονική διαφορά από την τελευταία τιμή και τη μεταβολή της τιμής σε ποσοστό
                var timeDifference = (priceTime - _lastReceivedTime[streamId]).TotalSeconds;
                var priceDifference = Math.Abs((price - _lastReceivedPrice[streamId]) / _lastReceivedPrice[streamId]) * 100;

                //  Αν η χρονική διαφορά ή η μεταβολή της τιμής υπερβαίνουν τα επιτρεπτά όρια (thresholds)
                if (timeDifference > settings.MaxRateTimePeriod.TotalSeconds || priceDifference > settings.MaxConsecutivePricesDifferencePercentage) // thresholds
                {
                    //  Αλλάζουμε το πρωτεύον stream σε περίπτωση που κάποιο από τα όρια παραβιάζεται
                    SwitchPrimaryStream(streamId, stockName);
                }

                //  Ενημερώνουμε τα στατιστικά του stock
                UpdateStockStatistics(streamId, stockName, price, priceTime);

                //  Αποθηκεύουμε την τελευταία ληφθείσα τιμή και τον χρόνο λήψης της
                _lastReceivedTime[streamId] = priceTime;
                _lastReceivedPrice[streamId] = price;

                //  Raise the PriceReceived event for further processing
                PriceReceived?.Invoke(this, new PriceReceivedEventArgs(streamId, stockName, price, priceTime));

                //  Δημοσιεύουμε την τιμή στους συνδρομητές αν πληροί τις συνθήκες δημοσίευσης
                _pricesPublisher.Publish(streamId, stockName, price, priceTime);
            }
        }

        /// <summary>
        /// This method is responsible for switching the primary price stream for a stock when the conditions (price difference or delay)
        /// are met, based on the configured preference order. 
        /// </summary>
        private void SwitchPrimaryStream(string streamId, string stockName)
        {
            lock (_lockObject)
            {
                //  Ανάκτηση του τρέχοντος πρωτεύοντος stream για το συγκεκριμένο stock
                var currentStream = _stockStreamPreferences[streamId];

                //  Εύρεση του επόμενου διαθέσιμου stream με βάση την προτεραιότητα
                var nextStream = _stockStreamPreferences.Values
                    .Where(s => s.PreferenceOrder > currentStream.PreferenceOrder)
                    .OrderBy(s => s.PreferenceOrder)
                    .FirstOrDefault();
                
                //  Αν υπάρχει διαθέσιμο stream και έχουμε ήδη λάβει τιμές από αυτό, γίνεται η μετάβαση
                if (nextStream != null && _lastReceivedPrice.ContainsKey(nextStream.StreamId))
                {
                    //  Ορισμός του νέου πρωτεύοντος stream
                    _stockStreamPreferences[streamId] = nextStream;

                    //  Λήψη της τελευταίας αποθηκευμένης τιμής και χρόνου για το νέο stream
                    var lastPrice = _lastReceivedPrice[nextStream.StreamId];
                    var lastTime = _lastReceivedTime[nextStream.StreamId];

                    //  Δημοσίευση της τελευταίας διαθέσιμης τιμής από το νέο πρωτεύον stream
                    _pricesPublisher.Publish(nextStream.StreamId, nextStream.StockName, lastPrice, lastTime);
                }
            }
        }

        /// <summary>
        /// This method updates the stock statistics (min/max price, fluctuation) for the stock based on the received price.
        /// It is used to maintain detailed statistics for each stock.
        /// </summary>
        private void UpdateStockStatistics(string streamId, string stockName, decimal price, DateTime priceTime)
        {
            lock (_lockObject)
            {
                //  Αν δεν υπάρχει ήδη καταχωρημένο στατιστικό για το stream, το δημιουργούμε
                if (!_stockStatistics.ContainsKey(streamId))
                {
                    _stockStatistics[streamId] = new StockStatistics();
                }

                //  Ανάκτηση των στατιστικών για το συγκεκριμένο stream
                var stats = _stockStatistics[streamId];
                // Ενημέρωση της τελευταίας τιμής και του χρόνου που λήφθηκε
                stats.LastPriceTime = priceTime;
                stats.LastPrice = price;

                //  Ενημέρωση της ελάχιστης τιμής εάν η νέα τιμή είναι μικρότερη
                if (stats.MinPrice == 0 || price < stats.MinPrice)
                {
                    stats.MinPrice = price;
                }
                //  Ενημέρωση της μέγιστης τιμής εάν η νέα τιμή είναι μεγαλύτερη
                if (price > stats.MaxPrice)
                {
                    stats.MaxPrice = price;
                }
                //  Υπολογισμός της διακύμανσης τιμών προσθέτοντας τη διαφορά από την προηγούμενη τιμή
                stats.PriceFluctuation += Math.Abs(price - stats.LastPrice);

            }
        }
        /// <summary>
        /// This method provides the statistics for a stock when requested.
        /// It includes the min/max price, fluctuation, etc.
        /// </summary>
        public StockStatistics GetStockStatistics(string streamId)
        {
            lock (_lockObject)
            {
                return _stockStatistics.ContainsKey(streamId) ? _stockStatistics[streamId] : new StockStatistics();
            }
        }
    }
}
