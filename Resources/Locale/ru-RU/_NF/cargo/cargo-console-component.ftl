## UI

cargo-console-menu-nf-populate-orders-cargo-order-row-product-name-text = { CAPITALIZE($productName) } (x{ $total }) за { $purchaser }
cargo-console-menu-nf-populate-orders-cargo-order-row-product-quantity-text = { $remaining } осталось
cargo-console-menu-nf-order-capacity = { $count }/{ $capacity }
cargo-console-order-nf-menu-notes-label = Примечания:

## Orders

cargo-console-nf-no-bank-account = No bank account found
cargo-console-nf-paper-print-text =
    { "[" }head=2]Заказ #{ $orderNumber }[/head]
    { "[bold]Предмет:[/bold]" } { $itemName } ({ $orderIndex } из { $orderQuantity })
    { "[bold]Купил:[/bold]" } { $purchaser }
    { "[bold]Примечания:[/bold]" } { $notes }

## Upgrades

cargo-telepad-delay-upgrade = Откат телепортации
