# PlagueGame — рабочий Android-проект

Техническое имя проекта. Текущий мир — пчелиные королевства «Янтарный престол»; художественные модели представляют первый проход.

- Редактор: Unity 6.3 LTS, 6000.3.26f1.
- Графика: Universal Render Pipeline.
- Платформа: Android, вертикальный экран.
- Начальная конфигурация сборки: ARM64, IL2CPP, Android API 26–36, OpenGL ES 3.
- Модели и анимация: Blender 4.5 LTS.

Сцена `Assets/Scenes/EnvironmentCheck.unity` предназначена для проверки редактора и Android-сборки. Её простые фигуры не определяют будущий стиль игры.

Основная игровая сцена: `Assets/Scenes/KingdomPrototype.unity`, версия 0.4.0. Семь построек, производство, улучшения, обучение, Рута, походы и монстры. Академия Цветения добавляет четыре технологии с отдельной очередью, офлайн-завершением и переносом сохранений 0.3. Подробности — в `../academy-prototype-notes.md`.

Вертикальная ориентация закреплена в настройках Android и компоненте PortraitLock; горизонтальные положения отключены. UI учитывает безопасную область экрана.

Для открытия основной сцены используйте `PlagueGame → Prototype → Open kingdom`, для сборки — `Build Android kingdom` или `../setup/Build-Android.ps1`. APK: `Builds/Android/PlagueGame-Kingdom.apk`. Готовые обновления доступны в [GitHub Releases](https://github.com/Sanarakal/beewar/releases).

Предыдущая сцена лечебницы сохранена в `Assets/Scenes/ClinicPrototype.unity`. Для неё доступны `Open clinic` и `Build Android clinic`.

Меню редактора: `PlagueGame → Setup → Configure project` и `Build Android environment check`.

Готовая проверочная APK сохраняется в `Builds/Android`. Сборки, локальные кэши и ключи подписи исключены из Git. Git LFS настроен для моделей, изображений и звука.

15 октября 2026 года: проект скомпилирован и APK успешно собран (37 845 092 байта). Подпись v2 и параметры Android проверены инструментами SDK. Установка, запуск и отображение сцены проверены на Xiaomi 2306EPN60G с Android 16; вылетов и ошибок Unity в собранных журналах нет. Подробности: `../setup/android-build-verification.json` и `../setup/device-test/verification.json`.

Разбор референса расположен рядом с проектом, в `../reference-analysis.md`.
