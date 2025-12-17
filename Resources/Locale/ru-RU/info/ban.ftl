# ban
cmd-ban-desc = Банит кого-то
cmd-ban-help = Использование: ban <name или user ID> <причина> [длительность в минутах, укажите 0 или оставьте пустым для блокировки навсегда]
cmd-ban-player = Не удалось найти игрока с таким никнеймом.
cmd-ban-invalid-minutes = ${ minutes } не является допустимым количеством минут!
cmd-ban-invalid-severity = ${ severity } недопустимая тяжесть!
cmd-ban-invalid-arguments = Недопустимое количество аргументов
cmd-ban-hint = <name/user ID>
cmd-ban-hint-reason = <причина>
cmd-ban-hint-severity = [тяжесть]
cmd-ban-hint-duration = [срок]
cmd-ban-hint-duration-1 = Навсегда
cmd-ban-hint-duration-2 = 1 день
cmd-ban-hint-duration-3 = 3 дня
cmd-ban-hint-duration-4 = 1 неделя
cmd-ban-hint-duration-5 = 2 недели
cmd-ban-hint-duration-6 = 1 месяц
# ban panel
cmd-banpanel-desc = Открывает панель банов
cmd-banpanel-help = Использование: banpanel [имя или пользовательский guid]
cmd-banpanel-server = Это не может быть использовано из консоли сервера
cmd-banpanel-player-err = Указанный игрок не найден
# listbans
cmd-banlist-desc = Список активных банов пользователя.
cmd-banlist-help = Использование: banlist <name or user ID>
cmd-banlist-empty = Нет активных банов у пользователя { $user }
cmd-banlist-hint = <name/user ID>
cmd-banlistF-hint = <name/user ID>
cmd-ban_exemption_update-desc = Установить исключение на типы банов игрока.
cmd-ban_exemption_update-help =
    Использование: ban_exemption_update <игрок> <флаг> [<флаг> [...]]
    Укажите несколько флагов, чтобы дать игроку несколько флагов исключения из банов.
    Чтобы удалить все исключения, выполните эту команду, указав в качестве единственного флага "None".
cmd-ban_exemption_update-nargs = Ожидалось хотя бы 2 аргумента
cmd-ban_exemption_update-locate = Не удалось найти игрока '{ $player }'.
cmd-ban_exemption_update-invalid-flag = Недопустимый флаг '{ $flag }'.
cmd-ban_exemption_update-success = Обновлены флаги исключений банов для '{ $player }' ({ $uid }).
cmd-ban_exemption_update-arg-player = <player>
cmd-ban_exemption_update-arg-flag = <flag>
cmd-ban_exemption_get-desc = Показать исключения из банов для определённого игрока.
cmd-ban_exemption_get-help = Использование: ban_exemption_get <игрок>
cmd-ban_exemption_get-nargs = Ожидался ровно 1 аргумент
cmd-ban_exemption_get-none = Пользователь не имеет исключений от банов.
cmd-ban_exemption_get-show = Пользователь исключён из банов со следующими флагами: { $flags }.
# Ban panel
ban-panel-title = Панель бана
ban-panel-player = Игрок
ban-panel-ip = IP
ban-panel-hwid = HWID
ban-panel-reason = Причина
ban-panel-last-conn = Использовать IP и HWID из последнего присоединения?
ban-panel-submit = Забанить
ban-panel-confirm = Вы уверены?
ban-panel-tabs-basic = Информация о бане
ban-panel-tabs-reason = Причина
ban-panel-tabs-players = Список Игроков
ban-panel-tabs-role = Информация блокировки роли
ban-panel-no-data = Для блокировки вы должны указать пользователя, IP или HWID.создал полную блокировку
ban-panel-invalid-ip = IP-адрес не удалось проанализировать. Пожалуйста, попробуйте еще раз.
ban-panel-select = Выберите тип
ban-panel-server = Полная блокировка
ban-panel-role = Блокировка роли
ban-panel-minutes = Минуты
ban-panel-hours = Часы
ban-panel-days = Дни
ban-panel-weeks = Недели
ban-panel-months = Месяца
ban-panel-years = Года
ban-panel-permanent = Перманентно
ban-panel-ip-hwid-tooltip = Оставьте пустым и установите флажок ниже, чтобы использовать данные последнего подключения.
ban-panel-severity = Тяжесть:
# Ban string
server-ban-string = { $admin } выдал бан { $severity } тяжести, который истекает { $expires } для [{ $name }, { $ip }, { $hwid }], по причине: { $reason }
ban-panel-erase = Стереть сообщения в чате и игрока из раунда
server-ban-string-never = никогда
server-ban-string-no-pii = { $admin } выдал бан { $severity } тяжести, который истекает { $expires } для { $name } по причине: { $reason }
cmd-ban_exemption_get-arg-player = <игрок>
# Kick on ban
ban-kick-reason = Вы были забанены
# Horizon Ban webhook
discord-banned-title = Бан
discord-banned =
    { "**" }Админ:** `{ $admin }`
    { "**" }Нарушитель:** `{ $name }`
    
    { "**" }Причина:** ```{ $reason }```
    { "**" }До:** `{ $expires }`
discord-banned-footer =
    Сервер: { $server }
    Раунд: #{ $round }
# Horizon Role Ban webhook
discord-role-banned-title = Блокировка роли
discord-role-banned =
    { "**" }Админ:** `{ $admin }`
    { "**" }Нарушитель:** `{ $name }`
    { "**" }Роль:** `{ $role }`
    
    { "**" }Причина:** ```{ $reason }```
    { "**" }До:** `{ $expires }`
