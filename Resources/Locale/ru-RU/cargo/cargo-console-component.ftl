## UI

cargo-console-menu-title = Консоль заказа грузов
cargo-console-menu-account-name-label = Имя аккаунта:{ " " }
cargo-console-menu-account-name-none-text = Нет
cargo-console-menu-account-name-format = [bold][color={ $color }]{ $name }[/color][/bold] [font="Monospace"]\[{ $code }\][/font]
cargo-console-menu-shuttle-name-label = Название шаттла:{ " " }
cargo-console-menu-shuttle-name-none-text = Нет
cargo-console-menu-points-label = Кредиты:{ " " }
cargo-console-menu-points-amount = ${ $amount }
cargo-console-menu-shuttle-status-label = Статус шаттла:{ " " }
cargo-console-menu-shuttle-status-away-text = Отбыл
cargo-console-menu-order-capacity-label = Объём заказов:{ " " }
cargo-console-menu-call-shuttle-button = Активировать телепад
cargo-console-menu-permissions-button = Доступы
cargo-console-menu-categories-label = Категории:{ " " }
cargo-console-menu-search-bar-placeholder = Поиск
cargo-console-menu-requests-label = Запросы
cargo-console-menu-orders-label = Заказы
cargo-console-menu-order-reason-description = Причина: { $reason }
cargo-console-menu-populate-categories-all-text = Все
cargo-console-menu-populate-orders-cargo-order-row-product-name-text = { $productName } (x{ $orderAmount }) от { $orderRequester }
cargo-console-menu-cargo-order-row-approve-button = Одобрить
cargo-console-menu-cargo-order-row-cancel-button = Отменить
cargo-console-menu-tab-title-orders = Заказы
cargo-console-menu-tab-title-funds = Переводы
cargo-console-menu-account-action-transfer-limit = [bold]Лимит Перевода:[/bold] ${ $limit }
cargo-console-menu-account-action-transfer-limit-unlimited-notifier = [color=gold](Безлимитный)[/color]
cargo-console-menu-account-action-select = [bold]Действие Аккаунта:[/bold]
cargo-console-menu-account-action-amount = [bold]Кол-во:[/bold] $
cargo-console-menu-account-action-button = Перевод
cargo-console-menu-toggle-account-lock-button = Переключить Лимит Перевода
cargo-console-menu-account-action-option-withdraw = Снять Деньги
cargo-console-menu-account-action-option-transfer = Перевести кредиты { $code }
# Orders
cargo-console-order-not-allowed = Доступ запрещён
cargo-console-station-not-found = Нет доступной станции
cargo-console-invalid-product = Неверный ID продукта
cargo-console-too-many = Слишком много одобренных заказов
cargo-console-snip-snip = Заказ урезан до вместимости
cargo-console-insufficient-funds = Недостаточно средств (требуется { $cost })
cargo-console-unfulfilled = Нет места для выполнения заказа
cargo-console-trade-station = Отправить на { $destination }
cargo-console-unlock-approved-order-broadcast = [bold]{ $productName } x{ $orderAmount }[/bold], стоимостью [bold]{ $cost }[/bold], были одобрены [bold]{ $approver }[/bold]
cargo-console-fund-withdraw-broadcast = [bold]{ $name } снял { $amount } кредитов с { $name1 } \[{ $code1 }\]
cargo-console-fund-transfer-broadcast = [bold]{ $name } перевел { $amount } кредитов из { $name1 } \[{ $code1 }\] на { $name2 } \[{ $code2 }\][/bold]
cargo-console-fund-transfer-user-unknown = Неизвестно
cargo-console-paper-reason-default = Нет
cargo-console-paper-approver-default = Сам
cargo-console-paper-print-name = Заказ #{ $orderNumber }
cargo-console-paper-print-text =
    Заказ #{ $orderNumber }
    Товар: { $itemName }
    Количество: { $orderQuantity }
    Запросил: { $requester }
    Причина: { $reason }
    Одобрил: { $approver }
# Cargo shuttle console
cargo-shuttle-console-menu-title = Консоль вызова грузового шаттла
cargo-shuttle-console-station-unknown = Неизвестно
cargo-shuttle-console-shuttle-not-found = Не найден
cargo-no-shuttle = Грузовой шаттл не найден!
cargo-shuttle-console-organics = На шаттле обнаружены органические формы жизни
# Funding allocation console
cargo-funding-alloc-console-menu-title = Консоль распределения средств
cargo-funding-alloc-console-label-account = [bold]Аккаунт[/bold]
cargo-funding-alloc-console-label-code = [bold] Код [/bold]
cargo-funding-alloc-console-label-balance = [bold] Баланс [/bold]
cargo-funding-alloc-console-label-cut = [bold] Revenue Division (%) [/bold]
cargo-funding-alloc-console-label-primary-cut = Доля отдела снабжения от средств из источников, не связанных с спец ящиками (%):
cargo-funding-alloc-console-label-lockbox-cut = Доля отдела снабжения от средств от продаж спец ящиков (%):
cargo-funding-alloc-console-label-help-non-adjustible = Отдел снабжения получает { $percent }% прибыли от продаж, не связанных с спец ящиками. Остальное распределяется следующим образом:
cargo-funding-alloc-console-label-help-adjustible = Оставшиеся средства от источников, не связанных с спец ящиками, распределяются следующим образом:
cargo-funding-alloc-console-button-save = Сохранить
cargo-funding-alloc-console-label-save-fail =
    { "[" }bold]Доли доходов недействительны![/bold] [color=red]({ $pos ->
        [1] +
       *[-1] -
    }{ $val }%)[/color]

# Slip template

cargo-acquisition-slip-body =
    { "[" }head=3]Детали заказа[/head]
    { "[bold]Товар:[/bold]" } { $product }
    { "[bold]Описание:[/bold]" } { $description }
    { "[bold]Цена за единицу:[/bold]" } ${ $unit }
    { "[bold]Количество:[/bold]" } { $amount }
    { "[bold]Стоимость:[/bold]" } ${ $cost }
    
    { "[head=3]Детали заказа[/head]" }
    { "[bold]Заказчик:[/bold]" } { $orderer }
    { "[bold]Причина:[/bold]" } { $reason }
