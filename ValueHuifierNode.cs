using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeDISevenZeroR.SpeechSequencer.Core
{
    [XmlElementBinding("Huify")]
    [Description("Набор настоящего джентельмена-хуентельмена")]
    public class ValueHuifierNode : ValueModifierNode
    {
        private static readonly Dictionary<char, string> s_huyDictionary = new Dictionary<char, string>()
        {
            { 'а', "хуя" },
            { 'о', "хуё" },
            { 'э', "хуе" },
            { 'и', "хуи" },
            { 'у', "хую" },
            { 'ы', "хуй" },
            { 'ё', "хуё" },
            { 'е', "хуе" },
            { 'ю', "хую" },
            { 'я', "хуя" }
        };
        private static readonly char[] s_vowelSymbols = GatherVowels(s_huyDictionary.Keys);

        [XmlAttributeBinding]
        [Description("Необходимо ли отображать оригинальные слова рядом с хуифицированными")]
        public bool ShowOriginal { get; set; } = true;

        public override string ProcessValue(string value)
        {
            return HuifySentence(value);
        }

        public string HuifySentence(string text)
        {
            StringBuilder builder = new StringBuilder();

            foreach (string word in text.Split(' '))
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    string huified = HuifyWord(word);

                    if(!string.IsNullOrWhiteSpace(huified))
                    {
                        if (builder.Length != 0)
                        {
                            builder.Append(" ");
                        }

                        if (ShowOriginal)
                        {
                            builder.Append(word);
                        }

                        if (!huified.Equals(word))
                        {
                            if (ShowOriginal)
                            {
                                builder.Append("-");
                            }

                            builder.Append(huified);
                        }
                    }
                }
            }

            return builder.ToString();
        }

        public static string HuifyWord(string text)
        {
            int index = text.IndexOfAny(s_vowelSymbols);

            if (index != -1 && index < text.Length - 1)
            {
                char origVowel = char.ToLowerInvariant(text[index]);
                string replacement = s_huyDictionary[origVowel];

                if (IsAllCaps(text))
                {
                    replacement = replacement.ToUpperInvariant();
                }

                string postfix = text.Substring(index + 1);

                return replacement + postfix;
            }

            return text;
        }
        private static char[] GatherVowels(IEnumerable<char> vowelCollection)
        {
            List<char> vowels = new List<char>();

            foreach (char c in vowelCollection)
            {
                vowels.Add(c);
                vowels.Add(char.ToUpperInvariant(c));
            }

            return vowels.ToArray();
        }
        public static bool IsAllCaps(string text)
        {
            for(int i = 0; i < text.Length; i++)
            {
                if(char.IsLetter(text[i]) && char.IsLower(text[i]))
                {
                    return false;
                }
            }

            return true;
        }
        public static int FindWordStart(string text, int searchStart)
        {
            for (int i = searchStart; i >= 0; i--)
            {
                if (!char.IsLetter(text[i]))
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
