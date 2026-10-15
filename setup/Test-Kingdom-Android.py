"""Touch and persistence check on the isolated PlagueGamePrototype emulator only."""
import json
import subprocess
import time
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image

ADB = r'C:\Users\dlepe\AppData\Local\Android\Sdk\platform-tools\adb.exe'
SERIAL = 'emulator-5556'
PACKAGE = 'com.plaguegame.prototype'
ACTIVITY = PACKAGE + '/com.unity3d.player.UnityPlayerGameActivity'
OUTPUT = Path(__file__).parent / 'bee-android-test'
OUTPUT.mkdir(exist_ok=True)
checks = []


def adb(*arguments, binary=False):
    result = subprocess.run([ADB, '-s', SERIAL, *arguments], capture_output=True, timeout=30, check=True)
    return result.stdout if binary else result.stdout.decode('utf-8', errors='replace').strip()


def check(condition, label):
    if not condition:
        raise AssertionError(label)
    checks.append(label)
    print('PASS ' + label, flush=True)


def state():
    data = json.loads(adb('shell', 'cat', '/sdcard/Android/data/' + PACKAGE + '/files/kingdom-v1.json'))
    result = data['state']
    for name in ('upgrade', 'training', 'march', 'report'):
        if not data['has' + name[0].upper() + name[1:]]:
            result[name] = None
    return result


def tap(x, y):
    adb('shell', 'input', 'tap', str(x), str(y))
    time.sleep(.8)


def wait_for(predicate, label, timeout=35):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        try:
            current = state()
        except (subprocess.CalledProcessError, json.JSONDecodeError):
            current = None
        if current is not None and predicate(current):
            check(True, label)
            return current
        time.sleep(1.2)
    raise AssertionError('Timeout: ' + label)


def screenshot(name):
    activities = adb('shell', 'dumpsys', 'activity', 'activities')
    foreground = next((line for line in activities.splitlines() if 'topResumedActivity=' in line), '')
    check(PACKAGE in foreground, 'foreground check before ' + name)
    path = OUTPUT / (name + '.png')
    path.write_bytes(adb('exec-out', 'screencap', '-p', binary=True))
    return Image.open(path).size


def restart():
    adb('shell', 'am', 'force-stop', PACKAGE)
    adb('shell', 'am', 'start', '-W', '-n', ACTIVITY)
    wait_ready()


def wait_ready():
    deadline = time.monotonic() + 40
    while time.monotonic() < deadline:
        process_id = adb('shell', 'pidof', PACKAGE).split()[0]
        logs = adb('logcat', '-d', '--pid=' + process_id, '-s', 'Unity')
        if 'PLAGUE_KINGDOM_READY' in logs:
            time.sleep(.8)
            return
        time.sleep(1)
    raise AssertionError('Unity runtime startup timed out')


try:
    check(adb('emu', 'avd', 'name').splitlines()[0] == 'PlagueGamePrototype', 'isolated emulator identity')
    # Only the app data inside this newly created test AVD is reset.
    adb('shell', 'am', 'force-stop', PACKAGE)
    adb('shell', 'pm', 'clear', PACKAGE)
    adb('shell', 'am', 'start', '-W', '-n', ACTIVITY)
    wait_ready()
    initial = wait_for(lambda s: s['buildings'][0]['level'] == 1, 'fresh app initialized')
    check(initial['buildings'][0]['level'] == 1 and initial['trainedTotal'] == 0, 'fresh kingdom state')
    screenshot('prologue')
    tap(540, 1670)
    screenshot('settlement')
    tap(540, 1983)
    wait_for(lambda s: s['upgrade'] is not None, 'touch starts castle upgrade', 8)
    tap(540, 2340)
    tap(540, 2065)
    active = wait_for(lambda s: s['training'] is not None, 'touch starts training queue', 8)
    check(active['training'] is not None and active['training']['count'] == 10, 'touch starts training')
    check(active['upgrade'] is not None, 'construction and training run together')
    screenshot('army-training')
    restart()
    current = wait_for(lambda s: s['buildings'][0]['level'] == 2 and s['tier1'] == 10, 'queues survive force-stop and complete')
    check(current['trainedTotal'] == 10 and current['upgradesFinished'] == 1, 'completion counters credited once')
    tap(900, 2340)
    screenshot('map-ready')
    tap(540, 2057)
    current = wait_for(lambda s: s['march'] is not None, 'touch starts march queue', 8)
    check(current['march'] is not None and current['tier1'] == 0, 'touch dispatches and reserves soldiers')
    march_id = current['march']['id']
    screenshot('march-outbound')
    restart()
    tap(900, 2340)
    current = wait_for(lambda s: s['march'] is None and s['report'] is not None and s['report']['delivered'], 'march and reward survive force-stop')
    check(current['report']['marchId'] == march_id and current['report']['won'], 'first monster defeated')
    check(current['heroExperience'] == 20 and current['monstersDefeated'] == 1 and current['marchesReturned'] == 1, 'single reward and experience grant')
    check(current['tier1'] == current['report']['survivors'] and current['tier1'] < 10, 'battle losses and survivor return')
    tap(920, 290)
    screenshot('discovery')
    tap(540, 1670)
    tap(540, 2229)
    screenshot('battle-report')
    tap(540, 1611)
    restart()
    restored = state()
    check(restored['heroExperience'] == 20 and restored['marchesReturned'] == 1 and restored['tier1'] == current['tier1'], 'completed battle reload does not duplicate reward')
    tap(900, 2340)
    screenshot('map-after-battle')
    tap(180, 2340)
    adb('shell', 'input', 'swipe', '480', '1020', '670', '1110', '350')
    screenshot('settlement-after-pan')
    old_rotation = adb('shell', 'wm', 'user-rotation')
    try:
        adb('shell', 'wm', 'user-rotation', 'lock', '1')
        time.sleep(1.2)
        check(screenshot('portrait-rotation') == (1080, 2400), 'portrait lock under requested landscape rotation')
    finally:
        adb('shell', 'wm', 'user-rotation', *(('free',) if old_rotation == 'free' else ('lock', old_rotation.split()[-1])))
    process_id = adb('shell', 'pidof', PACKAGE).split()[0]
    logs = adb('logcat', '-d', '--pid=' + process_id, '-v', 'brief')
    (OUTPUT / 'latest-logcat.txt').write_text(logs, encoding='utf-8')
    exceptions = [line for line in logs.splitlines() if 'FATAL EXCEPTION' in line or 'E/Unity' in line or 'Exception:' in line]
    check(not exceptions, 'no managed Unity exceptions or Android crashes')
    (OUTPUT / 'last-state.json').write_text(json.dumps(restored, ensure_ascii=False, indent=2), encoding='utf-8')
    result = dict(status='passed', tested_at=datetime.now(timezone.utc).isoformat(), target='Android 16 API 36 emulator',
                  avd='PlagueGamePrototype', serial=SERIAL, size=[1080, 2400], graphics='Host GPU',
                  phone_tested=False, checks=checks,
                  limitations=['Physical phone disconnected during this build; emulator performance does not predict phone performance.'])
except Exception as error:
    result = dict(status='failed', checks=checks, error=str(error))
    raise
finally:
    (OUTPUT / 'verification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')

print('KINGDOM_ANDROID_CHECKS_OK ' + str(len(checks)), flush=True)
