using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmStocksService
{

    internal class StockStatistics
    {
        public DateTime LastPriceTime { get; set; }
        public decimal LastPrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal PriceFluctuation { get; set; }
    }
}
