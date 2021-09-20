﻿using Newtonsoft.Json;
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

            var lines = Utils.Tsv(Path.Combine(Config.SupplementalPath, "FFXIV Data - Items.tsv"));
            Clay.ClayMySQL clayManager = new Clay.ClayMySQL();
            foreach (var line in lines.Skip(1))
            {
                var type = line[1];
                var args = line.Skip(2).Where(c => c != "").ToArray();
                var itemName = line[0];

                try
                {
                    var itemId = clayManager.getItemID(itemName);
                    var item = _builder.Db.ItemsById[itemId];

                    switch (type)
                    {
                        case "Desynth":
                            BuildDesynth(clayManager, item,  args);
                            break;

                        case "Reduce":
                            BuildReduce(clayManager, item,  args);
                            break;

                        case "Loot":
                            BuildLoot(clayManager, item,  args);
                            break;

                        case "Venture":
                            BuildVentures(clayManager, item,  args);
                            break;

                        case "Node":
                            BuildNodes(item, args);
                            break;

                        case "FishingSpot":
                            BuildFishingSpots(item, args);
                            break;

                        case "Instance":
                            BuildInstances(clayManager, item, args);
                            break;

                        case "Voyage":
                            BuildVoyages(item, args);
                            break;

                        case "Gardening":
                            BuildGardening(clayManager, item,  args);
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
                    if (System.Diagnostics.Debugger.IsAttached)
                       System.Diagnostics.Debugger.Break();
                }
            }
            clayManager.Stop();
        }

        void BuildOther(dynamic item, string[] sources)
        {
            // For unstructured source strings.
            if (item.other != null)
                throw new InvalidOperationException("item.other already exists.");

            item.other = new JArray(sources);
        }

        void BuildGardening(Clay.ClayMySQL clayManager, dynamic item,  string[] sources)
        {
            foreach (string seedItemName in sources)
            {
                //var seedItem = _builder.Db.ItemsByName[seedItemName];
                var seedItem = _builder.Db.ItemsById[clayManager.getItemID(seedItemName)];
                Items.AddGardeningPlant(_builder, seedItem, item);
            }
        }

        void BuildDesynth(Clay.ClayMySQL clayManager, dynamic item,  string[] sources)
        {
            if (item.desynthedFrom == null)
                item.desynthedFrom = new JArray();

            foreach (string itemName in sources)
            {
                var desynthItem = _builder.Db.ItemsById[clayManager.getItemID(itemName)];
                //var desynthItem = _builder.Db.ItemsByName[itemName];
                item.desynthedFrom.Add((int)desynthItem.id);
                _builder.Db.AddReference(item, "item", (int)desynthItem.id, false);

                if (desynthItem.desynthedTo == null)
                    desynthItem.desynthedTo = new JArray();
                desynthItem.desynthedTo.Add((int)item.id);
                _builder.Db.AddReference(desynthItem, "item", (int)item.id, false);
            }
        }

        void BuildReduce(Clay.ClayMySQL clayManager, dynamic item, string[] sources)
        {
            if (item.reducedFrom == null)
                item.reducedFrom = new JArray();

            foreach (string sourceItemName in sources)
            {
                //var sourceItem = _builder.Db.ItemsByName[sourceItemName];
                int itemID = -1;
                try
                {
                    itemID = clayManager.getItemID(sourceItemName);
                }
                catch (NotSupportedException notFound) {
                    continue;
                }

                if (itemID == -1)
                    continue;

                var sourceItem = _builder.Db.ItemsById[itemID];
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
                            slot.reduce.item = item.chs.name;
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

        void BuildLoot(Clay.ClayMySQL clayManager, dynamic item,  string[] sources)
        {
            if (item.treasure == null)
                item.treasure = new JArray();

            //var generators = sources.Select(j => _builder.Db.ItemsByName[j]).ToArray();
            var generators = sources.Select(j => _builder.Db.ItemsById[clayManager.getItemID(j)]).ToArray();
            foreach (var generator in generators)
            {
                if (generator.loot == null)
                    generator.loot = new JArray();

                generator.loot.Add((int)item.id);
                _builder.Db.AddReference(generator, "item", (int)item.id, false);

                item.treasure.Add((int)generator.id);
                _builder.Db.AddReference(item, "item", (int)generator.id, true);
            }
        }

        void BuildVentures(Clay.ClayMySQL clayManager, dynamic item,string[] sources)
        {
            if (item.ventures != null)
                throw new InvalidOperationException("item.ventures already exists.");
            var ventureIds = sources.Select(j => (int)_venturesByName[clayManager.getRetainerTaskChs(j)].id);
            item.ventures = new JArray(ventureIds);
        }

        void BuildVoyages(dynamic item, string[] sources)
        {
            if (item.voyages != null)
                throw new InvalidOperationException("item.voyages already exists.");
            item.voyages = new JArray(sources);
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

        void BuildInstances(Clay.ClayMySQL clayManager, dynamic item,  string[] sources)
        {
            if (item.instances == null)
                item.instances = new JArray();

            int itemId = item.id;

            foreach (var name in sources)
            {
                var instance = _builder.Db.Instances.First(i => i.chs.name == clayManager.getInstanceChs(name));
                int instanceId = instance.id;
                if (instance.rewards == null)
                    instance.rewards = new JArray();
                instance.rewards.Add(itemId);
                item.instances.Add(instanceId);

                _builder.Db.AddReference(instance, "item", itemId, false);
                _builder.Db.AddReference(item, "instance", instanceId, true);
            }
        }
    }
}