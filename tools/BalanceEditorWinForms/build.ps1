$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceRoot = Join-Path $scriptRoot "src"
$outputPath = Join-Path $scriptRoot "FightBalanceEditor.exe"
$compilerPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $compilerPath)) {
    throw "找不到 C# 编译器：$compilerPath"
}

$sources = @(
    (Join-Path $sourceRoot "Program.cs")
    (Join-Path $sourceRoot "CsvTable.cs")
    (Join-Path $sourceRoot "HeroCatalog.cs")
    (Join-Path $sourceRoot "BalanceEditorForm.cs")
)

& $compilerPath `
    /nologo `
    /target:winexe `
    /out:$outputPath `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    $sources

Write-Host "已生成：" $outputPath
