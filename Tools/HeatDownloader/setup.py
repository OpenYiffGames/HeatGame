from setuptools import setup, find_packages
from pathlib import Path

from heat_downloader import __version__

setup(
    name = 'heat-downloader',
    version = __version__,
    description = 'A tool to download the latest Heat releases',
    author = 'Yossi99',
    packages = find_packages(),
    install_requires = Path('requirements.txt').read_text().splitlines(),
    entry_points = {
        'console_scripts': [
            'heat-downloader=heat_downloader.cli:run'
        ]
    }
)