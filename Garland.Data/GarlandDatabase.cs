using Garland.Data.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garland.Data
{
    public class GarlandDatabase
    {
        // NOTE: This section must be updated with every patch!
        public const decimal NextPatch = 6.3m;
        public static Patch[] MajorPatches = new[] {
            new Patch(1m, "구 파판14", "구 파판14"),

            new Patch(2m, "신생 에오르제아", "신생 에오르제아"),
            new Patch(2.1m, "각성한 자들", "신생 에오르제아"),
            new Patch(2.2m, "혼돈의 소용돌이", "신생 에오르제아"),
            new Patch(2.3m, "에오르제아의 수호자", "신생 에오르제아"),
            new Patch(2.4m, "빙결의 환상", "신생 에오르제아"),
            new Patch(2.5m, "희망의 등불", "신생 에오르제아"),

            new Patch(3m, "창천의 이슈가르드", "창천의 이슈가르드"),
            new Patch(3.1m, "빛과 어둠의 경계", "창천의 이슈가르드"),
            new Patch(3.2m, "운명의 톱니바퀴", "창천의 이슈가르드"),
            new Patch(3.3m, "최후의 포효", "창천의 이슈가르드"),
            new Patch(3.4m, "혼을 계승하는 자", "창천의 이슈가르드"),
            new Patch(3.5m, "숙명의 끝", "창천의 이슈가르드"),

            new Patch(4m, "홍련의 해방자", "홍련의 해방자"),
            new Patch(4.1m, "영웅의 귀환", "홍련의 해방자"),
            new Patch(4.2m, "새벽의 빛", "홍련의 해방자"),
            new Patch(4.3m, "월하의 꽃", "홍련의 해방자"),
            new Patch(4.4m, "광란의 전주곡", "홍련의 해방자"),
            new Patch(4.5m, "영웅을 위한 진혼가", "홍련의 해방자"),

            new Patch(5m, "칠흑의 반역자", "칠흑의 반역자"),
            new Patch(5.1m, "하얀 서약, 검은 밀약", "칠흑의 반역자"),
            new Patch(5.2m, "추억의 흉성", "칠흑의 반역자"),
            new Patch(5.3m, "크리스탈의 잔광", "칠흑의 반역자"),
            new Patch(5.4m, "또 하나의 미래", "칠흑의 반역자"),
            new Patch(5.5m, "여명의 사투", "칠흑의 반역자"),

            new Patch(6m, "효월의 종언", "효월의 종언"),
            new Patch(6.1m, "새로운 모험", "효월의 종언"),
            new Patch(6.2m, "금단의 기억", "효월의 종언"),
            new Patch(6.3m, "하늘의 축제, 땅의 전율", "효월의 종언")
        };

        public static int LevelCap = -1; // Filled in from Miscellaneous.
        public static int BlueMageLevelCap = 50;

        public HashSet<int> LocationReferences = new HashSet<int>();
        public Dictionary<object, List<DataReference>> DataReferencesBySource = new Dictionary<object, List<DataReference>>();
        public List<int> EmbeddedPartialItemIds = new List<int>();
        public List<dynamic> EmbeddedIngredientItems = new List<dynamic>();
        public HashSet<int> IgnoredCurrencyItemIds = new HashSet<int>();

        public List<dynamic> Items = new List<dynamic>();
        public List<dynamic> Mobs = new List<dynamic>();
        public List<dynamic> Locations = new List<dynamic>();
        public List<dynamic> Nodes = new List<dynamic>();
        public List<dynamic> NodeBonuses = new List<dynamic>();
        public List<dynamic> Npcs = new List<dynamic>();
        public List<dynamic> Instances = new List<dynamic>();
        public List<dynamic> Quests = new List<dynamic>();
        public List<dynamic> QuestJournalGenres = new List<dynamic>();
        public List<dynamic> FishingSpots = new List<dynamic>();
        public List<dynamic> Leves = new List<dynamic>();
        public List<dynamic> WeatherRates = new List<dynamic>();
        public List<string> Weather = new List<string>();
        public List<dynamic> Achievements = new List<dynamic>();
        public List<dynamic> AchievementCategories = new List<dynamic>();
        public List<dynamic> Fates = new List<dynamic>();
        public List<dynamic> DutyRoulette = new List<dynamic>();
        public List<dynamic> ItemCategories = new List<dynamic>();
        public List<dynamic> ItemSeries = new List<dynamic>();
        public List<dynamic> ItemSpecialBonus = new List<dynamic>();
        public List<dynamic> JobCategories = new List<dynamic>();
        public List<dynamic> Ventures = new List<dynamic>();
        public List<dynamic> Actions = new List<dynamic>();
        public List<dynamic> ActionCategories = new List<dynamic>();
        public List<dynamic> Baits = new List<dynamic>();
        public List<dynamic> Jobs = new List<dynamic>();
        public List<dynamic> Dyes = new List<dynamic>();
        public List<dynamic> Statuses = new List<dynamic>();

        public dynamic MateriaJoinRates;

        public Dictionary<string, JArray> LevelingEquipmentByJob = new Dictionary<string, JArray>();
        public Dictionary<string, JObject> EndGameEquipmentByJob = new Dictionary<string, JObject>();
        public Dictionary<int, int> ExperienceToNextByLevel = new Dictionary<int, int>();

        public Dictionary<object, dynamic> ItemsById = new Dictionary<object, dynamic>();
        public Dictionary<int, dynamic> NpcsById = new Dictionary<int, dynamic>();
        public Dictionary<int, dynamic> LeveRewardsById = new Dictionary<int, dynamic>();
        public Dictionary<int, dynamic> InstancesById = new Dictionary<int, dynamic>();
        public Dictionary<int, dynamic> ActionsById = new Dictionary<int, dynamic>();
        public Dictionary<int, dynamic> FishingSpotsById = new Dictionary<int, dynamic>();
        public Dictionary<int, dynamic> QuestsById = new Dictionary<int, dynamic>();
        public Dictionary<int, dynamic> LocationsById = new Dictionary<int, dynamic>();
        public Dictionary<string, int> LocationIdsByName = new Dictionary<string, int>();
        public Dictionary<string, dynamic> ItemsByName = new Dictionary<string, dynamic>();
        public Dictionary<int, List<dynamic>> ItemsByInstanceId = new Dictionary<int, List<dynamic>>();
        public Dictionary<int, List<dynamic>> ItemsBySeriesId = new Dictionary<int, List<dynamic>>();
        public Dictionary<int, dynamic> NodesById = new Dictionary<int, dynamic>();
        public Dictionary<string, dynamic> SpearfishingNodesByName = new Dictionary<string, dynamic>();
        public Dictionary<int, dynamic> VenturesById = new Dictionary<int, dynamic>();
        public Dictionary<int, dynamic> StatusesById = new Dictionary<int, dynamic>();
        public Dictionary<int, string> WeatherById = new Dictionary<int, string>();
        public Dictionary<string, dynamic> MobsByLodestoneId = new Dictionary<string, dynamic>();

        public Dictionary<SaintCoinach.Xiv.PlaceName, LocationInfo> LocationIndex;

        public static HashSet<string> LocalizedTypes = new HashSet<string>() { "achievement", "action", "fate", "fishing", "instance", "item", "leve", "quest", "npc", "mob", "status" };

        // Views
        public List<dynamic> NodeViews = new List<dynamic>();
        public List<dynamic> Fish = new List<dynamic>();

        #region Singleton
        private GarlandDatabase() { }

        public static GarlandDatabase Instance { get; } = new GarlandDatabase();
        #endregion

        public void AddLocationReference(int id)
        {
            if (id <= 10)
                throw new InvalidOperationException();

            LocationReferences.Add(id);
        }

        public void AddReference(object source, string type, string id, bool isNested)
        {
            if (!DataReferencesBySource.TryGetValue(source, out var list))
                DataReferencesBySource[source] = list = new List<DataReference>();

            AddReference(list, type, id, isNested);
        }

        public void AddReference(object source, string type, int id, bool isNested)
        {
            AddReference(source, type, id.ToString(), isNested);
        }

        public void AddReference(object source, string type, IEnumerable<int> ids, bool isNested)
        {
            if (!DataReferencesBySource.TryGetValue(source, out var list))
                DataReferencesBySource[source] = list = new List<DataReference>();

            foreach (var id in ids)
                AddReference(list, type, id.ToString(), isNested);
        }

        public void AddReference(object source, string type, IEnumerable<string> ids, bool isNested)
        {
            if (!DataReferencesBySource.TryGetValue(source, out var list))
                DataReferencesBySource[source] = list = new List<DataReference>();

            foreach (var id in ids)
                AddReference(list, type, id, isNested);
        }

        void AddReference(List<DataReference> list, string type, string id, bool isNested)
        {
            if (list.Any(dr => dr.Type == type && dr.Id == id))
                return; // Skip dupes.

            list.Add(new DataReference(type, id, isNested));
        }
    }
}
