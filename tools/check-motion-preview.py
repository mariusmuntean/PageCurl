#!/usr/bin/env python3
"""Reject incomplete recordings before publishing the motion preview."""
import json
from pathlib import Path
import subprocess
import sys


def check_preview(movie: Path) -> None:
    info = json.loads(subprocess.check_output([
        'ffprobe', '-v', 'error', '-show_entries', 'format=duration:stream=codec_type,codec_name',
        '-of', 'json', str(movie)], timeout=30))
    duration = float(info['format']['duration'])
    if not 19.5 <= duration <= 22:
        raise ValueError(f'Incomplete preview ({duration:.2f}s); unlock the Mac and record again')
    video = [stream for stream in info['streams'] if stream['codec_type'] == 'video']
    if len(video) != 1 or video[0]['codec_name'] != 'h264':
        raise ValueError('Expected one H.264 video stream')
    if any(stream['codec_type'] == 'audio' for stream in info['streams']):
        raise ValueError('The preview must be silent')
    # Decode sparse, small frames: duration alone can accept a frozen window.
    frames = subprocess.check_output([
        'ffmpeg', '-v', 'error', '-i', str(movie), '-an',
        '-vf', 'fps=2,scale=64:36:flags=area,format=gray', '-f', 'framemd5', '-'],
        text=True, timeout=30)
    hashes = {line.rsplit(',', 1)[-1].strip() for line in frames.splitlines()
              if line and not line.startswith('#')}
    if len(hashes) < 6:
        raise ValueError('Insufficient motion; unlock the Mac and record again')


if __name__ == '__main__':
    try:
        check_preview(Path(sys.argv[1]))
    except (ValueError, KeyError, IndexError, OSError, subprocess.SubprocessError) as error:
        sys.exit(f'FAIL motion preview: {error}')
    print('PASS motion preview media')
