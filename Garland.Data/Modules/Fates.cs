﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game = SaintCoinach.Xiv;

namespace Garland.Data.Modules
{
    public class Fates : Module
    {
        private static string[] _separator = new string[] { ", " };

        public override string Name => "FATEs";
        private Dictionary<int, dynamic> _fateDataById = new Dictionary<int, dynamic>();

        public override void Start()
        {
            ImportSupplementalData();

            Dictionary<int, Game.Fate> iFateByID = new Dictionary<int, Game.Fate>();
            foreach (var iFate in _builder.InterSheet<Game.Fate>())
                iFateByID[iFate.Key] = iFate;
            foreach (var sFate in _builder.Sheet<Game.Fate>())
            {
                iFateByID.TryGetValue(sFate.Key, out var iFate);
                BuildFate(sFate, iFate);
            }    
        }

        void ImportSupplementalData()
        {
            var lines = Utils.Tsv(Path.Combine(Config.SupplementalPath, "FFXIV Data - Fates.tsv"));
            foreach (var line in lines.Skip(1))
            {
                try
                {
                    var name = line[0];
                    var id = int.Parse(line[1]);
                    var zone = line[2];
                    var coords = line[3];
                    var patch = line[4]; // Unused
                    var rewardItemNameStr = line[5];

                    dynamic fate = new JObject();
                    fate.name = name;
                    fate.id = id;

                    if (zone != "")
                    {
                        fate.zoneid = _builder.Db.LocationIdsByEnName[zone];
                    }

                    if (coords != "")
                        fate.coords = new JArray(Utils.FloatComma(coords));

                    if (rewardItemNameStr != "")
                    {
                        var rewardItemNames = rewardItemNameStr.Split(_separator, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var rewardItemName in rewardItemNames)
                        {
                            var item = _builder.Db.ItemsByEnName[rewardItemName];
                            //var item = _builder.Db.ItemsById[clayManager.getItemID(rewardItemName)];
                            if (item.fates == null)
                                item.fates = new JArray();
                            item.fates.Add(id);

                            if (fate.items == null)
                                fate.items = new JArray();
                            fate.items.Add((int)item.id);
                            _builder.Db.AddReference(item, "fate", id, false);
                        }
                    }

                    _fateDataById[(int)fate.id] = fate;
                }
                catch (Exception e)
                {
                    DatabaseBuilder.PrintLine($"Fate Line Error: {line}");
                    if (System.Diagnostics.Debugger.IsAttached)
                        System.Diagnostics.Debugger.Break();
                }
            }
        }

        void BuildFate(Game.Fate sFate, Game.Fate iFate)
        {
            if (string.IsNullOrEmpty(sFate.Name.ToString()) || sFate.MaximumClassJobLevel <= 1)
                return;

            dynamic fate = new JObject();
            fate.id = sFate.Key;
            _builder.Localize.Strings((JObject)fate, sFate, iFate, false, x => Utils.RemoveLineBreaks(Utils.SanitizeTags(x)), "Name");
            _builder.Localize.HtmlStrings(fate, sFate, iFate, "Description");
            fate.patch = PatchDatabase.Get("fate", sFate.Key);
            fate.lvl = sFate.ClassJobLevel;
            fate.maxlvl = sFate.MaximumClassJobLevel;
            fate.type = MapIconToFateType(sFate.Key, sFate.Name, Utils.GetIconId(sFate.MapIcon));

            if (_fateDataById.TryGetValue(sFate.Key, out var data))
            {
                if (data.zoneid != null)
                {
                    fate.zoneid = data.zoneid;
                    _builder.Db.AddLocationReference((int)fate.zoneid);
                }
                //else
                //    System.Diagnostics.Debug.WriteLine("FATE " + name + " has no zone");

                if (data.coords != null)
                {
                    var coords = ((JArray)data.coords).Select(t => (int)t).ToArray();
                    fate.coords = new JArray(coords);
                }

                if (data.items != null)
                {
                    fate.items = data.items;
                    foreach (int itemId in fate.items)
                        _builder.Db.AddReference(fate, "item", itemId, false);
                }
            }

            _builder.Db.Fates.Add(fate);
        }

        static string MapIconToFateType(int key, string name, int mapIconId)
        {
            switch (mapIconId)
            {
                case 60501: return "Slay Enemies";
                case 60502: return "Notorious Monster";
                case 60503: return "Gather";
                case 60504: return "Defense";
                case 60505: return "Escort";
                case 60506: return "Path";
                case 60958: return "EurekaNM";

                case 60508: return "Festival"; 
                case 60994: return "Rebuild";
                case 63926: return "Carnival";

                case 60801:
                case 63914: return "Bozjan Fight";

                case 60802:
                case 63915: return "Bozjan Monster";

                case 60803:
                case 63916: return "Bozjan Gather";

                case 60804:
                case 63917: return "Bozjan Defense";

                default:
                    DatabaseBuilder.PrintLine($"Unknown fate type: {key}, {name}, {mapIconId}");
                    return "Unknown";
            }
        }
    }
}
