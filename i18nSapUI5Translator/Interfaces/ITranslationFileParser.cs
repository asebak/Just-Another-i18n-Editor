using i18nSapUI5Translator.Classes;
using System.Collections.Generic;

namespace i18nSapUI5Translator.Interfaces
{
    public interface ITranslationFileParser
    {
        string FileExt { get; }
        string Locale { get; }
        List<KeyValuePair<string, I18n>> ReadFile(string fileName);
    }
}
