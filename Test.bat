SETLOCAL
CALL Tools\Build\FindVisualStudio.bat || GOTO Error0

REM Deploying test application for integration tests:
test\TestApp\bin\rhetos.exe dbupdate || GOTO Error0

REM Running integration tests:
vstest.console.exe test\Rhetos.Jobs.Test\bin\Debug\Rhetos.Jobs.Test.dll || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
