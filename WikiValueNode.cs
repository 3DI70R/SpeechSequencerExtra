using System;
using System.Net;
using System.ComponentModel;
using System.Xml;

namespace ThreeDISevenZeroR.SpeechSequencer.Core
{
	[XmlElementBinding("Wiki")]
	[Description("Загружает краткое описание значения из википедии")]
	public class WikiValueNode : ValueModifierNode
	{
		private const string c_wikiSearchUrl = "https://{0}.wikipedia.org/w/api.php?format=xml&action=query&prop=extracts&list=search&srsearch={1}";
		private const string c_wikiPageUrl = "https://{0}.wikipedia.org/w/api.php?format=xml&action=query&prop=extracts&exintro=&explaintext=&titles={1}";

        [XmlAttributeBinding]
        [Description("Из какого языкового сегмента википедии необходимо брать данные")]
        public string Domain { get; set; } = "ru";

		public override string ProcessValue (string value)
		{
			string articleName = SearchFullArticleName (value);

			if (articleName != null)
            {
                string fullArticle = LoadArticle(articleName);

                if(fullArticle != null)
                {
                    return fullArticle;
                }
			}

			return "Не удалось найти статью в википедии по теме " + value;
		}

		public string SearchFullArticleName(string text)
		{
			ServicePointManager.ServerCertificateValidationCallback = (srvPoint, certificate, chain, errors) => true;

			string url = string.Format (c_wikiSearchUrl, "ru", text);
			XmlDocument document = CreateDocumentFromUrl (url);
			XmlElement element = (XmlElement)document.SelectSingleNode ("//p");

			if (element != null)
			{
				return element.GetAttribute ("title");
			}
			else
			{
				return null;
			}
		}
		public string LoadArticle(string fullName)
		{
			ServicePointManager.ServerCertificateValidationCallback = (srvPoint, certificate, chain, errors) => true;

			string url = string.Format (c_wikiPageUrl, "ru", fullName);
			XmlDocument document = CreateDocumentFromUrl (url);
			XmlElement element = (XmlElement)document.SelectSingleNode ("//extract");

			if (element != null) {
				return element.InnerText;
			} else {
				return string.Empty;
			}
		}

		private XmlDocument CreateDocumentFromUrl(string url)
		{
			WebRequest request = WebRequest.Create (url);
			WebResponse response = request.GetResponse ();

			XmlDocument document = new XmlDocument ();
			document.Load (response.GetResponseStream ());

			return document;
		}
	}
}

