gt.fishing = {
    pluralName: '鱼池',
    type: 'fishing',
    blockTemplate: null,
    index: {},
    version: 2,
    partialIndex: {},
    categories: ['海洋垂钓', '淡水垂钓', '沙海垂钓', '浮岛垂钓', '云海垂钓', '熔岩垂钓', '魔泉垂钓', '盐湖垂钓'],
    browse: [
        { type: 'group', prop: 'region' },
        { type: 'group', prop: 'location' },
        { type: 'sort', func: gt.browse.transformLevelAndName }
    ],

    initialize: function(data) {
        gt.fishing.blockTemplate = doT.template($('#block-fishing-template').text());
    },

    cache: function(data) {
        gt.fishing.index[data.fishing.id] = data.fishing;
    },

    getViewModel: function(spot, data) {
        var view = {
            obj: spot,
            id: spot.id,
            type: 'fishing',
            name: spot.name,
            patch: gt.formatPatch(spot.patch),
            template: gt.fishing.blockTemplate,
            blockClass: 'node',
            icon: 'images/FSH.png',
            settings: 1,

            lvl: spot.lvl,
            zone: spot.zoneid ? gt.location.index[spot.zoneid] : null,
            category: gt.fishing.categories[spot.category],
            browseIcon: 'images/FSH.png'
        };

        gt.localize.extractLocalize(spot, view);

        var zoneName = view.zone ? view.zone.name : "云冠群岛";

        // Location and source
        view.sourceName = gt.util.abbr(zoneName) + ', Lv. ' + view.lvl;
        view.longSourceName = zoneName + ', Lv. ' + view.lvl;
        view.location = zoneName;
        var region = view.zone ? gt.location.index[view.zone.parentId] : null;
        if (region)
            view.region = region.name;
        else
            view.region = "出海垂钓";

        if (data) {
            view.items = gt.model.partialList(gt.item, spot.items, function(v, i) { return { item: v, lvl: i.lvl }; });

            if (view.zone) {
                view.map = gt.map.getViewModel({
                    location: view.zone, coords: [spot.x, spot.y], radius: spot.radius, approx: spot.approx,
                    icon: 'images/FSH.png', iconfilter: 'sepia(100%)'
                });
            }
        }

        view.subheader = '等级 ' + view.lvl + ' ' + view.category;
        view.byline = view.subheader;

        return view;
    },

    getPartialViewModel: function(partial) {
        var name = gt.model.name(partial);
        var zone = partial.z ? gt.location.index[partial.z] : null;
        var zoneName = zone ? zone.name : "云冠群岛";
        var region = zone ? gt.location.index[zone.parentId] : null;

        return {
            id: partial.i,
            type: 'fishing',
            name: name,
            sourceName: gt.util.abbr(zoneName) + ', Lv. ' + partial.l,
            longSourceName: zoneName + ', Lv. ' + partial.l,
            byline: '等级 ' + partial.l + ' ' + gt.fishing.categories[partial.c],
            region: region ? region.name : "出海垂钓",
            location: zoneName,
            icon: 'images/FSH.png',
            lvl: partial.l
        };
    },

    resolveCraftSource: function(step, id) {
        step.sourceType = 'fishing';
        step.sourceView = gt.model.partial(gt.fishing, id || step.item.fishingSpots[0]);
        step.setCategory(['钓鱼', '采集']);
    }
};

