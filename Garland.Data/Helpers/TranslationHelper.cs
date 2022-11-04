using Garland.Data.Models;
using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Garland.Data.Helpers
{
    public class TranslationHelper
    {
        private readonly Dictionary<string, int> nameMap;
        private static readonly Dictionary<string, Dictionary<string, int>> sheetCache = new Dictionary<string, Dictionary<string, int>>();

        public TranslationHelper(string sheetName, string nameField = "Name", string keyField = "#")
        {
            nameMap = ImportCsv(Path.Combine(Config.BasePath, "lib/ffxiv-datamining/csv", sheetName + ".csv"), nameField, keyField);
        }

        public TranslationHelper(Dictionary<string, int> _nameMap)
        {
            nameMap = _nameMap;
        }

        public int GetID(string name)
        {
            return this.nameMap[name];
        }

        public bool TryGetID(string name, out int id)
        {
            return this.nameMap.TryGetValue(name, out id);
        }
        public TranslationHelper ToLower()
        {
            var newNameMap = new Dictionary<string, int>();
            foreach (var item in this.nameMap)
            {
                try
                {
                    newNameMap.Add(item.Key.ToLower(), item.Value);
                } catch { }
            }
            return new TranslationHelper(newNameMap);
        }

        private static Dictionary<string, int> ImportCsv(string path, string nameField, string keyField)
        {
            if (sheetCache.TryGetValue(path, out Dictionary<string, int> cachedMap))
            {
                return cachedMap;
            }
            else
            {
                using (var reader = new StreamReader(path))
                {
                    // Get header
                    reader.ReadLine();
                    var header = reader.ReadLine();
                    reader.ReadLine();
                    // and then keep parsing csv
                    var csv = new CachedCsvReader(reader, false);
                    csv.Columns = header
                        .Split(',')
                        .Select((w, i) =>
                        {
                            if (string.IsNullOrEmpty(w))
                                return new Column { Name = "unknown" + i.ToString(), Type = typeof(string) };
                            return new Column { Name = w, Type = typeof(string) };
                        })
                        .ToList();
                    var map = GenerateMap(csv, nameField, keyField);
                    sheetCache.Add(path, map);
                    return map;
                }
            }
        }

        private static Dictionary<string, int> GenerateMap(CachedCsvReader sheet, string nameField, string keyField)
        {
            var nameMap = new Dictionary<string, int>();
            var keyIndex = sheet.GetFieldIndex(keyField);
            var nameIndex = sheet.GetFieldIndex(nameField);
            while (sheet.ReadNextRecord())
            {
                try
                {
                    var _name = sheet[nameIndex].Replace("<Emphasis>", "").Replace("</Emphasis>", "");
                    var _id = int.Parse(sheet[keyIndex]);
                    if (!nameMap.ContainsKey(_name))
                        nameMap.Add(_name, _id);
                }
                finally
                {
                    // Do something if needed
                }
            }
            return nameMap;
        }
    }
}