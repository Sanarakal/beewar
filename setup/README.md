# Подготовка рабочего места

Дата: 15 октября 2026 года.

Текущая игровая сборка — **0.1.0**, `UnityProject/Builds/Android/PlagueGame-Clinic.apk`. В ней реализован первый цикл лечебницы с временными моделями. Установка, отображение, запрет горизонтального режима и восстановление сохранения проверены на телефоне; автоматическая проверка кнопок ожидает разрешения управления касаниями на Xiaomi. Отчёт: `clinic-device-test/verification.json`. Ниже сохранены сведения об инструментах и первоначальной технической сборке.

## Установлено и проверено

| Инструмент | Версия | Проверка |
|---|---|---|
| Unity Editor + Android Build Support | 6000.3.26f1 / 6.3 LTS | Проект скомпилирован, Android APK собран, редактор зарегистрирован в Hub |
| Blender LTS | 4.5.14 | Запуск в фоновом режиме, выполнение Python через bpy |
| .NET SDK | 10.0.402 | SDK обнаруживается установленным dotnet.exe |
| Unity Hub | 3.22.3 | Запускается справка командной строки |
| VS Code: Unity | 1.3.2 | Расширение установлено |
| VS Code: C# | 2.160.4 | Расширение установлено |
| VS Code: C# Dev Kit | 3.40.210 | Расширение установлено |
| OpenJDK | 17.0.18+8 | java.exe -version |
| Android NDK | r27c / 27.2.12479018 | Проверены файлы и source.properties |
| Android SDK | API 36, Build Tools 36.0.0 | Существующая установка подтверждена sdkmanager |
| Git и Git LFS | Существующие установки | Создан репозиторий, проверены правила LFS |

Инструменты размещены в `C:\Users\dlepe\AppData\Local\Programs\GameDev`.
Существующий Android SDK: `C:\Users\dlepe\AppData\Local\Android\Sdk`.

Unity Hub извлечён из официального подписанного установочного пакета и работает из пользовательской папки. Созданы ярлыки инструментов в меню «Пуск → PlagueGame», а также ярлык проекта PlagueGame на рабочем столе.

## Unity Editor и Android Build Support

Установлены редактор и Android Build Support версии **6000.3.26f1 / Unity 6.3 LTS**.
Размеры и MD5 сверены с официальным API релизов Unity. Проверена действительная цифровая подпись Unity Technologies у редактора; установочный скрипт дополнительно проверяет подписи обоих пакетов перед запуском.

Место установки: `C:\Users\dlepe\AppData\Local\Programs\GameDev\Unity\6000.3.26f1`.

Установка завершилась успешно: оба установщика вернули код 0, в `unity-install.log` записано `SUCCESS`. Редактор зарегистрирован в Unity Hub. Имеющаяся лицензия успешно прошла проверку при запуске редактора.

После установки восстановлены два пропущенных файла метаданных URP из официального репозитория Unity Technologies. Причина и копии файлов сохранены в `unity-package-repair/README.md`.

## Проект

`C:\Users\dlepe\Documents\PlagueGame\UnityProject`

Подготовлены manifest, настройки VS Code, Git, Git LFS и редакторский скрипт для настройки вертикального Android-проекта и создания проверочной сцены.

Первый импорт и компиляция проекта завершились успешно. Через `Build-Android.ps1` собран проверочный APK: `UnityProject\Builds\Android\PlagueGame-EnvironmentCheck.apk`, **37 845 092 байта**.

Проверки Android SDK подтвердили:

- Подпись APK корректна: apksigner, APK Signature Scheme v2.
- Идентификатор приложения: `com.plaguegame.prototype`, версия `0.0.1`.
- Минимальный Android API 26, целевой API 36, архитектура `arm64-v8a`.
- В журнале Unity: `PLAGUE_SETUP_PROJECT_OK`, `Build Finished, Result: Success.`, `PLAGUE_SETUP_ANDROID_BUILD_OK`. Процесс завершился с кодом 0.

Результаты и SHA256 APK сохранены в `android-build-verification.json`, перечень установленных инструментов — в `installed-tools.json`, журнал сборки — в `android-build.log`.

Команда открытия проекта: `..\Open-Project.cmd`.

При первом интерактивном запуске редактор показывает окно **Unity Editor Software Terms**. Для перехода в интерфейс редактора пользователю нужно ознакомиться с условиями и принять их, если он согласен. Окно оставлено открытым. Пакетная компиляция и сборка APK уже завершились успешно.

APK установлен и проверен на телефоне **Xiaomi 2306EPN60G, Android 16 / API 36, ARM64, Mali-G610 MC6**. Холодный запуск прошёл успешно, техническая сцена отображается в вертикальном режиме, процесс продолжал работать во время проверки. В собранных журналах приложения нет ошибок Unity и записей об аварийном завершении.

Снимок экрана, журналы и результат сохранены в `device-test/`; основной отчёт — `device-test/verification.json`. Драйвер выводит некритичное предупреждение о замене MSAA 2x на 1x. Производительность будущей игры под нагрузкой ещё не проверялась: APK содержит только техническую сцену с камерой, освещением и простыми фигурами.

## Источники

- Unity Editor: официальный API `https://services.api.unity.com/unity/editor/release/v1/releases` и загрузки с `download.unity3d.com`.
- Unity Hub: официальный манифест и пакет с `public-cdn.cloud.unity3d.com`.
- Blender: `download.blender.org`, проверен SHA256 официального архива.
- OpenJDK: архив из метаданных Unity, проверен SHA256.
- Android NDK: официальный архив `dl.google.com/android/repository/android-ndk-r27c-windows.zip`.
- .NET SDK: официальный dotnet-install и пакет Microsoft.
- Расширения VS Code: Microsoft Marketplace.
- 7-Zip: официальный выпуск ip7z/7zip, вспомогательный инструмент распаковки.

Скачанные установочные пакеты и служебные файлы находятся в `C:\Users\dlepe\AppData\Local\PlagueGameSetup`. Они не включены в репозиторий игры.
