from tqdm import tqdm
import requests
import os
from time import sleep as time_sleep

from heat_downloader import find_heat_releases, try_find_artifact
import heat_downloader.utils as utils

def _download_artifact(file_uri: str, output_file: str):
    file_info = requests.head(file_uri)
    file_size = int(file_info.headers.get('Content-Length', 0))
   
    with requests.get(file_uri, stream=True) as response:
        with open(output_file, 'wb') as file:
            with tqdm(total=file_size, unit='B', unit_scale=True, desc=output_file) as progress_bar:
                for chunk in response.iter_content(chunk_size=8192):
                    if chunk:
                        file.write(chunk)
                        progress_bar.update(len(chunk))


def download_latest_heat_release(output_dir: str):
    if not os.path.exists(output_dir):
        raise FileNotFoundError(f'Output directory not found: {output_dir}')
  
    all_versions = find_heat_releases()
    for version, title in all_versions:
        if version:
            utils.print_info(f'Found release version: {version}\n\tTitle: {title}')
            file_uri = try_find_artifact(version)
            if file_uri:
                file_name = os.path.basename(file_uri)
                output_dir = os.path.join(output_dir, file_name)
                _download_artifact(file_uri, output_dir)
                utils.print_success(f'Artifact downloaded: {output_dir}')
                return
            else:
                time_sleep(1) # Avoid rate limiting
                utils.print_warning(f'Artifact for version: {version} not found!')
    utils.print_error('No artifact found for any release version')

def download_heat_release_version(output_dir: str, version: str):
    if not os.path.exists(output_dir):
        raise FileNotFoundError(f'Output directory not found: {output_dir}')
    
    file_uri = try_find_artifact(version)
    if not file_uri:
        utils.print_error(f'Artifact for version: {version} not found!')
        return
    file_name = os.path.basename(file_uri)
    output_dir = os.path.join(output_dir, file_name)
    if file_uri:
        _download_artifact(file_uri, output_dir)
    else:
        utils.print_error(f'Artifact for version: {version} not found!')