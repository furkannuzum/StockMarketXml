using System.Collections.Generic;
using System.Xml.Serialization;

namespace StockMarket.Api.Models
{
    [XmlRoot("StockDataFeed", Namespace = "http://www.example.com/stockdata/v1")]
    public class StockDataFeed
    {
        [XmlElement("Stock")] // Koleksiyon elemanlarının adı <Stock> olacak
        public List<Stock> Stocks { get; set; } = new List<Stock>();
    }
}