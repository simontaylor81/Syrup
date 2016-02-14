@ECHO OFF

SETLOCAL

CALL "%VS140COMNTOOLS%VsMSBuildCmd.bat"

ECHO Building solution...
msbuild .\ShaderEditor.sln /property:Configuration=Debug /property:Platform=x86 /verbosity:minimal /nologo

ECHO Running tests...
.\packages\xunit.runner.console.2.1.0\tools\xunit.console.x86.exe .\SRPTests\bin\x86\Debug\SRPTests.dll