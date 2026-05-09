salvage-expedition-window-finish = Завершить экспедицию
salvage-expedition-announcement-early-finish = Экспедиция была окончена. Шаттл покинет планету через { $departTime } секунд.
salvage-expedition-announcement-destruction =
    { $count ->
        [1] Уничтожить { $structure } до окончания экспедиции.
       *[others] Уничтожить { $count } { MAKEPLURAL($structure) } до окончания экспедиции.
    }
salvage-expedition-announcement-elimination =
    { $count ->
        [1] Устранить { $target } до окончания экспедиции.
       *[others] Устранить { $count } { MAKEPLURAL($target) } до окончания экспедиции.
    }
salvage-expedition-announcement-destruction-entity-fallback = строений
salvage-expedition-announcement-elimination-entity-fallback = целей
salvage-expedition-shuttle-not-found = Не обнаружен шаттл.
salvage-expedition-not-everyone-aboard = Не вся команда на шаттле! { CAPITALIZE($target) } всё еще отсутствует!
salvage-expedition-failed = Expedition is failed.
# Salvage mods
salvage-time-mod-standard-time = Нормальная продолжительность
salvage-time-mod-rush = Уменьшенная продолжительность
salvage-weather-mod-heavy-snowfall = Сильный снегопад
salvage-weather-mod-rain = Дождь
salvage-biome-mod-shadow = Теневой лес
salvage-dungeon-mod-cave-factory = Пещерная фабрика
salvage-dungeon-mod-med-sci = Научно-медицинская база
salvage-dungeon-mod-factory-dorms = Заводские общежития
salvage-dungeon-mod-lava-mercenary = База наемников над лавой
salvage-dungeon-mod-virology-lab = Вирусологическая лаборатория
salvage-dungeon-mod-salvage-outpost = Шахтерский форпост
salvage-air-mod-1 = 82 Азот, 21 Кислород
salvage-air-mod-2 = 72 Азот, 21 Кислород, 10 Оксид азота
salvage-air-mod-3 = 72 Азот, 21 Кислород, 10 Водяной пар
salvage-air-mod-4 = 72 Азот, 21 Кислород, 10 Аммиак
salvage-air-mod-5 = 72 Азот, 21 Кислород, 10 Углекислый газ
salvage-air-mod-6 = 79 Азот, 21 Кислород, 5 Фосфор
salvage-air-mod-7 = 57 Азот, 21 Кислород, 15 Аммиак, 5 Фосфор, 5 Оксид азота
salvage-air-mod-8 = 57 Азот, 21 Кислород, 15 Водяной пар, 5 Аммиак, 5 Оксид азота
salvage-air-mod-9 = 57 Азот, 21 Кислород, 15 Углекислый газ, 5 Фосфор, 5 Оксид азота
salvage-air-mod-10 = 82 Углекислый газ, 21 Кислород
salvage-air-mod-11 = 67 Углекислый газ, 31 Кислород, 5 Фосфор
salvage-air-mod-12 = 103 Водяной пар
salvage-air-mod-13 = 103 Аммиак
salvage-air-mod-14 = 103 Оксид азота
salvage-air-mod-15 = 103 Углекислый газ
salvage-air-mod-16 = 34 Углекислый газ, 34 Аммиак, 34 Оксид азота
salvage-air-mod-17 = 34 Водяной пар, 34 Аммиак, 34 Оксид азота
salvage-air-mod-18 = 34 Водяной пар, 34 Оксид азота, 17 Аммиак, 17 Углекислый газ
salvage-air-mod-unknown = Неизвестная атмосфера
salvage-expedition-difficulty-NFModerate = Умеренная
salvage-expedition-difficulty-NFHazardous = Высокая
salvage-expedition-difficulty-NFExtreme = Экстремальная
salvage-expedition-megafauna-remaining =
    { $count ->
        [one] { $count } цель осталась.
       *[other] { $count } оставшиеся цели.
    }
salvage-expedition-type-Destruction = Уничтожение
salvage-expedition-type-Elimination = Устранение
