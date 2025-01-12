﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Garland.Data.Helpers;
using Saint = SaintCoinach.Xiv;

namespace Garland.Data.Modules
{
    public class OtherItemSources : Module
    {

        public override string Name => "Other Item Sources";
        private static readonly TranslationHelper itemHelper = new TranslationHelper("Item");
        private static readonly TranslationHelper mobHelper = new TranslationHelper("BNpcName", "Singular");
        private static readonly TranslationHelper instanceHelper = new TranslationHelper("ContentFinderCondition", "Name", "Content");
        private static readonly TranslationHelper locationHelper = new TranslationHelper("PlaceName");
        private static readonly TranslationHelper ventureNameHelper = new TranslationHelper("RetainerTaskRandom");
        private static readonly TranslationHelper ventureHelper = new TranslationHelper("RetainerTask", "Task");
        private static readonly TranslationHelper airshipHelper = new TranslationHelper("AirshipExplorationPoint", "Name{Short}");
        private static readonly TranslationHelper submarineHelper = new TranslationHelper("SubmarineExploration", "Destination");

        public override void Start()
        {
            
            var Items = Utils.Tsv(Path.Combine(Config.SupplementalPath, "FFXIV Data - Items.tsv")).Skip(1);
            var lodestones = Utils.Tsv(Path.Combine(Config.SupplementalPath, "lodestones.tsv")).Skip(1);
            var lines = Items.Concat(lodestones);
            foreach (var line in lines)
            {
                var type = line[1];
                var args = line.Skip(2).Where(c => c != "").ToArray();
                var itemName = line[0];

                // Jump over comments
                if (itemName.StartsWith("#"))
                    continue;

                try
                {
                    var _id = itemHelper.GetID(itemName);
                    var item = _builder.Db.ItemsById[_id];

                    switch (type)
                    {
                        case "Desynth":
                            BuildDesynth(item, args);
                            break;

                        case "Reduce":
                            BuildReduce(item, args);
                            break;

                        case "Loot":
                            BuildLoot(item, args);
                            break;

                        case "Venture":
                            BuildVentures(item, args);
                            break;

                        case "Node":
                            BuildNodes(item, args);
                            break;

                        case "FishingSpot":
                            BuildFishingSpots(item, args);
                            break;

                        case "Instance":
                            BuildInstances(item, args);
                            break;

                        case "Voyage":
                            BuildVoyages(item, args);
                            break;

                        case "Gardening":
                            BuildGardening(item, args);
                            break;

                        case "Drop":
                            BuildDrop(item, args);
                            break;

                        case "Other":
                            BuildOther(item, args);
                            break;

                        default:
                            {
                                var joinedArgs = string.Join(", ", args);
                                DatabaseBuilder.PrintLine($"Error importing supplemental source '{itemName}' with args '{joinedArgs}': Unsupported type '{type}'");
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    var joinedArgs = string.Join(", ", args);
                    DatabaseBuilder.PrintLine($"Error importing supplemental source '{itemName}' with args '{joinedArgs}': {ex.Message}");
                    //if (System.Diagnostics.Debugger.IsAttached)
                    //System.Diagnostics.Debugger.Break();
                }
            }
        }

        void BuildDrop(dynamic item, string[] sources)
        {
            if (item.drops == null)
                item.drops = new JArray();

            foreach (string mobId in sources)
            {
                try
                {
                    _builder.Db.MobsByLodestoneId.TryGetValue(mobId, out var mob);
                    if (mob == null)
                    {
                        DatabaseBuilder.PrintLine($"Supplemental : No mob data '{mobId}' of '{item.name}'");
                        continue;
                    }
                    long mobFullKey = mob.id;

                    if (!item.drops.Contains(mobFullKey))
                    {
                        item.drops.Add(mobFullKey);
                        _builder.Db.AddReference(item, "mob", mobFullKey.ToString(), false);
                    }
                }
                catch (Exception ex)
                {
                    DatabaseBuilder.PrintLine($"Error importing supplemental source Mob '{mobId}' of '{item.name}' : {ex.Message}");
                }
            }
        }

        void BuildOther(dynamic item, string[] sources)
        {
            // For unstructured source strings.
            if (item.other != null)
                throw new InvalidOperationException("item.other already exists.");

            if (sources.Length > 0)
                item.other = new JArray(sources);
        }

        void BuildGardening(dynamic item, string[] sources)
        {
            foreach (string seedItemName in sources)
            {
                if (!itemHelper.TryGetID(seedItemName, out var _id))
                {
                    DatabaseBuilder.PrintLine($"Error with parsing '{seedItemName}' of {item.name}");
                    continue;
                }

                var seedItem = _builder.Db.ItemsById[_id];
                Items.AddGardeningPlant(_builder, seedItem, item);
            }
        }

        void BuildDesynth(dynamic item, string[] sources)
        {
            if (item.desynthedFrom == null)
                item.desynthedFrom = new JArray();

            foreach (string itemName in sources)
            {
                
                if (!itemHelper.TryGetID(itemName, out var _id))
                {
                    DatabaseBuilder.PrintLine($"Error with parsing '{itemName}' of {item.name}");
                    continue;
                }

                var desynthItem = _builder.Db.ItemsById[_id];
                item.desynthedFrom.Add((int)desynthItem.id);
                _builder.Db.AddReference(item, "item", (int)desynthItem.id, false);

                if (desynthItem.desynthedTo == null)
                    desynthItem.desynthedTo = new JArray();
                desynthItem.desynthedTo.Add((int)item.id);
                _builder.Db.AddReference(desynthItem, "item", (int)item.id, false);
            }
        }

        void BuildReduce(dynamic item, string[] sources)
        {
            if (item.reducedFrom == null)
                item.reducedFrom = new JArray();

            foreach (string sourceItemName in sources)
            {
                if (!itemHelper.TryGetID(sourceItemName, out var _id))
                {
                    DatabaseBuilder.PrintLine($"Error with parsing '{sourceItemName}' of {item.name}");
                    continue;
                }

                var sourceItem = _builder.Db.ItemsById[_id];
                if (sourceItem.reducesTo == null)
                    sourceItem.reducesTo = new JArray();
                sourceItem.reducesTo.Add((int)item.id);
                item.reducedFrom.Add((int)sourceItem.id);

                _builder.Db.AddReference(sourceItem, "item", (int)item.id, false);
                _builder.Db.AddReference(item, "item", (int)sourceItem.id, true);

                // Set aetherial reduction info on the gathering node views.
                // Bell views
                foreach (var nodeView in _builder.Db.NodeViews)
                {
                    foreach (var slot in nodeView.items)
                    {
                        if (slot.id == sourceItem.id && slot.reduce == null)
                        {
                            slot.reduce = new JObject();
                            slot.reduce.item = item.name;
                            slot.reduce.icon = item.icon;
                        }
                    }
                }

                // Database views
                foreach (var node in _builder.Db.Nodes)
                {
                    foreach (var slot in node.items)
                    {
                        if (slot.id == sourceItem.id && slot.reduceId == null)
                        {
                            slot.reduceId = (int)item.id;
                            _builder.Db.AddReference(node, "item", (int)item.id, false);
                        }
                    }
                }
            }
        }

        void BuildLoot(dynamic item, string[] sources)
        {
            if (item.treasure == null)
                item.treasure = new JArray();

            var generators = sources.Select(j => itemHelper.GetID(j)).ToArray();
            foreach (var _id in generators)
            {
                var generator = _builder.Db.ItemsById[_id];
                if (generator.loot == null)
                    generator.loot = new JArray();

                generator.loot.Add((int)item.id);
                _builder.Db.AddReference(generator, "item", (int)item.id, false);

                item.treasure.Add((int)generator.id);
                _builder.Db.AddReference(item, "item", (int)generator.id, true);
            }
        }

        void BuildVentures(dynamic item, string[] sources)
        {
            if (item.ventures != null)
                throw new InvalidOperationException("item.ventures already exists.");
            var ventureIds = sources
                .Select(j => ventureHelper.GetID(ventureNameHelper.GetID(j).ToString()))
                .ToArray();
            item.ventures = new JArray(ventureIds);
        }

        void BuildVoyages(dynamic item, string[] sources)
        {
            if (item.voyages != null)
                throw new InvalidOperationException("item.voyages already exists.");

            var _airshipSheet = _builder.Sheet<Saint.AirshipExplorationPoint>();
            var _submarineSheet = _builder.Sheet<Saint.SubmarineExploration>();

            var _sources = new JArray();
            foreach (var source in sources)
            {
                if (airshipHelper.ToLower().TryGetID(source.ToLower(), out var _id))
                {
                    var _voyage = _airshipSheet.First(voyage => voyage.Key == _id);
                    _sources.Add(_voyage.Name.ToString());
                } else if (submarineHelper.ToLower().TryGetID(source.ToLower(), out var __id))
                {
                    var _voyage = _submarineSheet.First(voyage => voyage.Key == __id);
                    _sources.Add(_voyage.Name.ToString());
                } else if (submarineHelper.ToLower().TryGetID("the " + source.ToLower(), out var ___id))
                {
                    var _voyage = _submarineSheet.First(voyage => voyage.Key == ___id);
                    _sources.Add(_voyage.Name.ToString());
                }
                else
                {
                    DatabaseBuilder.PrintLine($"Error with parsing voyage '{source}' of {item.name}");
                }
            }
            item.voyages = _sources;
        }

        void BuildNodes(dynamic item, string[] sources)
        {
            if (item.nodes == null)
                item.nodes = new JArray();

            foreach (var id in sources.Select(int.Parse))
            {
                item.nodes.Add(id);

                int itemId = item.id;

                var node = _builder.Db.Nodes.First(n => n.id == id);
                dynamic w = new JObject();
                w.id = itemId;
                w.slot = "?"; // Only hidden items here
                node.items.Add(w);

                _builder.Db.AddReference(node, "item", itemId, false);
                _builder.Db.AddReference(item, "node", id, true);
            }
        }

        void BuildFishingSpots(dynamic item, string[] sources)
        {
            if (item.fishingSpots == null)
                item.fishingSpots = new JArray();
            
            foreach (var name in sources)
            {
                if (!locationHelper.TryGetID(name, out var _locationId)) {
                    DatabaseBuilder.PrintLine($"Error with parsing '{name}' of {item.name}");
                    continue;
                }

                var _name = _builder.Db.LocationsById[_locationId].name;
                dynamic w = new JObject();
                w.id = (int)item.id;
                w.lvl = (int)item.ilvl;

                var spot = _builder.Db.FishingSpots.First(f => f.name == _name);
                item.fishingSpots.Add((int)spot.id);
                spot.items.Add(w);

                _builder.Db.AddReference(spot, "item", (int)item.id, false);
            }
        }

        void BuildInstances(dynamic item, string[] sources)
        {
            if (item.instances == null)
                item.instances = new JArray();

            int itemId = item.id;
            foreach (var name in sources)
            {                   
                // Fix wrong name
                var _name = name
                    .Replace("Heaven-on-High (", "Heaven-on-High  (")
                    .Replace("Tam-Tara", "Tam–Tara")
                    .Replace("Toto-Rak", "Toto–Rak")
                    .ToLower();
                if (!instanceHelper.ToLower().TryGetID(_name, out var _id))
                {
                    DatabaseBuilder.PrintLine($"Error with parsing '{name}' of {item.name}");
                    continue;
                }
                var instance = _builder.Db.InstancesById[_id];
                int instanceId = instance.id;

                if (!item.instances.Contains(instanceId))
                {
                    item.instances.Add(instanceId);
                    _builder.Db.AddReference(item, "instance", instanceId, true);
                }

                if (instance.rewards == null)
                    instance.rewards = new JArray();

                if (!instance.rewards.Contains(instanceId))
                {
                    instance.rewards.Add(itemId);
                    _builder.Db.AddReference(instance, "item", itemId, false);
                }
            }
        }
    }
}