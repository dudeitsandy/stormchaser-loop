param(
    [string]$ProjectPath = (Resolve-Path "$PSScriptRoot/../../../..").Path,
    [string]$EditorData = 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data'
)
$ErrorActionPreference = 'Stop'
$checkDirectory = Join-Path $env:TEMP 'stormchaser-codex-s7-check'
New-Item -ItemType Directory -Force $checkDirectory | Out-Null
Push-Location $ProjectPath
try {
    $gameResponse = Get-ChildItem Library/Bee/artifacts -Recurse -Filter StormChaser.rsp |
        Where-Object { $_.FullName -match 'E\.dag' } | Select-Object -First 1
    $testResponse = Get-ChildItem Library/Bee/artifacts -Recurse -Filter StormChaser.Tests.rsp |
        Where-Object { $_.FullName -match 'E\.dag' } | Select-Object -First 1
    $compiler = Join-Path $EditorData 'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
    $dotnet = Join-Path $EditorData 'DotNetSdk/dotnet.exe'
    $gameArgs = Get-Content $gameResponse.FullName | Where-Object { $_ -notmatch '^-(out|refout):|^/additionalfile:|^-analyzer:|\.cs"?$' }
    $gameArgs += '-out:"' + (Join-Path $checkDirectory 'StormChaser.dll') + '"'
    $gameArgs += Get-ChildItem Assets/Scripts -Recurse -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' }
    $gameRsp = Join-Path $checkDirectory 'StormChaser.rsp'
    Set-Content -Encoding UTF8 $gameRsp $gameArgs
    & $dotnet $compiler ('@' + $gameRsp)
    if ($LASTEXITCODE -ne 0) { throw 'Gameplay compilation failed.' }
    $testArgs = Get-Content $testResponse.FullName | Where-Object { $_ -notmatch '^-(out|refout):|^/additionalfile:|^-analyzer:|\.cs"?$' }
    $testArgs = $testArgs | ForEach-Object {
        if ($_ -match '^-r:.*StormChaser\.ref\.dll') { '-r:"' + (Join-Path $checkDirectory 'StormChaser.dll') + '"' } else { $_ }
    }
    $testArgs += '-out:"' + (Join-Path $checkDirectory 'StormChaser.Tests.dll') + '"'
    $testArgs += Get-ChildItem Assets/Tests/Presentation -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' }
    if (Test-Path Assets/Tests/Environment) {
        $testArgs += Get-ChildItem Assets/Tests/Environment -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' }
    }
    $testRsp = Join-Path $checkDirectory 'PresentationTests.rsp'
    Set-Content -Encoding UTF8 $testRsp $testArgs
    & $dotnet $compiler ('@' + $testRsp)
    if ($LASTEXITCODE -ne 0) { throw 'Presentation test compilation failed.' }
    $runnerCode = @'
using System;
using System.Reflection;
using NUnit.Framework;
public static class PresentationChecks {
    public static int Main() {
        int passed = 0;
        try {
            foreach (var type in new[] { typeof(IndicatorGeometryTests), typeof(WindVfxEmissionTests), typeof(FunnelSurfaceGeometryTests), typeof(VehicleFeedbackLevelsTests), typeof(FunnelLifecycleVisualTests), typeof(StormCueTests) }) {
                var fixture = Activator.CreateInstance(type);
                foreach (var method in type.GetMethods()) {
                    foreach (TestCaseAttribute test in method.GetCustomAttributes(typeof(TestCaseAttribute), true)) {
                        method.Invoke(fixture, test.Arguments); passed++;
                    }
                    if (method.IsDefined(typeof(TestAttribute), true)) { method.Invoke(fixture, null); passed++; }
                }
            }
            Console.WriteLine("PASS: " + passed + " pure presentation NUnit cases (standalone managed runner; not Unity EditMode).");
            return 0;
        } catch (Exception e) { Console.WriteLine(e.InnerException ?? e); return 1; }
    }
}
'@
    $runnerSource = Join-Path $checkDirectory 'PresentationChecks.cs'
    Set-Content -Encoding UTF8 $runnerSource $runnerCode
    $runnerArgs = $testArgs | Where-Object { $_ -notmatch '^-out:|\.cs"?$|^-target:' }
    $runnerArgs += '-target:exe'
    $runnerArgs += '-out:"' + (Join-Path $checkDirectory 'PresentationChecks.dll') + '"'
    $runnerArgs += '-r:"' + (Join-Path $checkDirectory 'StormChaser.Tests.dll') + '"'
    $runnerArgs += '"' + $runnerSource + '"'
    $runnerRsp = Join-Path $checkDirectory 'PresentationChecks.rsp'
    Set-Content -Encoding UTF8 $runnerRsp $runnerArgs
    & $dotnet $compiler ('@' + $runnerRsp)
    if ($LASTEXITCODE -ne 0) { throw 'Managed runner compilation failed.' }
    Copy-Item -LiteralPath (Join-Path $EditorData 'Managed/UnityEngine/UnityEngine.CoreModule.dll') -Destination $checkDirectory
    $nunitReference = Get-Content $testResponse.FullName | Where-Object { $_ -match '^-r:.*nunit.framework.dll' } | Select-Object -First 1
    Copy-Item -LiteralPath ($nunitReference.Substring(3).Trim('"')) -Destination $checkDirectory
    Set-Content -Encoding UTF8 (Join-Path $checkDirectory 'PresentationChecks.runtimeconfig.json') '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}'
    & (Join-Path $EditorData 'NetCoreRuntime/dotnet.exe') (Join-Path $checkDirectory 'PresentationChecks.dll')
    if ($LASTEXITCODE -ne 0) { throw 'Presentation tests failed.' }
} finally { Pop-Location }
