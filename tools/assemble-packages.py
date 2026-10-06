"""Assemble unsigned PageCurl host packages and symbols into one complete distribution."""
import argparse
import copy
import importlib.util
from pathlib import Path
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile

FRAMEWORKS = ['net10.0', 'net10.0-android', 'net10.0-ios', 'net10.0-maccatalyst', 'net10.0-windows']
PROPERTIES = 'package/services/metadata/core-properties/'

spec = importlib.util.spec_from_file_location('check_packages', Path(__file__).with_name('check-packages.py'))
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)


def canonical(node):
    if node is None:
        return None
    return (node.tag.split('}')[-1], tuple(sorted(node.attrib.items())), (node.text or '').strip(),
            tuple(sorted(canonical(child) for child in node)))


def xml_bytes(root):
    if root.tag.startswith('{'):
        ET.register_namespace('', root.tag.split('}')[0][1:])
    return ET.tostring(root, encoding='utf-8', xml_declaration=True)


class Package:
    def __init__(self, path):
        self.path = path
        self.files = checker.read_package_files(path)
        manifests = [name for name in self.files if name.endswith('.nuspec')]
        if len(manifests) != 1:
            raise ValueError(f'Expected one package manifest in {path.name}')
        self.manifest = manifests[0]
        self.root = ET.fromstring(self.files[self.manifest])
        self.metadata = self.root.find('{*}metadata')
        if self.metadata is None:
            raise ValueError(f'Missing package metadata in {path.name}')
        self.package_id = self.metadata.findtext('{*}id')
        self.version = self.metadata.findtext('{*}version')
        self.dependencies = self.metadata.find('{*}dependencies')
        self.identity = (self.package_id, self.version, canonical(self.metadata.find('{*}repository')))
        self.frameworks = {name.split('/')[1] for name in self.files
                           if name.startswith('lib/') and name.endswith(f'/{self.package_id}.dll')}



def symbol_package(package):
    path = package.path.with_suffix('.snupkg')
    if not path.is_file():
        raise ValueError(f'Missing symbol package for {package.path.name}')
    symbols = Package(path)
    if symbols.identity != package.identity or canonical(symbols.dependencies) != canonical(package.dependencies):
        raise ValueError(f'Symbol identity differs from {package.path.name}')
    types = symbols.metadata.findall('{*}packageTypes/{*}packageType')
    if len(types) != 1 or types[0].get('name') != 'SymbolsPackage':
        raise ValueError(f'Invalid symbol package type in {path.name}')
    for tfm in sorted(package.frameworks):
        asset = f'lib/{tfm}/{package.package_id}.pdb'
        if not symbols.files.get(asset):
            raise ValueError(f'Missing symbol asset {asset}')
    symbol_frameworks = {name.split('/')[1] for name in symbols.files
                         if name.startswith('lib/') and name.endswith('.pdb')}
    if symbol_frameworks != package.frameworks:
        raise ValueError(f'Symbol frameworks differ from {package.path.name}')
    return symbols


def merge_packages(packages):
    """Preserve base OPC relationships; merge payload, dependency groups and content types."""
    base = packages[0]
    root = copy.deepcopy(base.root)
    metadata = root.find('{*}metadata')
    dependencies = metadata.find('{*}dependencies')
    if dependencies is None:
        raise ValueError('Missing MAUI dependency groups')
    for node in list(dependencies):
        dependencies.remove(node)
    common = copy.deepcopy(base.metadata)
    common.remove(common.find('{*}dependencies'))
    files = {}
    file_names = {}
    groups = {}
    content_types = {}
    type_root = ET.fromstring(base.files['[Content_Types].xml'])
    for node in list(type_root):
        type_root.remove(node)
    for package in packages:
        candidate = copy.deepcopy(package.metadata)
        if package.dependencies is None:
            raise ValueError('Missing MAUI dependency groups')
        candidate.remove(candidate.find('{*}dependencies'))
        if canonical(candidate) != canonical(common):
            raise ValueError(f'Package metadata differs in {package.path.name}')
        for group in package.dependencies:
            tfm = group.get('targetFramework')
            if group.tag.split('}')[-1] != 'group' or not tfm:
                raise ValueError('Expected framework-specific dependency groups')
            if tfm in groups and canonical(groups[tfm]) != canonical(group):
                raise ValueError(f'Conflicting dependencies for {tfm}')
            groups[tfm] = group
        for name, data in package.files.items():
            if name in [package.manifest, '[Content_Types].xml', '_rels/.rels'] or name.startswith(PROPERTIES):
                continue
            if name.casefold() in file_names and file_names[name.casefold()] != name:
                raise ValueError(f'Conflicting package entry casing: {name}')
            if name in files and files[name] != data:
                raise ValueError(f'Conflicting package entry {name}')
            files[name] = data
            file_names[name.casefold()] = name
        for node in ET.fromstring(package.files['[Content_Types].xml']):
            kind = node.tag.split('}')[-1]
            key = (kind, node.get('Extension') if kind == 'Default' else node.get('PartName'))
            if kind not in ['Default', 'Override'] or not key[1]:
                raise ValueError('Invalid package content type')
            if key in content_types and canonical(content_types[key]) != canonical(node):
                raise ValueError(f'Conflicting content type {key[1]}')
            content_types[key] = node
    for tfm in sorted(groups):
        dependencies.append(copy.deepcopy(groups[tfm]))
    files[base.manifest] = xml_bytes(root)
    files['_rels/.rels'] = base.files['_rels/.rels']
    files.update({name: data for name, data in base.files.items() if name.startswith(PROPERTIES)})
    for key in sorted(content_types):
        if key[0] == 'Default' or key[1].lstrip('/') in files:
            type_root.append(copy.deepcopy(content_types[key]))
    files['[Content_Types].xml'] = xml_bytes(type_root)
    return files


def write_package(path, files):
    with zipfile.ZipFile(path, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(files.items()):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            archive.writestr(info, data)


def assemble_distribution(directories, output):
    paths = []
    for directory in directories:
        candidates = sorted(directory.glob('PageCurl.*.nupkg'))
        if not candidates:
            raise ValueError(f'No PageCurl packages in {directory}')
        paths.extend(candidates)
    packages = [Package(path) for path in sorted(set(paths))]
    cores = [package for package in packages if package.package_id == 'PageCurl.Core']
    maui = [package for package in packages if package.package_id == 'PageCurl.Maui']
    if len(cores) != 1 or not maui or len(cores) + len(maui) != len(packages):
        raise ValueError('Expected one Core package and host-specific MAUI packages')
    core = cores[0]
    merged = merge_packages(maui)
    if core.identity[1:] != maui[0].identity[1:]:
        raise ValueError('Core release identity differs from MAUI')
    symbols = [symbol_package(package) for package in maui]
    core_symbols = symbol_package(core)
    outputs = {
        core.path.name: core.files,
        core_symbols.path.name: core_symbols.files,
        maui[0].path.name: merged,
        symbols[0].path.name: merge_packages(symbols),
    }
    if output.exists() and (not output.is_dir() or any(output.iterdir())):
        raise ValueError(f'Distribution already exists; use an empty output directory: {output}')
    # Validate on the destination filesystem, then expose the complete directory.
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='.pagecurl-package-', dir=output.parent) as scratch:
        scratch = Path(scratch)
        for name, files in outputs.items():
            write_package(scratch / name, files)
        for package_id, frameworks in [('PageCurl.Core', ['net10.0']), ('PageCurl.Maui', FRAMEWORKS)]:
            path = scratch / f'{package_id}.{core.version}.nupkg'
            errors = checker.check_package(path, frameworks)
            if errors:
                raise ValueError('; '.join(errors))
        if output.exists():
            output.rmdir()
        scratch.rename(output)
    return sorted(outputs)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('package_directories', nargs='+', type=Path)
    args = parser.parse_args()
    try:
        names = assemble_distribution(args.package_directories, args.output)
    except (ValueError, OSError, ET.ParseError, zipfile.BadZipFile, KeyError) as error:
        parser.exit(1, f'{error}\n')
    print(f'PASS complete package distribution: {", ".join(names)}')
