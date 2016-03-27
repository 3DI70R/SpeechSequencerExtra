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
    [XmlElementBinding("YandexNews")]
    [Description("Загружает новости с news.yandex как значение")]
    public class YandexNewsNode : ValueNode
    {
        private static readonly string c_urlTemplate = "https://news.yandex.{0}/{1}.rss";

        [XmlAttributeBinding]
        [Description("Разделитель")]
        public string Divider { get; set; } = "..., ";

        [XmlAttributeBinding]
        [Description("Загружать ли заголовок")]
        public bool LoadTitle { get; set; } = true;

        [XmlAttributeBinding]
        [Description("Загружать ли более детальное описание новости")]
        public bool LoadDescription { get; set; } = false;

        [XmlAttributeBinding]
        [Description("Домен")]
        public string Domain { get; set; } = "ru";

        [XmlAttributeBinding]
        [Description("Категория новостей")]
        public string Category { get; set; } = "index";

        [XmlAttributeBinding]
        [Description("Количество новостей")]
        public int NewsCount { get; set; } = 1;

        protected override string InitValue(Context context)
        {
            WebRequest request = WebRequest.Create(string.Format(c_urlTemplate, Domain, Category));
            WebResponse response = request.GetResponse();

            XmlDocument document = new XmlDocument();
            document.Load(response.GetResponseStream());
            XmlNodeList nodeList = document.SelectNodes("/rss/channel/item");
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < Math.Min(nodeList.Count, NewsCount); i++)
            {
                XmlNode node = nodeList[i];

                if (LoadTitle)
                {
                    if (builder.Length != 0)
                    {
                        builder.Append(Divider);
                    }

                    builder.Append(node.SelectSingleNode("title").InnerText);
                }

                if (LoadDescription)
                {
                    if (builder.Length != 0)
                    {
                        builder.Append(Divider);
                    }

                    builder.Append(node.SelectSingleNode("description").InnerText);
                }
            }

            return builder.ToString();
        }
    }
}
