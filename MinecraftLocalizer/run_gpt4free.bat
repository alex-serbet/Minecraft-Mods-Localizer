@echo off

cd gpt4free

if not exist venv (
    echo [INFO] Virtual environment is not exist. Creating...
    python -m venv venv
)

call venv\Scripts\activate

pip show g4f >nul 2>&1
if %errorlevel% neq 0 (
    echo [INFO] Installing dependencies...
    pip install --upgrade pip
    pip install -r requirements.txt
)

python -m g4f.api.run
