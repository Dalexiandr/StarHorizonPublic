defusable-examine-defused = { CAPITALIZE($name) } [color=lime]обезврежена[/color].
defusable-examine-live =
    { CAPITALIZE($name) } [color=red]взведена[/color] и таймер показывает [color=red]{ $time } { $time ->
        [one] секунда
        [few] секунды
       *[other] секунд
    }.
defusable-examine-live-display-off = { CAPITALIZE($name) } [color=red]взведена[/color], и таймер похоже, выключен.
defusable-examine-inactive = { CAPITALIZE($name) } [color=lime]неактивна[/color], но все еще может быть взведена.
defusable-examine-bolts =
    Крепления { $down ->
        [true] [color=red]активны[/color]
       *[false] [color=green]отключены[/color]
    }.
