@echo off
setlocal
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b %errorlevel%
cd /d "%~dp0.."
if not exist ".utmp\gameplay" mkdir ".utmp\gameplay"
cl /nologo /std:c++20 /MP4 /EHsc /W4 /utf-8 /I "EverWindServer\LoginServer" "EverWindServer\LoginServer\Gameplay\*.cpp" "EverWindServer\LoginServer\Gameplay\Rule\*.cpp" "EverWindServer\LoginServer\Gameplay\Map\*.cpp" "EverWindServer\LoginServer\Gameplay\Refill\*.cpp" "EverWindServer\LoginServer\Gameplay\Validation\*.cpp" "EverWindServer\LoginServer\Gameplay\Validation\Ingame\*.cpp" "tests\gameplay\CoreTests.cpp" "tests\gameplay\FeatureTests.cpp" "tests\gameplay\ValidationTests.cpp" "tests\gameplay\Client.cpp" /Fe:".utmp\gameplay\CoreTests.exe" /Fo:".utmp\gameplay\\"
if errorlevel 1 exit /b %errorlevel%
".utmp\gameplay\CoreTests.exe" "tests\gameplay\fixtures\Gameplay.rules"
