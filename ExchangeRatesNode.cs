using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ThreeDISevenZeroR.SpeechSequencer.Core;

namespace ThreeDISevenZeroR.SpeechSequencer.Extra
{
    [XmlElementBinding("ExchangeRate")]
    public class ExchangeRatesNode : ValueNode
    {
        private const string c_exchangeDataUrl = "http://query.yahooapis.com/v1/public/yql?q=select%20%2a%20from%20yahoo.finance.xchange%20where%20pair%20in%20%28%22{0}RUB%22%29&env=store://datatables.org/alltableswithkeys";

        [XmlAttributeBinding]
        [Description("Валюта")]
        public string Currency { get; set; } = "USD";

        public override string LoadValue(Context context)
        {
            string exchangeRates = LoadExchangeRates();
            string[] parts = exchangeRates.Split('.');
            string rub = parts[0];
            string kop = parts[1].Substring(0, 2);

            return string.Format("{0} {1} {2} {3}", rub, RublePlurals(rub), kop, KopeyekPlurals(kop));
        }

        private string GetPlural(string number, params string[] endings)
        {
            int intNumber = int.Parse(number);
            intNumber = intNumber % 100;

            if (intNumber >= 11 && intNumber <= 19)
            {
                return endings[2];
            }
            else
            {
                switch (intNumber % 10)
                {
                    case (1): return endings[0];
                    case (2):
                    case (3):
                    case (4): return endings[1];
                    default: return endings[2];
                }
            }
        }

        private string RublePlurals(string count)
        {
            return GetPlural(count, "рубль", "рубля", "рублей");
        }
        private string KopeyekPlurals(string count)
        {
            return GetPlural(count, "копейка", "копейки", "копеек");
        }

        private string LoadExchangeRates()
        {
            string url = string.Format(c_exchangeDataUrl, Currency);
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();

            XmlDocument document = new XmlDocument();
            document.Load(response.GetResponseStream());

            XmlElement element = (XmlElement) document.SelectSingleNode(".//rate/Rate");
            return element.InnerText;
        }
    }
}
