using i18nSapUI5Translator.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i18nSapUI5Translator.Classes
{
    public class JsonParser : ITranslationFileParser
    {
        public string FileExt { get => ".json"; }

        public string Locale { get; private set; }

        public List<KeyValuePair<string, I18n>> ReadFile(string fileName)
        {
            if (!fileName.Contains(FileExt))
            {
                fileName += FileExt;
            }
            List<KeyValuePair<string, I18n>> duplicateKeyDictionary = new List<KeyValuePair<string, I18n>>();

            using (StreamReader file = File.OpenText(fileName))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject jObject = (JObject)JToken.ReadFrom(reader);
                foreach (JProperty property in jObject.Properties())
                {
                    string name = property.Name;
                    var value = property.Value;

                    JToken jToken = property.Value;
                    ReadJsonRecursive(duplicateKeyDictionary, jToken);


                }
            }
            return duplicateKeyDictionary;
        }

        private void ReadJsonRecursive(List<KeyValuePair<string, I18n>> dict, JToken jToken)
        {
            if (jToken.Type == JTokenType.Object)
            {
                var obj = (JObject)jToken;
                foreach(var prop in obj.Properties())
                {
                    ReadJsonRecursive(dict, prop);
                }
            }
            else if (jToken.Type == JTokenType.Property)
            {
                var obj = (JProperty)jToken;
                dict.Add(new KeyValuePair<string, I18n>(jToken.Path.Split('.')[0], new I18n
                {
                    Key = obj.Name,
                    Value = obj.Value.ToString()
                }));
            }
            else if(jToken.Type == JTokenType.String)
            {
                var obj = (string)jToken;
                dict.Add(new KeyValuePair<string, I18n>(jToken.Path.ToString(), new I18n
                {
                    Value = obj,
                    Key = jToken.Path.ToString()
                }));
            }

        }
        
    }
}
