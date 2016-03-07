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
    [XmlElementBinding("YandexTranslate")]
    [Description("Переводит лежащее внутри значение через Yandex переводчик")]
    public class YandexTranslateNode : ValueModifierNode
    {
        public static readonly string c_urlTemplate = "https://translate.yandex.net/api/v1.5/tr/translate?key={0}&text={1}&lang={2}";

        protected string m_translatedContent;

        [XmlAttributeBinding]
        [Description("Язык перевода")]
        public string Language { get; set; } = "ru-en";

        [ConstantBinding("YandexApiKey")]
        [XmlAttributeBinding]
        [Description("Ключ для доступа к API переводчика")]
        public string ApiKey { get; set; }

        public override string ProcessValue(string value)
        {
            string content = Uri.EscapeDataString(value);

            WebRequest request = WebRequest.Create(string.Format(c_urlTemplate, ApiKey, content, Language));
            WebResponse response = request.GetResponse();

            XmlDocument document = new XmlDocument();
            document.Load(response.GetResponseStream());

            return document.InnerText;
        }
    }
}
