import argparse
import heat_downloader.commands as commands
from typing import Callable, Any


def download_heat(output: str, version: str = None):
    if version is None:
        commands.download_latest_heat_release(output)
    else:
        commands.download_heat_release_version(output, version)

def list_heat_releases(n: int = 10):
    commands.list_heat_releases(n)

DOWNLOAD_COMMAND = (download_heat, lambda args: [args.output, args.version])
LIST_COMMAND = (list_heat_releases, lambda args: [args.n])
COMMAND_MAP: dict[str, (Callable, Callable[[Any], list])]  = {
    "download": DOWNLOAD_COMMAND,
    "d": DOWNLOAD_COMMAND,
    "list": LIST_COMMAND,
}

class DownloaderCLI:
    def __init__(self):
        self.args = self.build_parser()
   
    def build_parser(self):
        parser = argparse.ArgumentParser(description='Heat Downloader CLI')

        subparsers = parser.add_subparsers(dest="command", help="Commands")

        download_parser = subparsers.add_parser("download", aliases=['d'], help="Download latest heat release")
        download_parser.add_argument("-o", "--output", help="Output directory", required=True)
        download_parser.add_argument("-v", "--version", help="Heat version", required=False)

        list_parser = subparsers.add_parser("list", help="List all available heat releases")
        list_parser.add_argument("n", nargs="?", type=int, default=None, help="Number of releases to list")

        return parser

    def parse_args(self):
        parser = self.build_parser()
        try:
            args = parser.parse_args()
            if (args.command == None):
                parser.print_help()
                return None
            return args
        except Exception:
            parser.print_help()
            return None
        
    def run(self):
        args = self.parse_args()
        if args is None:
            return 
        command, expand_args = COMMAND_MAP[args.command]
        command(*expand_args(args))

def run():
    DownloaderCLI().run()