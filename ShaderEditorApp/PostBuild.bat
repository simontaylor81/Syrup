@ECHO OFF

REM %1 == Target directory.
REM %2 == Config
REM %3 == Platform

ECHO Permforming Post Build at %DATE% %TIME% >PostBuild.log

:: Copy python library files.
CALL "%~dp0\CopyPythonLibs.bat" %1 >>PostBuild.log
IF ERRORLEVEL 1 GOTO Error

:: Copy AssImp dll.
IF [%3] == [x64] (
	SET AssimpDll=Assimp64.dll
) ELSE (
	SET AssimpDll=Assimp32.dll
)

SET AssimpVer=AssimpNet-2.1.2 Refresh

robocopy "%~dp0\..\ThirdParty\%AssimpVer%\%3" %1 %AssimpDll% >>PostBuild.log

:: robocopy return codes are a bit funny...
IF ERRORLEVEL 8 GOTO Error

GOTO :EOF

:Error
ECHO Post Build failed. See %CD%\PostBuild.log for details.
EXIT /B 1
