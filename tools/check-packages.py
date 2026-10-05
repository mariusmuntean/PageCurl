"""Verify release package assets and metadata without trusting a successful pack."""
from pathlib import Path
import sys
import xml.etree.ElementTree as ET
import zipfile


def check_package(path, frameworks):
    errors = []
    with zipfile.ZipFile(path) as archive:
        files = set(archive.namelist())
        specs = [name for name in files if name.endswith('.nuspec')]
        if len(specs) != 1:
            return ['Expected one package manifest']
        root = ET.fromstring(archive.read(specs[0]))
        for node in root.iter():
            node.tag = node.tag.split('}')[-1]
        metadata = root.find('metadata')
        package_id = metadata.findtext('id')
        license_node = metadata.find('license')
        if license_node is None or license_node.get('type') != 'expression' or license_node.text != 'MIT':
            errors.append('Missing MIT expression')
        if metadata.findtext('readme') not in files:
            errors.append('Missing packaged readme')
        if not metadata.findtext('description'):
            errors.append('Missing description')
        if metadata.findtext('projectUrl') != 'https://mariusmuntean.github.io/PageCurl-docs/':
            errors.append('Missing documentation URL')
        assets = {name.split('/')[1] for name in files if name.startswith('lib/') and name.endswith('.dll')}
        for expected in frameworks:
            matching = [tfm for tfm in assets if tfm == expected or (expected != 'net10.0' and tfm.startswith(expected))]
            if not matching:
                errors.append(f'Missing framework {expected}')
            for tfm in matching:
                if f'lib/{tfm}/{package_id}.dll' not in files:
                    errors.append(f'Missing assembly for {tfm}')
                if f'lib/{tfm}/{package_id}.xml' not in files:
                    errors.append(f'Missing XML documentation for {tfm}')
                if package_id == 'PageCurl.Maui':
                    group = next((node for node in metadata.findall('dependencies/group')
                                  if node.get('targetFramework') == tfm), None)
                    if group is None or not any(node.get('id') == 'PageCurl.Core' for node in group.findall('dependency')):
                        errors.append(f'Missing Core dependency for {tfm}')
    return errors


if __name__ == '__main__':
    errors = check_package(Path(sys.argv[1]), sys.argv[2:])
    for error in errors:
        print(error, file=sys.stderr)
    if errors:
        sys.exit(1)
    print(f'PASS package {Path(sys.argv[1]).name}: {", ".join(sys.argv[2:])}')
