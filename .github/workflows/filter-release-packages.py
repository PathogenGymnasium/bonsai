#!/usr/bin/env python3
import os
import sys

from pathlib import Path

import gha
import nuget

if len(sys.argv) != 3:
    gha.print_error('Usage: filter-release-packages.py <release-manifest-path> <packages-path>')
    sys.exit(1)
else:
    release_manifest_path = Path(sys.argv[1])
    packages_path = Path(sys.argv[2])

if not release_manifest_path.exists():
    gha.print_error(f"Release manifest '{release_manifest_path}' doest not exist.")
if not packages_path.exists():
    gha.print_error(f"Packages path '{packages_path}' doest not exist.")
gha.fail_if_errors()

release_packages = set()
with open(release_manifest_path, 'r') as release_manifest:
    for line in release_manifest.readlines():
        release_packages.add(line.strip())

file_names = os.listdir(packages_path)
file_names.sort()
for file_name in file_names:
    extension = Path(file_name).suffix.lower()
    if extension != '.nupkg' and extension != '.snupkg':
        continue

    package_name = nuget.get_package_name(file_name)
    if package_name in release_packages:
        if extension != '.snupkg':
            print(f"✅ '{package_name}'")
        continue
    
    if extension != '.snupkg':
        print(f"🛑 '{package_name}'")
    os.unlink(packages_path / file_name)
