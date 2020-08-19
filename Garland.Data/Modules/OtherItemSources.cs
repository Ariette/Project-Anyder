using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garland.Data.Modules
{
    public class OtherItemSources : Module
    {
        Dictionary<string, dynamic> _venturesByName;

        public override string Name => "Other Item Sources";

        public override void Start()
        {
            _venturesByName = _builder.Db.Ventures.Where(v => v.name != null).ToDictionary(v => (string)v.name);

            var Items = Utils.Tsv(Path.Combine(Config.SupplementalPath, "FFXIV Data - Items.tsv")).Skip(1);
            var lodestones = Utils.Tsv(Path.Combine(Config.SupplementalPath, "lodestones.tsv")).Skip(1);
            var lines = Items.Concat(lodestones);
            foreach (var line in lines)
            {
                var type = line[1];
                var args = line.Skip(2).Where(c => c != "").ToArray();
                var itemName = line[0];

                if (itemName.StartsWith("#")) continue;

                try
                {
                    var item = _builder.Db.ItemsByName.ContainsKey(itemName) ? _builder.Db.ItemsByName[itemName] : _builder.Db.ItemsByName[itemName + " "]; // Some item names in database are not trimmed...

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
                    //    System.Diagnostics.Debugger.Break();
                }
            }
        }

        void BuildOther(dynamic item, string[] sources)
        {
            // For unstructured source strings.
            if (item.other != null)
                throw new InvalidOperationException("item.other already exists.");

            item.other = new JArray(sources);
        }

        void BuildGardening(dynamic item, string[] sources)
        {
            foreach (string seedItemName in sources)
            {
                try
                {
                    var seedItem = _builder.Db.ItemsByName[seedItemName];
                    Items.AddGardeningPlant(_builder, seedItem, item);
                }
                catch (Exception ex)
                {
                    DatabaseBuilder.PrintLine($"Error importing supplemental source Gardening '{seedItemName}' of '{item.en.name}' : {ex.Message}");
                }
            }
        }

        void BuildDesynth(dynamic item, string[] sources)
        {
            if (item.desynthedFrom == null)
                item.desynthedFrom = new JArray();

            foreach (string itemName in sources)
            {
                try
                {
                    var desynthItem = _builder.Db.ItemsByName[itemName];
                    item.desynthedFrom.Add((int)desynthItem.id);
                    _builder.Db.AddReference(item, "item", (int)desynthItem.id, false);

                    if (desynthItem.desynthedTo == null)
                        desynthItem.desynthedTo = new JArray();
                    desynthItem.desynthedTo.Add((int)item.id);
                    _builder.Db.AddReference(desynthItem, "item", (int)item.id, false);
                }
                catch (Exception ex)
                {
                    DatabaseBuilder.PrintLine($"Error importing supplemental source Desynth '{itemName}' of '{item.en.name}' : {ex.Message}");
                }
            }
        }

        void BuildDrop(dynamic item, string[] sources)
        {
            if (item.drops == null)
                item.drops = new JArray();

            foreach (string mobName in sources)
            {
                try
                {
                    var mob = _builder.Db.Mobs.First(n => String.Equals(n.en.name.ToString(), mobName, StringComparison.CurrentCultureIgnoreCase));
                    if (!item.drops.Contains((long)mob.id))
                    {
                        item.drops.Add((long)mob.id);
                        _builder.Db.AddReference(item, "mob", (string)mob.id, false);
                    }

                    if (mob.drops == null)
                        mob.drops = new JArray();

                    if (!mob.drops.Contains((int)item.id))
                    {
                        mob.drops.Add((int)item.id);
                        _builder.Db.AddReference(mob, "item", (int)item.id, false);
                    }
                }
                catch (Exception ex)
                {
                    DatabaseBuilder.PrintLine($"Error importing supplemental source Mob '{mobName}' of '{item.en.name}' : {ex.Message}");
                }
            }
        }

        void BuildReduce(dynamic item, string[] sources)
        {
            if (item.reducedFrom == null)
                item.reducedFrom = new JArray();

            foreach (string sourceItemName in sources)
            {
                try
                {
                    var sourceItem = _builder.Db.ItemsByName[sourceItemName];
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
                                slot.reduce.item = item.en.name;
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
                catch (Exception ex)
                {
                    DatabaseBuilder.PrintLine($"Error importing supplemental source Reduce '{sourceItemName}' of '{item.en.name}' : {ex.Message}");
                }
            }
        }

        void BuildLoot(dynamic item, string[] sources)
        {
            if (item.treasure == null)
                item.treasure = new JArray();

            var generators = sources.Select(j => _builder.Db.ItemsByName[j]).ToArray();
            foreach (var generator in generators)
            {
                if (generator.loot == null)
                    generator.loot = new JArray();

                if (!generator.loot.Contains((int)item.id))
                {
                    generator.loot.Add((int)item.id);
                    _builder.Db.AddReference(generator, "item", (int)item.id, false);
                }
                if (!item.treasure.Contains((int)generator.id))
                {
                    item.treasure.Add((int)generator.id);
                    _builder.Db.AddReference(item, "item", (int)generator.id, true);
                }
            }
        }

        void BuildVentures(dynamic item, string[] sources)
        {
            var ventureIds = sources.Select(j => (int)_venturesByName[j].id);
            if (item.ventures == null)
            {
                item.ventures = new JArray(ventureIds);
            }
            else
            {
                item.ventures.Merge(new JArray(ventureIds), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });
            }
        }

        void BuildVoyages(dynamic item, string[] sources)
        {
            if (item.voyages == null)
            {
                item.voyages = new JArray(sources);
            }
            else
            {
                item.voyages.Merge(new JArray(sources), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });
            }
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
                dynamic w = new JObject();
                w.id = (int)item.id;
                w.lvl = (int)item.ilvl;

                var spot = _builder.Db.FishingSpots.First(f => f.name == name);
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
                try
                {
                    var instance = _builder.Db.Instances.First(i => i.en.name == name);
                    int instanceId = instance.id;
                    if (instance.rewards == null)
                        instance.rewards = new JArray();

                    if (!instance.rewards.Contains(itemId))
                    {
                        instance.rewards.Add(itemId);
                        _builder.Db.AddReference(instance, "item", itemId, false);
                    }
                    
                    if (!item.instances.Contains(instanceId))
                    {
                        item.instances.Add(instanceId);
                        _builder.Db.AddReference(item, "instance", instanceId, true);
                    }
                }
                catch (Exception ex)
                {
                    DatabaseBuilder.PrintLine($"Error importing supplemental source Reduce '{name}' of '{item.en.name}' : {ex.Message}");
                }
            }
        }
    }
}