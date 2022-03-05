﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public override string Name => "Mobs";

        void IndexMobData()
        {
            var viewsByNodeId = new Dictionary<int, dynamic>();
            // Lookup mob data from /Supplemental/mob.json.
            var text = System.IO.File.ReadAllText(System.IO.Path.Combine(Config.SupplementalPath, "mob.json"));
            dynamic _mobs = JsonConvert.DeserializeObject(text);
            var mobList = _builder.Sheet<Saint.BNpcName>();
            foreach (var mobItem in mobList)
            {
                if (_mobs[mobItem.Singular.ToString().ToLower()] != null)
                {
                    int i = 1;
                    foreach (var mob in _mobs[mobItem.Singular.ToString().ToLower()])
                    {
                        var FullKey = ( 10000000000 * i ) + mobItem.Key;
                        if (!_bnpcDataByFullKey.TryGetValue(FullKey, out var bnpcData))
                        {
                            bnpcData = new BNpcData();
                            bnpcData.FullKey = FullKey;
                            bnpcData.DebugName = mob.name;
                            bnpcData.BNpcNameKey = mobItem.Key;
                            bnpcData.BNpcBaseKey = i;
                            var location = new BNpcLocation();
                            location.PlaceNameKey = _builder.Sheet<Saint.PlaceName>().First(W => W.Name.ToString() == mob.zone.ToString()).Key;
                            location.X = Convert.ToDouble(mob.x);
                            location.Y = Convert.ToDouble(mob.y);
                            location.Z = 0.0;
                            location.LevelRange = mob.lv ?? "??";
                            bnpcData.Locations.Add(location);
                            _bnpcDataByFullKey[FullKey] = bnpcData;
                        }
                        i++;
                    }
                }
            }
        }

        void IndexLibraData()
        {
            var sTerritoryTypes = _builder.Sheet<Saint.TerritoryType>();

            foreach (var lBNpcName in _builder.Libra.Table<Libra.BNpcName>())
            {
                if (!_bnpcDataByFullKey.TryGetValue(lBNpcName.Key, out var bnpcData))
                {
                    bnpcData = new BNpcData();
                    bnpcData.FullKey = lBNpcName.Key;
                    bnpcData.BNpcBaseKey = (int)(lBNpcName.Key / 10000000000);
                    bnpcData.BNpcNameKey = (int)(lBNpcName.Key % 10000000000);
                    bnpcData.DebugName = lBNpcName.Index_en;
                    _bnpcDataByFullKey[lBNpcName.Key] = bnpcData;
                }

                dynamic data = JsonConvert.DeserializeObject((string)lBNpcName.data);
                if (data.nonpop != null)
                    bnpcData.HasSpecialSpawnRules = true;

                if (data.region != null)
                {
                    var area = Utils.GetPair(data.region);
                    var zone = Utils.GetPair(area.Value);
                    var levelRange = (JArray)zone.Value;

                    var location = new BNpcLocation();
                    location.PlaceNameKey = int.Parse(zone.Key);
                    location.LevelRange = string.Join(" - ", levelRange.Select(v => (string)v));
                    bnpcData.Locations.Add(location);
                }
            }
        }

        void IndexSapphireData()
        {
            var sTerritoryTypes = _builder.Sheet<Saint.TerritoryType>();

            var sql =
                @"SELECT AVG(spawnpoint.x) x, AVG(spawnpoint.y) y, AVG(spawnpoint.z) z, AVG(spawnpoint.r) r, spawnpoint.spawnGroupId,
                        spawngroup.level, spawngroup.maxHp, spawngroup.territoryTypeId,
                        bnpctemplate.bNPCBaseId, bnpctemplate.bNPCNameId, bnpctemplate.aggressionMode, bnpctemplate.enemyType, bnpctemplate.name
                    FROM bnpctemplate
                    JOIN spawngroup ON spawngroup.bNpcTemplateId = bnpctemplate.Id
                    JOIN spawnpoint ON spawnpoint.spawnGroupId = spawngroup.id
                    GROUP BY level, maxHp, territoryTypeId, bNPCBaseId, bNPCNameId, aggressionMode, enemyType, name, spawnGroupId
                    ORDER BY bNPCBaseId, bNPCNameId, spawnGroupId";

            SqlDatabase.WithReader(Config.SapphireConnectionString, sql,
                r => IndexSapphireDataCore(r, sTerritoryTypes));
        }

        void IndexSapphireDataCore(MySql.Data.MySqlClient.MySqlDataReader reader, Saint.IXivSheet<Saint.TerritoryType> sTerritoryTypes)
        {
            BNpcData bnpcData = null;
            while (reader.Read())
            {
                var nameKey = reader.GetInt32("bNPCNameId");
                var baseKey = reader.GetInt32("bNPCBaseId");
                if (bnpcData == null || nameKey != bnpcData.BNpcNameKey || baseKey != bnpcData.BNpcBaseKey)
                {
                    var fullKey = nameKey + baseKey * 10000000000;
                    if (!_bnpcDataByFullKey.TryGetValue(fullKey, out bnpcData))
                    {
                        bnpcData = new BNpcData();
                        bnpcData.BNpcNameKey = nameKey;
                        bnpcData.BNpcBaseKey = baseKey;
                        bnpcData.FullKey = fullKey;
                        bnpcData.DebugName = reader.GetString("name");
                    }
                }

                // Incomplete location data may already exist from Libra.  Fill in.
                var territoryTypeId = reader.GetInt32("territoryTypeId");
                var sTerritoryType = sTerritoryTypes[territoryTypeId];
                var sMap = sTerritoryType.Map;

                var bnpcLocation = bnpcData.Locations.FirstOrDefault(l => l.X == 0 && l.PlaceNameKey == sTerritoryType.PlaceName.Key);
                if (bnpcLocation == null)
                {
                    bnpcLocation = new BNpcLocation();
                    bnpcLocation.PlaceNameKey = sTerritoryType.PlaceName.Key;
                    bnpcData.Locations.Add(bnpcLocation);
                }

                bnpcLocation.Radius = reader.GetFloat("r");
                bnpcLocation.X = sMap.ToMapCoordinate3d(reader.GetDouble("x"), sMap.OffsetX);
                // Z is intentionally used for Y here.
                bnpcLocation.Y = sMap.ToMapCoordinate3d(reader.GetDouble("z"), sMap.OffsetY);
                bnpcLocation.Z = sMap.ToMapCoordinate3d(reader.GetDouble("y"), 0);

                if (bnpcLocation.LevelRange == null)
                {
                    var level = reader.GetInt32("level");
                    bnpcLocation.LevelRange = level.ToString();
                }
            }
        }

        public override void Start()
        {
            IndexLibraData();
            IndexSapphireData();
            IndexMobData();

            var sBNpcNames = _builder.Sheet<Saint.BNpcName>();
            var sBNpcBases = _builder.Sheet<Saint.BNpcBase>();

            foreach (var bnpcData in _bnpcDataByFullKey.Values)
            {
                // No unnamed mobs right now.  Need to figure out how to fit
                // them in the base key - name key id structure.
                var sBNpcName = sBNpcNames[bnpcData.BNpcNameKey];
                var sBNpcBase = sBNpcBases[bnpcData.BNpcBaseKey];

                dynamic mob = new JObject();
                mob.id = bnpcData.FullKey;

                _builder.Localize.Column((JObject)mob, sBNpcName, "Singular", "name", Utils.CapitalizeWords);

                if (bnpcData.HasSpecialSpawnRules)
                    mob.quest = 1;

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
