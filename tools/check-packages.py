"""Verify unsigned release package assets and metadata before distribution signing."""
import argparse
from collections import Counter
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

PROPERTIES = 'package/services/metadata/core-properties/'


def read_package_files(path):
    """Check ZIP and OPC structure before inspecting or merging package payloads."""
    with zipfile.ZipFile(path) as archive:
        names = archive.namelist()
        if any(count > 1 for count in Counter(name.casefold() for name in names).values()):
            raise ValueError(f'Duplicate package entry in {Path(path).name}')
        for name in names:
            parts = name.rstrip('/').split('/')
            if '\\' in name or name.startswith('/') or any(part in ('', '.', '..') or ':' in part for part in parts):
                raise ValueError(f'Invalid package entry {name}')
        if any(name.casefold() == '.signature.p7s' for name in names):
            raise ValueError('Signed packages cannot be assembled or checked here; sign only the final distribution')
        files = {name: archive.read(name) for name in names if not name.endswith('/')}
    manifests = [name for name in files if name.endswith('.nuspec')]
    if len(manifests) != 1:
        raise ValueError('Expected one package manifest')
    for required in ['_rels/.rels', '[Content_Types].xml']:
        if required not in files:
            raise ValueError(f'Missing {required} in {Path(path).name}')
    relationships = ET.fromstring(files['_rels/.rels'])
    manifest_type = 'http://schemas.microsoft.com/packaging/2010/07/manifest'
    properties_type = 'http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties'
    if len(relationships) != 2 or {node.get('Type') for node in relationships} != {manifest_type, properties_type}:
        raise ValueError(f'Unsupported package relationship in {Path(path).name}')
    for node in relationships:
        target = (node.get('Target') or '').lstrip('/')
        valid = target == manifests[0] if node.get('Type') == manifest_type else target.startswith(PROPERTIES)
        if node.get('TargetMode') is not None or not valid or target not in files:
            raise ValueError(f'Invalid package relationship in {Path(path).name}')
    ET.fromstring(files['[Content_Types].xml'])
    return files


def matches_framework(actual, expected):
    """Accept an optional numeric platform version, never an arbitrary prefix."""
    if actual == expected:
        return True
    return expected != 'net10.0' and re.fullmatch(re.escape(expected) + r'\d+(?:\.\d+)*', actual) is not None


def check_package(path, frameworks):
    try:
        files = read_package_files(path)
        specs = [name for name in files if name.endswith('.nuspec')]
        root = ET.fromstring(files[specs[0]])
        for node in root.iter():
            node.tag = node.tag.split('}')[-1]
        metadata = root.find('metadata')
        if metadata is None:
            return ['Missing package metadata']
        package_id = metadata.findtext('id')
        version = metadata.findtext('version')
        errors = []
        if package_id not in ('PageCurl.Core', 'PageCurl.Maui'):
            errors.append('Unexpected package identity')
        if not version or not version.strip():
            errors.append('Missing package version')
        license_node = metadata.find('license')
        if license_node is None or license_node.get('type') != 'expression' or license_node.text != 'MIT':
            errors.append('Missing MIT expression')
        readme = metadata.findtext('readme')
        if readme not in files or not files[readme].strip():
            errors.append('Missing packaged readme')
        if not (metadata.findtext('description') or '').strip():
            errors.append('Missing description')
        if metadata.findtext('projectUrl') != 'https://mariusmuntean.github.io/PageCurl-docs/':
            errors.append('Missing documentation URL')
        repository = metadata.find('repository')
        if repository is None or repository.get('type') != 'git' or repository.get('url') != 'https://github.com/mariusmuntean/PageCurl':
            errors.append('Missing repository URL')
        if repository is None or not re.fullmatch(r'[0-9a-fA-F]{40}', repository.get('commit', '')):
            errors.append('Missing repository commit')
        assets = {name.split('/')[1] for name in files if name.startswith('lib/') and name.endswith('.dll')}
        for expected in frameworks:
            matching = [tfm for tfm in assets if matches_framework(tfm, expected)]
            if not matching:
                errors.append(f'Missing framework {expected}')
            for tfm in sorted(matching):
                for extension, label in [('dll', 'assembly'), ('xml', 'XML documentation')]:
                    asset = f'lib/{tfm}/{package_id}.{extension}'
                    if asset not in files or not files[asset]:
                        errors.append(f'Missing {label} for {tfm}')
                if package_id == 'PageCurl.Maui':
                    groups = [node for node in metadata.findall('dependencies/group') if node.get('targetFramework') == tfm]
                    cores = [node for group in groups for node in group.findall('dependency') if node.get('id') == 'PageCurl.Core']
                    if len(groups) != 1 or len(cores) != 1:
                        errors.append(f'Missing or duplicate Core dependency for {tfm}')
                    elif cores[0].get('version') not in (version, f'[{version}]'):
                        errors.append(f'Core dependency version differs from package for {tfm}')
        return errors
    except (OSError, zipfile.BadZipFile, ET.ParseError, KeyError, ValueError) as error:
        return [f'Invalid package {Path(path).name}: {error}']


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('package', type=Path)
    parser.add_argument('frameworks', nargs='+')
    args = parser.parse_args()
    errors = check_package(args.package, args.frameworks)
    for error in errors:
        print(error, file=sys.stderr)
    if errors:
        sys.exit(1)
    print(f'PASS package {args.package.name}: {", ".join(args.frameworks)}')
