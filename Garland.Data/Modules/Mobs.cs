using Garland.Data.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SaintCoinach.Xiv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Saint = SaintCoinach.Xiv;

namespace Garland.Data.Modules
{
    public class Mobs : Module
    {
        Dictionary<long, BNpcData> _bnpcDataByFullKey = new Dictionary<long, BNpcData>();
        Dictionary<int, List<dynamic>> _bnpcDataByNameKey = new Dictionary<int, List<dynamic>>();

        public override string Name => "Mobs";

        void IndexMappyData()
        {
            var text = System.IO.File.ReadAllText(System.IO.Path.Combine(Config.SupplementalPath, "mappy.json"));
            dynamic _npcs = JsonConvert.DeserializeObject<dynamic>(text);
            foreach (var npc in _npcs)
            {
                if (npc.Type != "BNPC") continue;
                if (npc.BNpcNameID == 0) continue;
                if (npc.BNpcBaseID == 0) continue;
                if (npc.PlaceNameID == 0) continue;

                if (!_bnpcDataByNameKey.ContainsKey((int)npc.BNpcNameID))
                    _bnpcDataByNameKey[(int)npc.BNpcNameID] = new List<dynamic>();

                _bnpcDataByNameKey[(int)npc.BNpcNameID].Add(npc);
            }
        }

        void IndexMobData()
        {
            var mobHelper = new TranslationHelper("BNpcName", "Singular");
            var locationHelper = new TranslationHelper("PlaceName");

            // Lookup mob data from /Supplemental/mob.json.
            var text = System.IO.File.ReadAllText(System.IO.Path.Combine(Config.SupplementalPath, "mob.json"));
            dynamic _mobs = JsonConvert.DeserializeObject(text);
            int i = 0;
            foreach (var mob in _mobs)
            {
                i++;
                if (!mobHelper.ToLower().TryGetID(mob.name.ToString().ToLower(), out int _nameKey))
                {
                    DatabaseBuilder.PrintLine($"Error creating mob data - No matched name '{mob.name}'");
                    continue;
                }

                if (!locationHelper.ToLower().TryGetID(mob.zone.ToString().ToLower(), out int _zoneKey))
                {
                    DatabaseBuilder.PrintLine($"Error creating mob data - No matched zone '{mob.zone}'");
                    continue;
                }
                _bnpcDataByNameKey.TryGetValue(_nameKey, out var _mobList);
                var mappyData = _mobList?.Find(w => (int)w.PlaceNameID == _zoneKey) ?? _mobList?[0];

                var bnpcData = new BNpcData();
                bnpcData.FullKey = (mappyData?.BNpcBaseID ?? i) * 10000000000 + _nameKey;
                bnpcData.DebugName = mob.id;
                bnpcData.BNpcNameKey = _nameKey;
                bnpcData.BNpcBaseKey = mappyData?.BNpcBaseID ?? i;

                var location = new BNpcLocation();
                location.PlaceNameKey = _zoneKey;
                location.X = Convert.ToDouble(mob.x);
                location.Y = Convert.ToDouble(mob.y);
                location.Z = Convert.ToDouble(mappyData?.PosZ ?? 0.0);
                location.LevelRange = mob.lv ?? "??";
                bnpcData.Locations.Add(location);

                _bnpcDataByFullKey[bnpcData.FullKey] = bnpcData;
            }
        }

        public override void Start()
        {
            IndexMappyData();
            IndexMobData();

            var sBNpcNames = _builder.Sheet<Saint.BNpcName>();

            foreach (var bnpcData in _bnpcDataByFullKey.Values)
            {
                // No unnamed mobs right now.  Need to figure out how to fit
                // them in the base key - name key id structure.
                var sBNpcName = sBNpcNames[bnpcData.BNpcNameKey];

                dynamic mob = new JObject();
                mob.id = bnpcData.FullKey;

                _builder.Localize.Column((JObject)mob, sBNpcName, "Singular", "name", Utils.CapitalizeWords);

                // todo: Store in a location array.
                // Technically this is per-location, but for now the first record wins.
                var location = bnpcData.Locations.FirstOrDefault();
                if (location != null)
                {
                    if (location.X != 0)
                        mob.coords = new JArray(Math.Round(location.X, 2), Math.Round(location.Y, 2), Math.Round(location.Z, 2));

                    mob.zoneid = location.PlaceNameKey;
                    _builder.Db.AddLocationReference(location.PlaceNameKey);
                    mob.lvl = location.LevelRange;
                }

                if (_builder.ItemDropsByMobId.TryGetValue(bnpcData.FullKey, out var itemDropIds))
                {
                    mob.drops = new JArray(itemDropIds);
                    _builder.Db.AddReference(mob, "item", itemDropIds, false);
                    foreach (var itemId in itemDropIds)
                        _builder.Db.AddReference(_builder.Db.ItemsById[itemId], "mob", bnpcData.FullKey.ToString(), true);
                }

                if (_builder.InstanceIdsByMobId.TryGetValue(bnpcData.FullKey, out var instanceId))
                {
                    mob.instance = instanceId;
                    _builder.Db.AddReference(mob, "instance", instanceId, false);
                    _builder.Db.AddReference(_builder.Db.InstancesById[instanceId], "mob", bnpcData.FullKey.ToString(), false);
                }

                var currency = _builder.GetBossCurrency(bnpcData.FullKey);
                if (currency != null)
                    mob.currency = currency;

                // todo: modelchara info
                // todo: NpcEquip for equipment
                // todo: BNpcCustomize for appearance
                // todo: link all other mobs with this appearance
                // todo: link all other mobs with this name

                _builder.Db.Mobs.Add(mob);
                _builder.Db.MobsByLodestoneId[bnpcData.DebugName] = mob;
            }
        }

        class BNpcData
        {
            public int BNpcBaseKey;
            public int BNpcNameKey;
            public long FullKey;
            public string DebugName;
            public bool HasSpecialSpawnRules;
            public List<BNpcLocation> Locations = new List<BNpcLocation>();

            public override string ToString() => DebugName;
        }

        class BNpcLocation
        {
            public int PlaceNameKey;
            public double X;
            public double Y;
            public double Z;
            public double Radius;
            public string LevelRange;

            public override string ToString() => $"({X}, {Y}, {Z}, r{Radius})";
        }
    }
}
