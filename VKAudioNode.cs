using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using ThreeDISevenZeroR.SpeechSequencer.Core;
using System.ComponentModel;
using System.Net;
using System.IO;
using System.Xml;

namespace ThreeDISevenZeroR.SpeechSequencer.Extra
{
    [XmlElementBinding("VKAudio")]
    [Description("Проигрывает музыку из соцсети вконтакте")]
    public class VKAudioNode : ValuePlaybackNode
    {
        private const string c_apiSearch = "https://api.vk.com/method/audio.search.xml?access_token={0}&q={1}&count=1";

        [ConstantBinding("VKApiKey")]
        [XmlAttributeBinding]
        [Description("Ключ доступа для API ВКонтакте")]
        public string ApiKey { get; set; }

        public ISampleProvider LoadAudio(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();

            MemoryStream memoryStream = new MemoryStream();
            response.GetResponseStream().CopyTo(memoryStream);

            memoryStream.Position = 0;
            return new Mp3FileReader(memoryStream).ToSampleProvider();
        }

        protected override ISampleProvider CreateProvider(string value, IPlaybackContext context)
        {
            WebRequest request = WebRequest.Create(string.Format(c_apiSearch, ApiKey, value));
            WebResponse response = request.GetResponse();
            XmlDocument document = new XmlDocument();
            document.Load(response.GetResponseStream());
            XmlElement element = (XmlElement)document.SelectSingleNode(".//audio/url");

            if (element != null)
            {
                return LoadAudio(element.InnerText);
            }
            else
            {
                IAudioNode audio = ("Не удалось найти аудио по запросу " + value).WrapStringAsSpeech();
                audio.InitNewState(context);
                return audio;
            }
        }
    }
}
