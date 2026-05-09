-entity-heater-setting-name =
    { $setting ->
        [off] выкл
        [low] слабо
        [medium] средне
        [high] сильно
       *[other] неизвестно
    }
entity-heater-examined =
    Он установлен на { $setting ->
        [off] [color=gray]{ -entity-heater-setting-name(setting: "off") }[/color]
        [low] [color=yellow]{ -entity-heater-setting-name(setting: "low") }[/color]
        [medium] [color=orange]{ -entity-heater-setting-name(setting: "medium") }[/color]
        [high] [color=red]{ -entity-heater-setting-name(setting: "high") }[/color]
       *[other] [color=purple]{ -entity-heater-setting-name(setting: "other") }[/color]
    }.
entity-heater-switch-setting = Переключено на { -entity-heater-setting-name(setting: $setting) }
entity-heater-switched-setting = Переключено на{ -entity-heater-setting-name(setting: $setting) }.
