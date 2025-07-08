using System.Xml.Serialization;
using System.Xml;
namespace StockMarket.Api.Models
{
    public class Stock
    {
        [XmlAttribute("TickerSymbol")] // XML'de attribute olarak görünmesi için
        public string? TickerSymbol { get; set; }

        [XmlElement("CompanyName")] // XML'de element olarak görünmesi için
        public string? CompanyName { get; set; }

        [XmlElement("CurrentPrice")]
        public decimal CurrentPrice { get; set; }

        [XmlElement("Change")]
        public decimal Change { get; set; }

        [XmlElement("PercentChange")]
        public decimal PercentChange { get; set; }

        [XmlElement("Volume")]
        public long Volume { get; set; } // Hacim büyük bir sayı olabilir

        [XmlElement("LastUpdated")]
        public DateTime LastUpdated { get; set; }

        [XmlElement("Description")]
        public XmlCDataSection? DescriptionCData // CDATA için özel tip
        {
            get
            {
                return !string.IsNullOrEmpty(Description) ? new XmlDocument().CreateCDataSection(Description) : null;
            }
            set
            {
                Description = value?.Value;
            }
        }

        [XmlIgnore] // Bu property XML'e serialize edilmeyecek, DescriptionCData kullanılacak
        public string? Description { get; set; }
    }
}