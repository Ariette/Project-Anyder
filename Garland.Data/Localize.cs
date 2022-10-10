using Newtonsoft.Json.Linq;
using SaintCoinach;
using SaintCoinach.Ex;
using SaintCoinach.Text;
using SaintCoinach.Xiv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garland.Data
{
    public class Localize
    {
        private ARealmReversed _realm;
        private readonly XivCollection _data;
        private readonly Tuple<string, Language>[] _langs;

        public Localize(ARealmReversed realm)
        {
            _realm = realm;
            _data = realm.GameData;
            _langs = new Tuple<string, Language>[]
            {
                Tuple.Create(Language.Korean.GetCode(), Language.Korean)
            };
        }

        public void Strings(JObject obj, IXivRow row, Func<XivString, string> transform, params string[] cols)
        {
            var currentLang = _data.ActiveLanguage;

            foreach (var langTuple in _langs)
            {
                var lang = langTuple.Item2;
                _data.ActiveLanguage = lang;

                foreach (var col in cols)
                {
                    var value = row[col];
                    if (value is XivString && string.IsNullOrEmpty((XivString)value))
                        continue;

                    var sanitizedCol = col.ToLower().Replace("{", "").Replace("}", "");
                    obj[sanitizedCol] = transform == null ? (value.ToString().TrimEnd()) : transform((XivString)value);
                }
            }

            _data.ActiveLanguage = currentLang;
        }

        public void Strings(JObject obj, IXivRow row, params string[] cols)
        {
            Strings(obj, row, null, cols);
        }

        public void HtmlStrings(JObject obj, IXivRow row, params string[] cols)
        {
            Strings(obj, row, HtmlStringFormatter.Convert, cols);
        }

        public void Column(JObject obj, IXivRow row, string fromColumn, string toColumn, Func<XivString, string> transform = null)
        {
            var currentLang = _data.ActiveLanguage;

            foreach (var langTuple in _langs)
            {
                var lang = langTuple.Item2;
                _data.ActiveLanguage = lang;

                var value = row[fromColumn];
                var toValue = transform == null ? (value.ToString()) : transform((XivString)value);
                if (string.IsNullOrEmpty(toValue))
                    continue;

                obj[toColumn] = toValue;
            }

            _data.ActiveLanguage = currentLang;
        }
    }
}
