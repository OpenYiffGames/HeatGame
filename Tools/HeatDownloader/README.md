[![Release Builder](https://github.com/OpenYiffGames/HeatGame/actions/workflows/release.yml/badge.svg)](https://github.com/OpenYiffGames/HeatGame/actions/workflows/release.yml)

# HeatDownloader

This CLI tool allows you to download the latest build of heat. It is a simple patreon scraper that scans for releases and finds the build in the CDN server

## How to install it

- Clone this repository in any place you like:
- Go to the `HeatDownloader` directory located in `Tools/HeatDownloader`. (Where we are now)
- Have **python 3.10** or newer
- Install the package using `pip install -e .`
- Or use one of the installation scripts: `setup-windows.cmd` [*legacy*] or `setup-windows.ps1` [**recommended**]

### Automatic (git and script) - [_**recommended if you are naive to python**_]
#### Powershell or windows terminal:
```bash
# on powershell or windows terminal type:
git clone https://github.com/OpenYiffGames/HeatGame.git
cd HeatGame/Tools/HeatDownloader
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\setup-windows.ps1
```
#### Legacy CMD
```bash
git clone https://github.com/OpenYiffGames/HeatGame.git
cd HeatGame/Tools/HeatDownloader
setup-windows.cmd
```

### Manual (git and pip)
```bash 
# clone
git clone https://github.com/OpenYiffGames/HeatGame.git
cd HeatGame/tools/HeatDownloader
# install
pip install -e .

# If you don't have pip on your PATH try:
python -m pip install .
# if you are on windows, you probably have the 'windows python launcher' (py.exe)
py -m pip install .
```

## How to use it

After a successful installation you can execute it as a module with `python -m heat_downloader` or, if your environment is set up correctly, simply run `heat-downloader`. (This requires your Python Scripts directory on PATH.)

### Commands:
`-h` - shows help message \
`download` - Downloads the game binaries \
`list` - List the versions

### Examples:

#### Downloading
```bash
# Download last version to directory
heat-downloader download --output my-download-directory/

# Download a specific version
heat-downloader download --output my-download-directory/ --version 0.6.7.2

# Running as a module
python -m heat_downloader --output my-download-directory/
```
#### List
```bash
# show all scraped versions
heat-downloader list

# limit results to last 5
heat-downloader list 5
```

## Demo
https://github.com/user-attachments/assets/0d669474-6797-41ff-bd2b-8259bcb63813

## Prebuilt binaries
If you’re too lazy to install the module, check the [releases section](https://github.com/OpenYiffGames/HeatGame/releases) for a single .exe packed with [PyInstaller](https://github.com/pyinstaller/pyinstaller).  
Keep in mind these binaries are auto-generated. Please open an issue if you have any problems.

- [Latest Version](https://github.com/OpenYiffGames/HeatGame/releases/latest/download/tools.zip)

