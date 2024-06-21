import re

import gha

package_version_regex = re.compile(r"(?P<major>0|[1-9]\d*)\.(?P<minor>0|[1-9]\d*)\.(?P<patch>0|[1-9]\d*)(?:-(?P<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?P<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?")

package_file_name_regex = re.compile(r"^(?P<package_name>.+?)\." + package_version_regex.pattern + r"\.s?nupkg$")
def get_package_name(file_name: str) -> str:
    match = package_file_name_regex.match(file_name)
    if match is None:
        gha.print_warning(f"File name '{file_name}' does not match the expected format for a NuGet package.")
        return file_name
    return match.group('package_name')

def is_valid_version(version: str, forbid_build_metadata: bool = False) -> bool:
    match = package_version_regex.match(version)
    if match is None:
        return False
    
    if forbid_build_metadata and match.group('buildmetadata') is not None:
        return False
    
    return True

def is_preview_version(version: str) -> bool:
    match = package_version_regex.match(version)
    if match is None:
        gha.print_error(f"Version '{version}' is not a legal semver version string!")
        return True
    
    return match.group('prerelease') is not None
