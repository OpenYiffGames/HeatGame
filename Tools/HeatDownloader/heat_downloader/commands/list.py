from heat_downloader import find_heat_releases
from itertools import islice

def list_heat_releases(n: int):
    all_versions = find_heat_releases()
    for version, title in islice(all_versions, n):
        print(f'Version: {version}\n\tTitle: {title}')