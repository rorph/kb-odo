@echo off
REM Test script for Keyboard Mouse Odometer (Windows)
REM This script runs all tests with various options

setlocal EnableDelayedExpansion

echo ======================================
echo Running Keyboard Mouse Odometer Tests
echo ======================================

REM Initialize variables
set COVERAGE=
set VERBOSE=
set FILTER=

REM Parse command line arguments
:parse_args
if "%~1"=="" goto :run_tests
if "%~1"=="--coverage" (
    set COVERAGE=1
    shift
    goto :parse_args
)
if "%~1"=="--verbose" (
    set VERBOSE=1
    shift
    goto :parse_args
)
if "%~1"=="--filter" (
    set FILTER=%~2
    shift
    shift
    goto :parse_args
)
echo Unknown option: %~1
echo Usage: test.cmd [--coverage] [--verbose] [--filter "test-filter"]
exit /b 1

:run_tests
REM Build in Debug mode
echo Building solution in Debug mode...
dotnet build -c Debug
if %ERRORLEVEL% neq 0 goto :error

REM Prepare test command
set TEST_CMD=dotnet test -c Debug --no-build

if defined VERBOSE (
    set TEST_CMD=%TEST_CMD% --verbosity detailed
) else (
    set TEST_CMD=%TEST_CMD% --verbosity normal
)

if defined FILTER (
    set TEST_CMD=%TEST_CMD% --filter "%FILTER%"
)

if defined COVERAGE (
    echo Running tests with code coverage...
    set TEST_CMD=%TEST_CMD% --collect:"XPlat Code Coverage" --results-directory ./TestResults
) else (
    echo Running tests...
)

REM Run tests
%TEST_CMD%
if %ERRORLEVEL% neq 0 goto :error

if defined COVERAGE (
    echo.
    echo Code coverage results saved to ./TestResults/
    echo To view coverage report, install and run:
    echo   dotnet tool install -g dotnet-reportgenerator-globaltool
    echo   reportgenerator -reports:./TestResults/*/coverage.cobertura.xml -targetdir:./TestResults/CoverageReport -reporttypes:Html
)

echo.
echo ======================================
echo All tests completed!
echo ======================================
goto :eof

:error
echo Tests failed!
exit /b 1