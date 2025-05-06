@echo off
setlocal enabledelayedexpansion

REM === Verify if pip exists directly ===
set PIP=pip
where /Q %PIP% >nul 2>&1
set pip_exists=%errorlevel%

REM === User confirmation ===
choice /C YN /M "This script will install the package in editable mode and WILL NOT CHECK for existing environments. Do you want to proceed?"
if errorlevel 2 exit /b 1

REM === Find the python executable ===
set "PYTHON="
for /f "delims=" %%i in ('where python py 2^>nul') do (
    if exist "%%i" (
        set "PYTHON=%%i"
        goto :found_python_exe
    )
)
:found_python_exe
if not defined PYTHON (
    call :echo_err Python not found in PATH. Please install Python 3.6 or later.
    exit /b 1
)
echo Found Python executable at: !PYTHON!

REM === Use the pip module as the PIP command ===
if %pip_exists% NEQ 0 (
    set PIP=!PYTHON! -m pip
)

REM === Check if PYTHON points to the windows python launcher and set PYTHON_PATH ===
!PYTHON! -0p >nul 2>&1
if !errorlevel! NEQ 0 (
    echo No python launcher found.
    for %%I in ("!PYTHON!") do (
        set "PYTHON_PATH=%%~dpI"
        if "!PYTHON_PATH:~-1!"=="\" set "PYTHON_PATH=!PYTHON_PATH:~0,-1!"
    )
) else (
    REM === Resolve the actual python executable path (active python) ===
    echo Found Windows Python launcher
    for /f "delims=" %%i in ('py -0p 2^>nul') do (
        for /f "tokens=3*" %%a in ("%%i") do (
            set "PYTHON_EXE=%%a %%b"
            for %%F in ("!PYTHON_EXE!") do (
                set "PYTHON_PATH=%%~dpF"
                if "!PYTHON_PATH:~-1!"=="\" set "PYTHON_PATH=!PYTHON_PATH:~0,-1!"
            )
            echo Python version found: %%a at !PYTHON_PATH!
            !PYTHON! -h | findstr /C:"!PYTHON_PATH!" >nul 2>&1
            if !errorlevel! EQU 0 (
                echo Active python path: !PYTHON_PATH!
                goto :check_python
            ) else (
                set "PYTHON_PATH="
            )
        )
    )
)


:check_python
if not defined PYTHON_PATH (
    call :echo_err Failed to determine Python path.
    exit /b 1
)
!PYTHON! --version | findstr /R "^Python 3\.[0-9]*" >nul 2>&1
if errorlevel 1 (
    call :echo_err "Python version is not 3.x. Please install Python 3.6 or later."
    exit /b 1
)

echo Found python path: !PYTHON_PATH!

REM === Set's the SCRIPTS_PATH and verify if is exists on PATH ===
set "SCRIPTS_PATH=!PYTHON_PATH!\Scripts"
if not exist "!SCRIPTS_PATH!" (
    call :echo_err "Scripts folder not found: !SCRIPTS_PATH!"
    call :echo_warn "Please check if Python is installed correctly."
    choice /C YN /M "Do you want to proceed anyway?"
    if errorlevel 2 exit /b 1
) else (
    REM Check if the scripts folder is in path
    set "PATH_OK="
    for %%i in ("!PATH:;=" "%") do (
        if /I "%%~i"=="!SCRIPTS_PATH!" (
            set "PATH_OK=1"
            goto :install_package
        )
    )
    call :echo_warn "The Scripts folder (!SCRIPTS_PATH!) is not in PATH. This may cause issues with running scripts."
    call :echo_warn "Please add it to your PATH or run the script directly from the Scripts folder."
)

:install_package
REM === Install the package in editable mode ===
%PIP% install -e . --force-reinstall
if %errorlevel% neq 0 (
    call :echo_err "Failed to install the package. Installation failed with error code %errorlevel%."
    exit /b 1
)

REM === Verify package installation via pip ===
%PIP% show heat-downloader >nul 2>&1
if %errorlevel% neq 0 (
    call :echo_err "Package not found in Python environment. Please check if the package is installed correctly."
    exit /b 1
)

REM === Verify if the package scripts were installed ===
for %%i in (heat-downloader.exe heat-downloader-script.py) do (
    set "script_full_path=!PYTHON_PATH!\Scripts\%%i"
    if exist "!script_full_path!" (
        goto :check_installation
    )
)

:check_failed_fallback
call :echo_err "Package not found in Scripts folder: !script_full_path!"
call :echo_err "Please check if the package is installed correctly."
exit /b 1

:check_installation
!script_full_path! --help >nul 2>&1
if %errorlevel% neq 0 (
    call :echo_err "Package is not installed correctly. Please check if environment setup is correct."
    exit /b 1
)

REM === Success ===
echo.
call :echo_ok "Package installed successfully. You can now use the package."
if defined PATH_OK (
    call :echo_ok "Type 'heat-downloader --help' for more information."
) else (
    call :echo_warn "Since your script folder is not in PATH, you need to execute it manually from: '!script_full_path!'"
    call :echo_warn "As a workaround you can execute it as a module: %PYTHON% -m heat_downloader --help"
)
exit /b 0

:echo_ok
echo [32m%~1[0m
goto :eof

:echo_err
echo [31m[ERROR] %~1[0m
goto :eof

:echo_warn
echo [33m[WARNING] %~1[0m
goto :eof
