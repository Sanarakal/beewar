"""Verify an installed 0.3 kingdom survives an APK update on the isolated test AVD."""
import json
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ADB = Path.home() / 'AppData/Local/Android/Sdk/platform-tools/adb.exe'
SERIAL = 'emulator-5556'
PACKAGE = 'com.plaguegame.prototype'
OUTPUT = ROOT / 'setup/bee-android-test'
OUTPUT.mkdir(exist_ok=True)
checks = []


def adb(*args, binary=False):
    result = subprocess.run([str(ADB), '-s', SERIAL, *args], capture_output=True, check=True, timeout=60)
    return result.stdout if binary else result.stdout.decode('utf-8', errors='replace').strip()


def read_save():
    return json.loads(adb('shell', 'cat', '/sdcard/Android/data/' + PACKAGE + '/files/kingdom-v1.json'))


def check(condition, label):
    if not condition:
        raise AssertionError(label)
    checks.append(label)
    print('PASS ' + label, flush=True)


try:
    check(adb('emu', 'avd', 'name').splitlines()[0] == 'PlagueGamePrototype', 'isolated emulator identity')
    before = read_save()
    check(before['format'] == 1 and before['state']['version'] == 1 and len(before['state']['buildings']) == 6, 'real previous-format save present')
    check(not any(before.get('has' + name, False) for name in ('Upgrade', 'Training', 'March')), 'settled migration fixture without time-dependent queues')
    (OUTPUT / 'upgrade-before.json').write_text(json.dumps(before, indent=2), encoding='utf-8')
    adb('shell', 'am', 'force-stop', PACKAGE)
    installed = adb('install', '-r', str(ROOT / 'UnityProject/Builds/Android/PlagueGame-Kingdom.apk'))
    check('Success' in installed, 'APK replaces previous installation with matching signature')
    adb('shell', 'am', 'start', '-W', '-n', PACKAGE + '/com.unity3d.player.UnityPlayerGameActivity')
    deadline = time.monotonic() + 45
    while time.monotonic() < deadline:
        current = read_save()
        if current['format'] == 2 and current['state']['version'] == 2:
            break
        time.sleep(1)
    else:
        raise AssertionError('Updated app did not migrate its save')
    original = before['state']
    updated = current['state']
    check(updated['buildings'][:6] == original['buildings'] and updated['buildings'][6] == dict(kind=6, level=1), 'all original buildings preserved and academy appended')
    for field in ('tier1', 'tier2', 'heroExperience', 'nextMarchId', 'upgradesFinished', 'trainedTotal', 'monstersDefeated', 'marchesReturned', 'monsterReadyAt', 'report'):
        check(updated[field] == original[field], 'migration preserves ' + field)
    check(updated['researchLevels'] == [0, 0, 0, 0] and not current['hasResearch'], 'new technologies start empty without ghost research')
    capacity = 600 + original['buildings'][3]['level'] * 400
    seconds = min(8 * 3600, updated['lastUtc'] - original['lastUtc'])
    for resource, rate in (('food', original['buildings'][1]['level'] * 1.2), ('wood', original['buildings'][2]['level'] * .9), ('gold', original['buildings'][0]['level'] * .35)):
        expected = min(capacity, original[resource] + seconds * rate)
        check(abs(updated[resource] - expected) < .001, 'migration preserves ' + resource + ' with correct offline production')
    deadline = time.monotonic() + 30
    while time.monotonic() < deadline:
        process_id = adb('shell', 'pidof', PACKAGE).split()[0]
        if 'PLAGUE_KINGDOM_READY' in adb('logcat', '-d', '--pid=' + process_id, '-s', 'Unity'):
            time.sleep(1)
            break
        time.sleep(1)
    else:
        raise AssertionError('Updated game did not finish runtime startup')
    foreground = adb('shell', 'dumpsys', 'activity', 'activities')
    check(any(PACKAGE in line and 'topResumedActivity=' in line for line in foreground.splitlines()), 'updated game in foreground')
    (OUTPUT / 'upgrade-after.json').write_text(json.dumps(current, indent=2), encoding='utf-8')
    (OUTPUT / 'upgrade-settlement.png').write_bytes(adb('exec-out', 'screencap', '-p', binary=True))
    result = dict(status='passed', checks=checks)
except Exception as error:
    result = dict(status='failed', checks=checks, error=str(error))
    raise
finally:
    (OUTPUT / 'upgrade-verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')

print('KINGDOM_UPGRADE_CHECKS_OK ' + str(len(checks)), flush=True)
