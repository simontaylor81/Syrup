@ECHO OFF

SET SOURCE="%~dp0\..\packages\IronPython.StdLib.2.7.3\content\Lib"
SET DEST="%~1\IronPythonLibs"

robocopy %SOURCE% %DEST% /S

:: robocopy return codes are a bit funny...
IF errorlevel 8 EXIT /B 1
