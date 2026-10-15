import json
import sys
from pathlib import Path

sys.path.insert(0, r'C:\Users\dlepe\AppData\Local\PlagueGameSetup\video-tools')
import av
from PIL import Image, ImageDraw

source = Path(r'C:\Users\dlepe\Downloads\20012128143931.mp4')
output = Path(__file__).parent / 'reference-video'
output.mkdir(exist_ok=True)
container = av.open(str(source))
stream = container.streams.video[0]
duration = float(stream.duration * stream.time_base) if stream.duration else container.duration / av.time_base
metadata = dict(source=str(source), duration_seconds=duration, width=stream.width, height=stream.height,
                fps=float(stream.average_rate), audio_streams=len(container.streams.audio), frames=[])
sheet = Image.new('RGB', (1080, 4 * 630), '#24353a')
draw = ImageDraw.Draw(sheet)
for index in range(16):
    timestamp = min(duration - .15, duration * index / 16)
    container.seek(max(0, int(timestamp / stream.time_base)), stream=stream, backward=True)
    selected = None
    for frame in container.decode(stream):
        selected = frame
        if frame.time is None or frame.time >= timestamp:
            break
    image = selected.to_image()
    filename = f'frame-{index:02d}-{timestamp:06.2f}s.jpg'
    image.save(output / filename, quality=90)
    image.thumbnail((260, 593))
    x, y = index % 4 * 270, index // 4 * 630
    sheet.paste(image, (x + (270 - image.width) // 2, y + 30))
    draw.text((x + 8, y + 8), f'{index:02d} / {timestamp:.1f} sec', fill='white')
    metadata['frames'].append(dict(index=index, seconds=timestamp, file=filename))
sheet.save(output / 'contact-sheet.jpg', quality=92)
(output / 'metadata.json').write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(metadata, ensure_ascii=False))
