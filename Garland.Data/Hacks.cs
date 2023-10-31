using Garland.Data.Models;
using Newtonsoft.Json.Linq;
using SaintCoinach.Xiv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garland.Data
{
    public static class Hacks
    {
        public static HashSet<int> ExcludedShops = new HashSet<int>() {
            1769474, // Currency Test
            1769475, // Materia Test
            1769524, // Items in Development
        };

        public static HashSet<int> NoModelCategories = new HashSet<int>()
        {
            33, // Fishing Tackle
            39, // Waist
            62  // Job Soul
        };

        public static string GetShopName(ScriptInstruction si)
        {
            if (si.Label.Contains("FCCSHOP"))
                return "부대 명성을 아이템으로 교환";
            else if (si.Label == "MOBSHOP1")
                return "센추리오 휘장 거래";
            else if (si.Label == "MOBSHOP2")
                return "센추리오 휘장 거래 (상급)";
            else if (si.Label == "SHOP_SPOIL")
                return "전리품 교환";
            else if (si.Label == "SPECIAL_SHOP0" && si.Argument == 1769813)
                return "업적 보상";
            else if (si.Label == "SPECIAL_SHOP1" && si.Argument == 1769845)
                return "업적 보상 2";
            else if (si.Label == "SPECIAL_SHOP2" && si.Argument == 1769846)
                return "업적 보상 3";
            else if (si.Label == "SHOP_0" && si.Argument == 1769842)
                return "쿠로의 상장: 금상 교환";
            else if (si.Label == "SHOP_1" && si.Argument == 1769841)
                return "쿠로의 상장: 은상 교환";
            else if (si.Label == "SHOP_2" && si.Argument == 1769956)
                return "쿠로의 상장: 동상 교환";
            else if (si.Label == "SHOP" && si.Argument == 1769812)
                return "PVP 보상";
            else if (si.Label == "REPLICA_SHOP0" && si.Argument == 262918)
                return "'에우레카 웨폰' 복제(투사)";
            else if (si.Label == "REPLICA_SHOP1" && si.Argument == 262922)
                return "'에우레카 웨폰' 복제(마법사)";
            else if (si.Label == "FREE_SHOP_BATTLE" && si.Argument == 1769898)
                return "전투 업적 보상";
            else if (si.Label == "FREE_SHOP_PVP" && si.Argument == 1769899)
                return "PvP 업적 보상";
            else if (si.Label == "FREE_SHOP_CHARACTER" && si.Argument == 1769900)
                return "캐릭터 업적 보상";
            else if (si.Label == "FREE_SHOP_ITEM" && si.Argument == 1769901)
                return "아이템 업적 보상";
            else if (si.Label == "FREE_SHOP_CRAFT" && si.Argument == 1769902)
                return "제작 업적 보상";
            else if (si.Label == "FREE_SHOP_GATHERING" && si.Argument == 1769903)
                return "채집 업적 보상";
            else if (si.Label == "FREE_SHOP_QUEST" && si.Argument == 1769904)
                return "퀘스트 업적 보상";
            else if (si.Label == "FREE_SHOP_EXPLORATION" && si.Argument == 1769905)
                return "탐험 업적 보상";
            else if (si.Label == "FREE_SHOP_GRANDCOMPANY" && si.Argument == 1769906)
                return "총사령부 업적 보상";

            else if (si.Label == "SPSHOP_HANDLER_ID" && si.Argument == 1770041)
                return "창천거리 진흥권 교환";
            else if (si.Label == "SPSHOP2_HANDLER_ID" && si.Argument == 1770281)
                return "창천거리 진흥권 교환 (장비/가구)";
            else if (si.Label == "SPSHOP3_HANDLER_ID" && si.Argument == 1770301)
                return "창천거리 진흥권 교환 (재료/마테리아/아이템)";
            else if (si.Label == "SPSHOP4_HANDLER_ID" && si.Argument == 1770343)
                return "두 빛깔 보석 교환";
            else
            {
                DatabaseBuilder.PrintLine($"Unknown shop label {si.Label}, arg {si.Argument}.");
                return si.Label;
            }
        }

        public static bool IsItemSkipped(string name, int key)
        {
            switch (key)
            {
                case 17557: // Dated Radz-at-Han Coin
                    return false;

                case 22357: // Wrapped Present (no icon)
                    return true;

                case 0: // Some weird item added with 6.4
                    return true;
            }

            if (name.Length == 0)
                return true;

            if (name.StartsWith("Dated"))
                return true;

            return false;
        }

        public static bool IsNpcSkipped(ENpc sNpc)
        {
            if (sNpc.Resident == null)
                return true;

            if (string.IsNullOrWhiteSpace(sNpc.Resident.Singular))
                return true;

            return false;
        }

        public static void SetManualShops(SaintCoinach.ARealmReversed realm, Dictionary<int, GarlandShop> shopsByKey)
        {
            var sENpcs = realm.GameData.ENpcs;

            // Special Shops
            var syndony = sENpcs[1016289];
            shopsByKey[1769635].ENpcs = new ENpc[] { syndony };

            var eunakotor = new ENpc[] { sENpcs[1017338] };
            shopsByKey[1769675].ENpcs = eunakotor;
            shopsByKey[1769869].Fill("장비 가지고 나오기", eunakotor);

            var disreputablePriest = new ENpc[] { sENpcs[1018655] };
            shopsByKey[1769743].Fill("명예 점수 거래(1)", disreputablePriest);
            shopsByKey[1769744].Fill("명예 점수 거래(2)", disreputablePriest);

            var eurekaGerolt = new ENpc[] { sENpcs[1025047] };
            shopsByKey[1769820].Fill("장비 강화(나이트)", eurekaGerolt);
            shopsByKey[1769821].Fill("장비 강화(전사)", eurekaGerolt);
            shopsByKey[1769822].Fill("장비 강화(암흑기사)", eurekaGerolt);
            shopsByKey[1769823].Fill("장비 강화(용기사)", eurekaGerolt);
            shopsByKey[1769824].Fill("장비 강화(몽크)", eurekaGerolt);
            shopsByKey[1769825].Fill("장비 강화(닌자)", eurekaGerolt);
            shopsByKey[1769826].Fill("장비 강화(사무라이)", eurekaGerolt);
            shopsByKey[1769827].Fill("장비 강화(음유시인)", eurekaGerolt);
            shopsByKey[1769828].Fill("장비 강화(기공사)", eurekaGerolt);
            shopsByKey[1769829].Fill("장비 강화(흑마도사)", eurekaGerolt);
            shopsByKey[1769830].Fill("장비 강화(소환사)", eurekaGerolt);
            shopsByKey[1769831].Fill("장비 강화(적마도사)", eurekaGerolt);
            shopsByKey[1769832].Fill("장비 강화(백마도사)", eurekaGerolt);
            shopsByKey[1769833].Fill("장비 강화(학자)", eurekaGerolt);
            shopsByKey[1769834].Fill("장비 강화(점성술사)", eurekaGerolt);

            var confederateCustodian = new ENpc[] { sENpcs[1025848] };
            shopsByKey[1769871].Fill("아이템 교환", confederateCustodian);
            shopsByKey[1769870].Fill("'천궁 무기' 가지고 나오기", confederateCustodian);

            // Gil Shops
            var domanJunkmonger = new ENpc[] { sENpcs[1025763] };
            shopsByKey[262919].ENpcs = domanJunkmonger;

            // Gemstone Traders
            shopsByKey[1769957].ENpcs = new ENpc[] { sENpcs[1027998] }; // Gramsol, Crystarium
            shopsByKey[1769958].ENpcs = new ENpc[] { sENpcs[1027538] }; // Pedronille, Eulmore
            shopsByKey[1769959].ENpcs = new ENpc[] { sENpcs[1027385] }; // Siulmet, Lakeland
            shopsByKey[1769960].ENpcs = new ENpc[] { sENpcs[1027497] }; // ??, Kholusia
            shopsByKey[1769961].ENpcs = new ENpc[] { sENpcs[1027892] }; // Halden, Amh Araeng
            shopsByKey[1769962].ENpcs = new ENpc[] { sENpcs[1027665] }; // Sul Lad, Il Mheg
            shopsByKey[1769963].ENpcs = new ENpc[] { sENpcs[1027709] }; // Nacille, Rak'tika
            shopsByKey[1769964].ENpcs = new ENpc[] { sENpcs[1027766] }; // ??, Tempest

            // Faux Leaves
            var fauxCommander = new ENpc[] { sENpcs[1033921] };
            shopsByKey[1770282].Fill("경품 교환", fauxCommander);

            // Faire Voucher
            shopsByKey[1770286].Name = "불꽃축제 증서 교환";

            // TODO: Fill the shop name via territory sheet
            // e.g. Bozja are at custom/006/CtsMycExorcismTrade_00679

            // Bozja Shops
            var resistanceSuppliers = new ENpc[] { sENpcs[1034007], // Southern Front Cluster exchange
                sENpcs[1036895] // Zadnor Cluster exchange
            };

            shopsByKey[1770087].Fill("아이템 교환", resistanceSuppliers);
        }

        public static bool IsMainAttribute (string attribute)
        {
            switch (attribute)
            {
                case "힘":
                case "민첩성":
                case "활력":
                case "지능":
                case "정신력":
                case "신앙":
                    return true;
            }

            return false;
        }

        public static void CreateDiademNodes(GarlandDatabase db)
        {
            //dynamic mining = new JObject();
            //mining.id = 10000;
            //mining.type = 0;
            //mining.lvl = 60;
            //mining.name = "Node";
            //mining.zoneid = -2;
            //mining.items = new JArray(CreateNodeItem(12534), CreateNodeItem(12537), CreateNodeItem(12535), CreateNodeItem(13750));
            //db.Nodes.Add(mining);

            //dynamic quarrying = new JObject();
            //quarrying.id = 10001;
            //quarrying.type = 1;
            //quarrying.lvl = 60;
            //quarrying.name = "Node";
            //quarrying.zoneid = -2;
            //quarrying.items = new JArray(CreateNodeItem(13751));
            //db.Nodes.Add(quarrying);

            //dynamic logging = new JObject();
            //logging.id = 10001;
            //logging.type = 2;
            //logging.lvl = 60;
            //logging.name = "Node";
            //logging.zoneid = -2;
            //logging.items = new JArray(CreateNodeItem(12586), CreateNodeItem(12891), CreateNodeItem(12579), CreateNodeItem(13752));
            //db.Nodes.Add(logging);

            //dynamic harvesting = new JObject();
            //harvesting.id = 10002;
            //harvesting.type = 3;
            //harvesting.lvl = 60;
            //harvesting.name = "Node";
            //harvesting.zoneid = -2;
            //harvesting.items = new JArray(CreateNodeItem(12879), CreateNodeItem(12878), CreateNodeItem(13753));
            //db.Nodes.Add(harvesting);
        }

        private static dynamic CreateNodeItem(int itemId)
        {
            dynamic obj = new JObject();
            obj.id = itemId;
            return obj;
        }

        public static void SetInstanceIcon(ContentFinderCondition sContentFinderCondition, dynamic obj)
        {
            if (sContentFinderCondition.Content.Key == 55001)
            {
                // Aquapolis
                obj.fullIcon = 1;
                return;
            }

            if (sContentFinderCondition.Content.Key == 55002)
            {
                // Lost Canals of Uznair
                obj.fullIcon = 2;
                return;
            }

            if (sContentFinderCondition.Content.Key == 55003)
            {
                // Hidden Canals of Uznair
                obj.fullIcon = 3;
                return;
            }

            if (sContentFinderCondition.Content.Key == 55004)
            {
                // The Shifting Altars of Uznair
                obj.fullIcon = 4;
                return;
            }

            if (sContentFinderCondition.Content.Key == 55006)
            {
                // The Dungeons of Lyhe Ghiah
                obj.fullIcon = 5;
                return;
            }

            if (sContentFinderCondition.Content.Key == 55008)
            {
                // The Shifting Oubliettes of Lyhe Ghiah
                obj.fullIcon = 6;
                return;
            }

            if (sContentFinderCondition.Image == null)
            {
                DatabaseBuilder.PrintLine($"Content {sContentFinderCondition.Content.Key} {sContentFinderCondition.Content} has no icon");
                return;
            }

            obj.fullIcon = IconDatabase.EnsureEntry("instance", sContentFinderCondition.Image);
        }

        public static string GetContentTypeNameOverride(ContentType sContentType)
        {
            switch (sContentType.Key)
            {
                case 20: return "초보자의 집";
                case 22: return "기간 한정 던전";
                case 23: return "비공정 탐사";
                case 27: return "가면무투회";
            }

            throw new InvalidOperationException($"Invalid missing ContentType override for {sContentType}.");
        }

        public static string GetCategoryDamageAttribute(SaintCoinach.Xiv.ItemUICategory category)
        {
            // This needs to be maintained when new ClassJobs are added, usually
            // in an expansion.

            switch (category.Key)
            {
                case 1: // Pugilist's Arm
                case 2: // Gladiator's Arm
                case 3: // Marauder's Arm
                case 4: // Archer's Arm
                case 5: // Lancer's Arm
                case 12: // Carpenter's Primary Tool
                case 14: // Blacksmith's Primary Tool
                case 16: // Armorer's Primary Tool
                case 18: // Goldsmith's Primary Tool
                case 20: // Leatherworker's Primary Tool
                case 22: // Weaver's Primary Tool
                case 24: // Alchemist's Primary Tool
                case 26: // Culinarian's Primary Tool
                case 28: // Miner's Primary Tool
                case 30: // Botanist's Primary Tool
                case 32: // Fisher's Primary Tool
                case 84: // Rogue's Arms
                case 87: // Dark Knight's Arm
                case 88: // Machinist's Arm
                case 96: // Samurai's Arm
                case 106: // Gunbreaker's Arm
                case 107: // Dancer's Arm
                case 108: // Reaper's Arm
                    return "물리 기본 성능";

                case 6: // One–handed Thaumaturge's Arm
                case 7: // Two–handed Thaumaturge's Arm
                case 8: // One–handed Conjurer's Arm
                case 9: // Two–handed Conjurer's Arm
                case 10: // Arcanist's Grimoire
                case 89: // Astrologian's Arm
                case 97: // Red Mage's Arm
                case 98: // Scholar's Arm
                case 105: // Blue Mage's Arm
                case 109: // Sage's Arm
                    return "마법 기본 성능";

                default:
                    return null;
            }
        }
    }
}
