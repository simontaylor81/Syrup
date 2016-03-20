@ECHO OFF

SETLOCAL

CALL "%VS140COMNTOOLS%VsMSBuildCmd.bat"

ECHO Building solution...
msbuild .\ShaderEditor.sln /property:Configuration=Debug /verbosity:minimal /nologo

ECHO Running tests...
.\packages\xunit.runner.console.2.1.0\tools\xunit.console.exe .\SRPTests\bin\Debug\SRPTests.dll