[![Release Builder](https://github.com/OpenYiffGames/HeatGame/actions/workflows/release.yml/badge.svg)](https://github.com/OpenYiffGames/HeatGame/actions/workflows/release.yml)

# HeatDownloader

This CLI tool allows you to download the latest build of heat. It is a simple patreon scraper that scans for releases and finds the build in the CDN server

## How to install it

- Clone this repository in any place you like:
- Go to the `HeatDownloader` directory located in `Tools/HeatDownloader`. (Where we are now)
- Have **python 3.10** or newer
- Install the package using `pip install .`

```bash 
# clone
git clone https://github.com/OpenYiffGames/HeatGame.git
cd HeatGame/tools/HeatDownloader
# install
pip install .

# If you don't have pip try:
# python -m pip install .
# if you are on windows, your python is problably named py
# py -m pip install .
```

## How to use it

After a succefull installation you can execute it using `python -m` command or if your enviroment is setup correctly you can just execute `heat_downloader` on your terminal.

### Commands:
`-h` - shows help message \
`download` - Downloads the game binaries \
`list` - List the versions

### Examples:

#### Downloding
```bash
# Download to directory
heat_downloader download --output my-download-directory/

# Download version
heat_downloader download --output my-download-directory/ --version 0.6.7.2

# If the previous options doesn't work, your path to the python's script folder is probably not set; so try this
python -m heat_downloader --output my-download-directory/
```
#### List
```bash
heat_downloader list
```

## Prebuilt binaries
If you are too lazy to install the module, you can check the [release session](OpenYiffGames/HeatGame/releases) where we have a single .exe packed with [PyInstaller](https://github.com/pyinstaller/pyinstaller).
Keep in mind that these binaries are auto-generated, please open an issue if you have any problems