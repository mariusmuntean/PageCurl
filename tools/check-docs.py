"""Check the generated site's local URLs and publication boundaries."""
from html.parser import HTMLParser
from pathlib import Path
import sys
from urllib.parse import unquote, urlsplit


class Page(HTMLParser):
    def __init__(self, text):
        super().__init__()
        self.links = []
        self.ids = set()
        self.feed(text)

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if 'id' in attrs:
            self.ids.add(attrs['id'])
        for key in ('href', 'src'):
            if key in attrs:
                self.links.append(attrs[key])


def check_site(root):
    root = root.resolve()
    errors = []
    pages = {file: Page(file.read_text(encoding='utf-8')) for file in root.rglob('*.html')}
    if root / 'index.html' not in pages:
        errors.append('Missing site index.html')
    for file in root.rglob('*'):
        if file.is_file() and (file.suffix in {'.log', '.cs', '.csproj', '.nupkg'}
                               or 'validation' in file.relative_to(root).parts):
            errors.append(f'Unexpected publication: {file.relative_to(root)}')
        if file.is_file() and file.suffix in {'.html', '.json', '.xml'}:
            text = file.read_text(encoding='utf-8')
            if any(prefix in text for prefix in ('/Users/', '/home/runner/', 'C:\\Users\\')):
                errors.append(f'Local build path: {file.relative_to(root)}')
    for file, page in pages.items():
        for link in page.links:
            url = urlsplit(link)
            if url.netloc == 'github.com' and (url.path == '/mariusmuntean/PageCurl'
                                                 or url.path.startswith('/mariusmuntean/PageCurl/')):
                errors.append(f'{file.name}: Private source link: {link}')
            if url.scheme or url.netloc or not link or link.startswith('javascript:'):
                continue
            path = unquote(url.path)
            target = (root / path.lstrip('/') if path.startswith('/') else file.parent / path).resolve() if path else file
            if not target.is_relative_to(root):
                errors.append(f'{file.name}: outside site: {link}')
                continue
            if target.is_dir():
                target /= 'index.html'
            if not target.is_file():
                errors.append(f'{file.name}: missing target: {link}')
            elif url.fragment and target in pages and unquote(url.fragment) not in pages[target].ids:
                errors.append(f'{file.name}: missing anchor: {link}')
    return errors


if __name__ == '__main__':
    errors = check_site(Path(sys.argv[1]))
    for error in errors:
        print(error, file=sys.stderr)
    if errors:
        sys.exit(1)
    print('PASS documentation links, anchors and publication boundaries')
