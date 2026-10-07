"""Small NuGet-format archives shared by package validation tests."""
import zipfile

COMMIT = '0123456789012345678901234567890123456789'


def package(path, frameworks, package_id='PageCurl.Maui', commit=COMMIT, version='0.1.0',
            readme='# PageCurl', missing_xml=False, missing_core=False, core_version=None, namespaced=True):
    symbols = path.suffix == '.snupkg'
    dependency = '' if missing_core else f'<dependency id="PageCurl.Core" version="{core_version or version}" />'
    groups = ''.join(f'<group targetFramework="{tfm}">{dependency}</group>' for tfm in frameworks) if package_id == 'PageCurl.Maui' else ''
    common = f'<id>{package_id}</id><version>{version}</version><description>Page turns</description><projectUrl>https://mariusmuntean.github.io/PageCurl-docs/</projectUrl><repository type="git" url="https://github.com/mariusmuntean/PageCurl" commit="{commit}" />'
    metadata = common + ('<packageTypes><packageType name="SymbolsPackage" /></packageTypes>' if symbols else '<authors>Marius Muntean</authors><license type="expression">MIT</license><readme>README.md</readme>')
    namespace = ' xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"' if namespaced else ''
    with zipfile.ZipFile(path, 'w') as archive:
        archive.writestr(f'{package_id}.nuspec', f'<package{namespace}><metadata>{metadata}<dependencies>{groups}</dependencies></metadata></package>')
        archive.writestr('_rels/.rels', f'<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Type="http://schemas.microsoft.com/packaging/2010/07/manifest" Target="/{package_id}.nuspec" Id="manifest" /><Relationship Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="/package/services/metadata/core-properties/nuget.psmdcp" Id="properties" /></Relationships>')
        archive.writestr('package/services/metadata/core-properties/nuget.psmdcp', '<properties/>')
        archive.writestr('[Content_Types].xml', '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="nuspec" ContentType="application/octet" /><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" /><Default Extension="psmdcp" ContentType="application/vnd.openxmlformats-package.core-properties+xml" /><Default Extension="dll" ContentType="application/octet" /><Default Extension="xml" ContentType="application/octet" /><Default Extension="md" ContentType="application/octet" /><Default Extension="pdb" ContentType="application/octet" /></Types>')
        if not symbols:
            archive.writestr('README.md', readme)
        for tfm in frameworks:
            for extension in ['pdb'] if symbols else ['dll', 'xml']:
                if extension != 'xml' or not missing_xml:
                    archive.writestr(f'lib/{tfm}/{package_id}.{extension}', f'{tfm} {extension}')


def package_pair(folder, package_id, frameworks, **kwargs):
    folder.mkdir(parents=True, exist_ok=True)
    for suffix in ['nupkg', 'snupkg']:
        package(folder / f'{package_id}.{kwargs.get("version", "0.1.0")}.{suffix}',
                frameworks, package_id=package_id, **kwargs)


def rewrite(path, changes):
    with zipfile.ZipFile(path) as archive:
        entries = {name: archive.read(name) for name in archive.namelist()}
    entries.update(changes)
    with zipfile.ZipFile(path, 'w') as archive:
        for name, data in entries.items():
            if data is not None:
                archive.writestr(name, data)
