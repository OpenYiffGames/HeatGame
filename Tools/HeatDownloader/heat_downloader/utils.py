
class bcolors:
    OKBLUE = '\033[94m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'


def print_error(message):
    print(bcolors.FAIL + "[ERROR] " + message + bcolors.ENDC)

def print_success(message):
    print(bcolors.OKGREEN + message + bcolors.ENDC)

def print_info(message):
    print(bcolors.OKBLUE + "[INFO] " + message + bcolors.ENDC)

def print_warning(message):
    print(bcolors.WARNING + "[WARNING] " + message + bcolors.ENDC)