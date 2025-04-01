from .patreon_api import ApiFilter, fetch_patreon_posts, filter_patreon_posts
import re
import requests
import heat_downloader.utils as utils
from time import sleep as time_sleep


HEAT_CAMPAIGN_ID = 4451021
ARTIFACT_SERVER = 'https://anthroheat.net/'
RELEASE_KEYWORDS = [
    'heat', 'milestone', 'test', 'build'
]
ARTIFACT_NAMES = [
    'Anthro Heat {v}', 'Heat {v}', 'Anthro Heat {v} Milestone'
]
ARTIFACT_NAMES_SUFFIXES = [
    '.7z', '.zip', '.rar'
]
VERSION_REGEX = re.compile(r'\d+\.\d+\.\d+\.\d+')

def _get_version_from_title(title: str):
    match = VERSION_REGEX.search(title)
    if match:
        return match.group(0)
    return None

def _filter_release_post_name(name: str):
    tokens = name.lower().split(' ')
    for keyword in RELEASE_KEYWORDS:
        if keyword in tokens:
            return True
        
def try_find_artifact(version: str) -> str|None:
    for suffix in ARTIFACT_NAMES_SUFFIXES:
        for artifact_name in ARTIFACT_NAMES:
            artifact_name = artifact_name.format(v=version)
            utils.print_info(f'probing artifact: {artifact_name}{suffix}')
            file_uri = f'{ARTIFACT_SERVER}{artifact_name}{suffix}'
            file_exits = requests.head(file_uri).status_code == 200
            if not file_exits:
                time_sleep(1) # Avoid rate limiting
                continue
            utils.print_info(f'Artifact found: {file_uri}')
            return file_uri
    return None

from typing import Generator

def find_heat_releases() -> Generator[tuple[str, str], None, None]:
    query_filter = ApiFilter(
        campaign_id=HEAT_CAMPAIGN_ID,
        include=['campaign'],
        fields={
            'post': ['title', 'published_at', 'url', 'is_paid', 'like_count', 'teaser_text'],
            'user': ['full_name']
        },
        sort='-published_at'
    )
    page = fetch_patreon_posts(query_filter)
    while page.meta.pagination.cursors.next is not None:
        posts = filter_patreon_posts(page)
        titles = [post.attributes.title for post in posts]
        titles = filter(_filter_release_post_name, titles)
        for version in iter((_get_version_from_title(title), title) for title in titles):
            if version[0] is None:
                continue
            yield version

        query_filter.page = page.meta.pagination.cursors
        page = fetch_patreon_posts(query_filter)
    return []