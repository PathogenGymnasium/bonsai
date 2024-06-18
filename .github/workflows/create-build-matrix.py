#!/usr/bin/env python3
import json
import os

import gha

matrix = [ ]

def add(name: str, runner_os: str, rid: str, configurations: list[str] = ['Debug', 'Release']):
    platform = {
        'name': name,
        'os': runner_os,
        'rid': rid,
    }

    ret = { }
    for configuration in configurations:
        job = {
            'platform': platform.copy(),
            'configuration': configuration,
            'configuration-lower': configuration.lower(),
            'job-title': f"{name} {configuration}",
            'artifacts-suffix': '',
        }
        matrix.append(job)
        ret[configuration] = job
    return ret

windows = add('Windows x64', 'windows-latest', 'win-x64')
linux = add('Linux x64', 'ubuntu-latest', 'linux-x64')

# Create packages and installer from Windows Release x64
windows['Release']['create-packages'] = True
#windows['Release']['create-installer'] = True #TODO: TEMP 🧶🧶🧶🧶🧶🧶🧶🧶🧶

# Build dummy packages to determine which ones changed (not relevant for pull requests since we won't publish)
def add_dummy(name: str, artifacts_suffix: str):
    dummy = add(name, 'ubuntu-latest', 'linux-x64', ['Release'])['Release']
    dummy['skip-tests'] = True
    dummy['create-packages'] = True
    dummy['dummy-build'] = True
    dummy['title'] = name # Don't include configuration in dummy target titles
    dummy['artifacts-suffix'] = artifacts_suffix

if os.getenv('GITHUB_EVENT_NAME') != 'pull_request':
    add_dummy('Previous Dummy', '-dummy-prev')
    add_dummy('Next Dummy', '-dummy-next')

# Output
matrix_json = json.dumps({ "include": matrix }, indent=2)
print(matrix_json)
gha.set_output('matrix', matrix_json)

gha.fail_if_errors()
