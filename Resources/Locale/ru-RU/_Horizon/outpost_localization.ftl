faction-uncaptured-state = Консоль не захвачена.
faction-capturing-state-by = Консоль захватывается фракцией:
faction-captured-state-by = Консоль захвачена фракцией:
faction-default-string = Консоль не захвачена.
faction-insert-faction-id = Вставьте ID для начала захвата!
faction-id-card-not-belong-to-any-faction = ID недействительно
faction-cant-capture-already-captured-outpost = Фракция уже владеет консолью
faction-start-capture-outpost = Запустить захват консоли
faction-none = Неизвестная фракция
faction-unknown = неизвестная
outpost-first-capture-by =
    Фракция { $factionName } начала захват { $outpostName ->
        [empty] одного из аванпостов
       *[other] аванпоста { $outpostName }
    }
outpost-capture-fall-controlled-by =
    Фракция { $oldFactionName } { $outpostName ->
        [empty] потеряла контроль над одним из аванпостов
       *[other] { $oldFactionName } потеряла контроль над аванпостом { $outpostName }
    }
outpost-capture-by-controlled-by =
    Фракция { $factionName } начала захват { $outpostName ->
        [empty] одного из аванпостов под контролем { $oldFactionName }
       *[other] аванпоста { $outpostName } под контролем { $oldFactionName }
    }
outpost-intercept-by-controlled-by =
    Фракция { $factionName } перехватила захват фракции { $outpostName ->
        [empty] { $oldFactionName } во время захвата одного из аванпостов
       *[other] { $oldFactionName } во время захвата аванпостов { $outpostName }
    }
