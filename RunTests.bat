@ECHO OFF

SETLOCAL

CALL "%VS140COMNTOOLS%VsMSBuildCmd.bat"

ECHO Building solution...
msbuild .\ShaderEditor.sln /property:Configuration=Debug /property:Platform=x86 /verbosity:minimal /nologo

ECHO Running tests...
.\packages\NUnit.Console.3.0.1\tools\nunit3-console.exe .\SRPTests\bin\x86\Debug\SRPTests.dll