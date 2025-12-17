analysis-console-menu-title = Широкополосный анализатор Марк 3
analysis-console-server-list-button = Сервер
analysis-console-extract-button = Извлечь очки
analysis-console-info-no-scanner = Анализатор не подключен! Подключите его с помощью мультитула.
analysis-console-info-no-artifact = Артефакт отсутствует! Поместите его на платформу для просмотра информации об узлах.
analysis-console-info-ready = Системы работают. Готово к сканированию.
analysis-console-no-node = Выберите узел для просмотра
analysis-console-info-id = [font="Monospace" size=11]ID:[/font]
analysis-console-info-id-value = [font="Monospace" size=11][color=yellow]{ $id }[/color][/font]
analysis-console-info-class = [font="Monospace" size=11]Класс:[/font]
analysis-console-info-class-value = [font="Monospace" size=11]{ $class }[/font]
analysis-console-info-locked = [font="Monospace" size=11]Статус:[/font]
analysis-console-info-locked-value =
    { "[" }font="Monospace" size=11][color={ $state ->
        [0] red]Закрыт
        [1] lime]Открыт
       *[2] plum]Активен
    }[/color][/font]
analysis-console-info-durability = [font="Monospace" size=11]Прочность:[/font]
analysis-console-info-durability-value = [font="Monospace" size=11][color={ $color }]{ $current }/{ $max }[/color][/font]
analysis-console-info-effect = [font="Monospace" size=11]Эффект:[/font]
analysis-console-info-effect-value =
    { "[" }font="Monospace" size=11][color=gray]{ $state ->
        [true] { $info }
       *[false] Откройте узлы, чтобы получить информацию
    }[/color][/font]
analysis-console-info-trigger = [font="Monospace" size=11]Триггеры:[/font]
analysis-console-info-triggered-value = [font="Monospace" size=11][color=gray]{ $triggers }[/color][/font]
analysis-console-info-scanner = Сканирование...
analysis-console-info-scanner-paused = Приостановлено.
analysis-console-progress-text =
    { $seconds ->
        [one] T-{ $seconds } секунда
        [few] T-{ $seconds } секунды
       *[other] T-{ $seconds } секунд
    }
analysis-console-extract-value = [font="Monospace" size=11][color=orange]Узел { $id } (+{ $value })[/color][/font]
analysis-console-extract-none = [font="Monospace" size=11][color=orange] Нет разблокированных узлов с оставшимися очками для извлечения [/color][/font]
analysis-console-extract-sum = [font="Monospace" size=11][color=orange]Общие исследования: { $value }[/color][/font]
analyzer-artifact-extract-popup = Энергия мерцает на поверхности артефакта!
