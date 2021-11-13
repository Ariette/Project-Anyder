gt.mob = {
    pluralName: 'Mob',
    type: 'mob',
    blockTemplate: null,
    index: {},
    partialIndex: {},
    version: 2,
    browse: null,

    initialize: function(data) {
        gt.mob.blockTemplate = doT.template($('#block-mob-template').text());
        gt.mob.browse = [
            { type: 'group', func: function(m) {
                    if (m.lvl == '??')
                        return 'Level ' + m.lvl;
                    else
                        return gt.browse.transformLevelRangeCore(Number(m.lvl.split(' - ')[0]), 5);
                } },
            { type: 'group', prop: 'region' },
            { type: 'header', prop: 'location' },
            { type: 'sort', func: gt.browse.transformLevelAndName }
        ];
    },

    cache: function(data) {
        gt.mob.index[data.mob.id] = data.mob;
    },

    getViewModel: function(mob, data) {
        var view = {
            id: mob.id,
            type: 'mob',
            name: mob.name,
            template: gt.mob.blockTemplate,
            icon: 'images/Mob.png',
            subheader: '等级 ' + mob.lvl + ' Mob',
            settings: 1,
            obj: mob,

            lvl: mob.lvl,
            quest: mob.quest
        };

        gt.localize.extractLocalize(mob, view);

        if (mob.instance) {
            var instance = gt.model.partial(gt.instance, mob.instance);
            view.location = instance.name;
            view.sourceName = view.name + ', ' + gt.util.abbr(instance.name);
            view.longSourceName = view.name + ', ' + instance.name;
            view.location_type = 'instance';
            view.location_id = instance.id;
            view.byline = view.location;
            view.region = '副本';
        } else {
            var location = gt.location.index[mob.zoneid];
            view.sourceName = view.name;
            view.longSourceName = view.name;
            view.byline = 'Lv. ' + view.lvl;
            if (location) {
                view.location = location.name;
                view.sourceName += ', ' + gt.util.abbr(location.name);
                view.longSourceName += ', ' + location.name;
                view.location_type = 'location';
                view.location_id = mob.zoneid;
                view.byline += ', ' + view.location;

                if (location.parentId)
                    view.region = gt.location.index[location.parentId].name;
                else
                    view.region = '副本';

                if (mob.coords)
                    view.map = gt.map.getViewModel({ location: location, coords: mob.coords, icon: view.icon });
            }
        }

        if (data) {
            if (mob.drops && mob.drops.length) {
                view.drops = gt.model.partialList(gt.item, mob.drops);
                view.drops = _.sortBy(view.drops, function(i) { return i.name; });
                view.item_text = '掉落 ' + view.drops.length + ' 件物品';
            }

            if (mob.currency)
                view.currency = _.map(mob.currency, function(c) { return { item: gt.model.partial(gt.item, c.id), amount: c.amount }; });

            view.hasInfo = view.drops || view.currency || view.location_type == 'instance';
        }

        return view;
    },

    getPartialViewModel: function(partial) {
        var view = {
            id: partial.i,
            icon: 'images/Mob.png',
            name: gt.model.name(partial),
            type: 'mob',
            lvl: partial.l
        };

        if (partial.t) {
            view.byline = partial.t;
            view.location = partial.t;
            view.region = '副本';
            view.sourceName = view.name + ', ' + gt.util.abbr(partial.t);
            view.longSourceName = view.name + ', ' + partial.t;
        } else {
            var location = gt.location.index[partial.z];
            view.byline = 'Lv. ' + partial.l;
            view.sourceName = view.name;
            view.longSourceName = view.name;
            if (location) {
                view.byline += ', ' + location.name;
                view.location = location.name;
                view.region = location.parentId ? gt.location.index[location.parentId].name : '副本';
                view.sourceName += ', ' + gt.util.abbr(location.name);
                view.longSourceName += ', ' + location.name;
            }
        }

        return view;
    },

    resolveCraftSource: function(step, id) {
        var mob = id ? gt.model.partial(gt.mob, id) : null;
        if (!mob) {
            var list = gt.model.partialList(gt.mob, step.item.drops);
            if (!list)
                return;

            mob = _.sortBy(list, function(m) { return (m.quest ? 'zzz' : '') + m.name; })[0];
        }

        step.sourceType = 'mob';
        step.sourceView = mob;
        step.setCategory(['怪物', '其他']);
    }
};
