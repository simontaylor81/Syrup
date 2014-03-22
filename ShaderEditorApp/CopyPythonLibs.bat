@ECHO OFF

SET SOURCE="%~dp0\Lib"
SET DEST="%~1\IronPythonLibs"

robocopy %SOURCE% %DEST% /S

:: robocopy return codes are a bit funny...
IF errorlevel 8 EXIT /B 1
