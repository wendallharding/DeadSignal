param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(
        "FocusedEdit",
        "FocusedPlay",
        "RouteRegression",
        "OptionalRouteRegression",
        "LiveBalance",
        "CombatEvidence",
        "ReleaseValidation",
        "FullEdit",
        "FullPlay")]
    [string]$Lane,

    [string]$TestFilter,

    [string]$UnityEditor = "C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"

$projectPath = Split-Path -Parent $PSScriptRoot
$logsPath = Join-Path $projectPath "Logs"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$laneName = $Lane.ToLowerInvariant()
$resultsPath = Join-Path $logsPath "validation-$laneName-$timestamp.xml"
$logPath = Join-Path $logsPath "validation-$laneName-$timestamp.log"

if (!(Test-Path -LiteralPath $UnityEditor -PathType Leaf))
{
    throw "Unity Editor was not found at '$UnityEditor'."
}

if (!(Test-Path -LiteralPath $logsPath -PathType Container))
{
    New-Item -ItemType Directory -Path $logsPath | Out-Null
}

$platform = if ($Lane -in @("FocusedEdit", "FullEdit")) { "EditMode" } else { "PlayMode" }
$unityArguments = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $projectPath,
    "-runTests",
    "-testPlatform", $platform,
    "-testResults", $resultsPath,
    "-logFile", $logPath
)

if ($Lane -in @("FocusedEdit", "FocusedPlay"))
{
    if ([string]::IsNullOrWhiteSpace($TestFilter))
    {
        throw "$Lane requires -TestFilter with one or more semicolon-separated fully qualified test names."
    }

    $unityArguments += @("-testFilter", $TestFilter)
}
elseif ($Lane -notin @("FullEdit", "FullPlay"))
{
    $unityArguments += @("-testCategory", $Lane)
}

Write-Output "Running $Lane validation with Unity $UnityEditor"
Write-Output "Results: $resultsPath"
Write-Output "Log: $logPath"

$unityProcess = Start-Process -FilePath $UnityEditor -ArgumentList $unityArguments -WindowStyle Hidden -Wait -PassThru
$unityExitCode = $unityProcess.ExitCode

if ($unityExitCode -ne 0)
{
    throw "$Lane validation failed with Unity exit code $unityExitCode. See '$logPath'."
}

if (!(Test-Path -LiteralPath $resultsPath -PathType Leaf))
{
    throw "$Lane validation exited successfully but did not create '$resultsPath'."
}

Write-Output "$Lane validation completed successfully."
