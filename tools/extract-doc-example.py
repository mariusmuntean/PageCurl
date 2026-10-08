#!/usr/bin/env python3
"""Extract a complete documented MAUI page without rewriting its source."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


def extract(guide: str) -> tuple[str, str, str]:
    pages = [block for block in re.findall(r'```xml\n(.*?)\n```', guide, re.S)
             if re.match(r'\s*<ContentPage\b', block)]
    if len(pages) != 1:
        raise ValueError('Expected one complete ContentPage XAML example')
    name = ET.fromstring(pages[0]).get('{http://schemas.microsoft.com/winfx/2009/xaml}Class', '')
    if not re.fullmatch(r'[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)+', name):
        raise ValueError('Expected a qualified x:Class')
    namespace, page = name.rsplit('.', 1)
    code = [block for block in re.findall(r'```csharp\n(.*?)\n```', guide, re.S)
            if re.search(r'namespace\s+' + re.escape(namespace) + r'\s*[;{]', block)
            and re.search(r'public\s+partial\s+class\s+' + re.escape(page) + r'\s*:\s*ContentPage\b', block)]
    if len(code) != 1:
        raise ValueError('Expected one matching ContentPage code-behind example')
    return page, pages[0] + '\n', code[0] + '\n'


if __name__ == '__main__':
    try:
        page, xaml, code = extract(Path(sys.argv[1]).read_text())
        output = Path(sys.argv[2])
        output.mkdir(parents=True, exist_ok=True)
        for name, content in ((page + '.xaml', xaml), (page + '.xaml.cs', code)):
            path = output / name
            if not path.exists() or path.read_text() != content:
                path.write_text(content)
    except (ValueError, IndexError, OSError, ET.ParseError) as error:
        sys.exit(f'FAIL documentation example: {error}')
