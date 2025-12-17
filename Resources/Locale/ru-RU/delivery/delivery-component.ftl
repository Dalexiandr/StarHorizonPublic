delivery-recipient-examine = Это адресовано { $recipient }, { $job }.
delivery-already-opened-examine = Это уже открыто.
delivery-earnings-examine = Доставка этого принесёт станции [color=yellow]{ $spesos }[/color] кредитов.
delivery-recipient-no-name = Безымянный
delivery-recipient-no-job = Неизвестно
delivery-unlocked-self = Вы разблокировали { $delivery } с помощью отпечатка своего отпечатка пальца.
delivery-opened-self = Вы открываете { $delivery }.
delivery-unlocked-others = { CAPITALIZE($recipient) } разблокирует { $delivery } с помощью { POSS-ADJ($possadj) } отпечатка пальца.
delivery-opened-others = { CAPITALIZE($recipient) } открывает { $delivery }.
delivery-unlock-verb = Разблокировать
delivery-open-verb = Открыть
delivery-slice-verb = Вскрыть
delivery-teleporter-amount-examine =
    { $amount ->
        [one] Содержит [color=yellow]{ $amount }[/color] посылку.
        [few] Содержит [color=yellow]{ $amount }[/color] посылки.
       *[other] Содержит [color=yellow]{ $amount }[/color] посылок.
    }
delivery-teleporter-empty = { $entity } пустой.
delivery-teleporter-empty-verb = Взять почту
# modifiers
delivery-priority-examine = Это [color=orange]приоритетная { $type }[/color]. У вас есть [color=orange]{ $time }[/color], чтобы доставить её и получить бонус.
delivery-priority-delivered-examine = Это [color=orange]приоритетная { $type }[/color]. Она была доставлена вовремя.
delivery-priority-expired-examine = Это [color=orange]приоритетная { $type }[/color]. Время на доставку истекло.
delivery-fragile-examine = Это [color=red]хрупкая { $type }[/color]. Доставьте её в целости, чтобы получить бонус.
delivery-fragile-broken-examine = Это [color=red]хрупкая { $type }[/color]. Она выглядит сильно повреждённой.
delivery-bomb-examine = Это [color=purple]бомба { $type }[/color]. Вот это поворот!
delivery-bomb-primed-examine = Это [color=purple]бомба { $type }[/color]. Серьёзно, сейчас не время читать!
