import re

import gha

package_file_name_regex = re.compile(r"^(?P<package_name>.+?)\.(?P<major>0|[1-9]\d*)\.(?P<minor>0|[1-9]\d*)\.(?P<patch>0|[1-9]\d*)(?:-(?P<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?P<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?\.s?nupkg$")
def get_package_name(file_name: str) -> str:
    match = package_file_name_regex.match(file_name)
    if match is None:
        gha.print_warning(f"File name '{file_name}' does not match the expected format for a NuGet package.")
        return file_name
    return match.group('package_name')
