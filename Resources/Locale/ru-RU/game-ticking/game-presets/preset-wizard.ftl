## Survivor

roles-antag-survivor-name = Выживший
# It's a Halo reference
roles-antag-survivor-objective = Текущая цель: Выжить
survivor-role-greeting =
    Вы выживший.
    Вам нужно вернуться на ЦентКомм живым.
    Соберите столько оружия, сколько нужно, чтобы гарантировать свое выживание.
    Не доверяйте никому.
survivor-round-end-dead-count =
    { $deadCount ->
        [one] [color=red]{ $deadCount }[/color] выживший умер.
       *[other] [color=red]{ $deadCount }[/color] выживших умерло.
    }
survivor-round-end-alive-count =
    { $aliveCount ->
        [one] [color=yellow]{ $aliveCount }[/color] выживший был оставлен на станции.
       *[other] [color=yellow]{ $aliveCount }[/color] выживших было оставлено на станции.
    }
survivor-round-end-alive-on-shuttle-count =
    { $aliveCount ->
        [one] [color=green]{ $aliveCount }[/color] выживший выбрался живым.
       *[other] [color=green]{ $aliveCount }[/color] выживших выбралось живыми.
    }

## Wizard

objective-issuer-swf = [color=turquoise]Федерация Космических Волшебников[/color]
wizard-title = Волшебник
wizard-description = На станции есть Волшебник! Никогда не знаешь, что он может сделать.
roles-antag-wizard-name = Волшебник
roles-antag-wizard-objective = Преподайте им урок, который они никогда не забудут.
wizard-role-greeting =
    ТЫ ВОЛШЕБНИК!
    Между Федерацией Космических Волшебников и NanoTrasen возникли напряженности.
    Так что вас выбрали Федерацией Космических Волшебников для посещения станции.
    Дайте им хорошую демонстрацию своих сил.
    Что вы будете делать, зависит только от вас, просто помните, что Федерация Космических Волшебников хочет, чтобы вы выбрались живым.
wizard-round-end-name = волшебник

## TODO: Wizard Apprentice (Coming sometime post-wizard release)

