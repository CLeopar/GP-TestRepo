set WORKSPACE=..\..

set LUBAN_DLL=%WORKSPACE%\Tools\Luban\LubanRelease\Luban.dll
set CONF_ROOT=%WORKSPACE%\Assets\Config\Excel

::Client
dotnet %LUBAN_DLL% ^
    --customTemplateDir CustomTemplate ^
    -t Client ^
    -c cs-bin ^
    -d bin ^
    -d json ^
    --conf %CONF_ROOT%\__luban__.conf ^
    -x outputCodeDir=%WORKSPACE%\Assets\Scripts\Model\Generate\Config ^
    -x bin.outputDataDir=%WORKSPACE%\Assets\Bundles\Config\Bin ^
    -x json.outputDataDir=%WORKSPACE%\Assets\Bundles\Config\Json ^
    -x lineEnding=CRLF ^
    

echo ==================== FuncConfig : GenClientFinish ====================

if %ERRORLEVEL% NEQ 0 (
    echo An error occurred, press any key to exit.
    pause
    exit /b
)