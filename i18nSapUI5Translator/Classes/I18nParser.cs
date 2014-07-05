using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
namespace i18nSapUI5Translator.Classes
{
    public class I18nParser
    {
        public static Dictionary<string, I18n> ReadFile(string fileName)
        {
            Dictionary<string, I18n> dictionary = new Dictionary<string, I18n>();
            foreach (string line in File.ReadAllLines(fileName))
            {
                if ((!string.IsNullOrEmpty(line)) &&
                    (!line.StartsWith(";")) &&
                    (!line.StartsWith("'")) &&
                    (!line.StartsWith("#")) &&
                    (line.Contains('=')))
                {
                    int index = line.IndexOf('=');
                    string key = line.Substring(0, index).Trim();
                    string value = line.Substring(index + 1).Trim();

                    if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    dictionary.Add(key, new I18n { Value = value });
                }
            }

            dictionary = ParseComments(dictionary, fileName);

            return dictionary;
        }

        private static Dictionary<string, I18n> ParseComments(Dictionary<string, I18n> dict, string file)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]) && lines[i].StartsWith("#"))
                {
                    var nextLine = lines[i + 1];
                    if (nextLine.Contains("="))
                    {
                        int index = nextLine.IndexOf('=');
                        string key = nextLine.Substring(0, index).Trim();
                        dict[key].Comment = lines[i].Replace("#", string.Empty).Trim();
                    }
                } 
            }
                return dict;
        }
    }
}
