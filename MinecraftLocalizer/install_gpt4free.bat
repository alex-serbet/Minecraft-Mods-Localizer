@echo off
setlocal

:: Install Python (if not installed, this step can be skipped if already available)
where python >nul 2>nul
if %errorlevel% neq 0 (
    echo Python not found. Please install Python and add it to PATH.
    exit /b 1
)

set RETRY=0
:clone_repo
if exist gpt4free (
    echo Removing old repository...
    rmdir /s /q gpt4free
)


git clone https://github.com/xtekky/gpt4free.git && goto clone_success

set /a RETRY+=1
if %RETRY% lss 3 (
    echo.
    echo Clone failed. Retrying attempt %RETRY%...
    timeout /t 5 >nul
    goto clone_repo
) else (
    echo.
    echo Clone failed. Exiting script.
    exit /b 1
)

:clone_success
cd gpt4free

:: Create virtual environment
python -m venv venv
call venv\Scripts\activate

:: Install dependencies
pip install --upgrade pip
pip install -r requirements.txt

endlocal
                