param(
    [Parameter(Mandatory = $true)][string]$FilePath,
    [string[]]$ArgumentList = @(),
    [int]$TimeoutSeconds = 30,
    [string]$WorkingDirectory = (Get-Location).Path
)

$process = [Diagnostics.Process]::new()
$process.StartInfo = [Diagnostics.ProcessStartInfo]::new()
$process.StartInfo.FileName = $FilePath
$process.StartInfo.WorkingDirectory = $WorkingDirectory
$process.StartInfo.UseShellExecute = $false
$process.StartInfo.RedirectStandardOutput = $true
$process.StartInfo.RedirectStandardError = $true
foreach ($argument in $ArgumentList) { [void]$process.StartInfo.ArgumentList.Add($argument) }
[void]$process.Start()
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill($true)
    $process.WaitForExit()
    [pscustomobject]@{ success = $false; timedOut = $true; exitCode = $null; stdout = $stdout.GetAwaiter().GetResult(); stderr = $stderr.GetAwaiter().GetResult() } | ConvertTo-Json -Compress
    exit 124
}
[pscustomobject]@{ success = ($process.ExitCode -eq 0); timedOut = $false; exitCode = $process.ExitCode; stdout = $stdout.GetAwaiter().GetResult(); stderr = $stderr.GetAwaiter().GetResult() } | ConvertTo-Json -Compress
exit $process.ExitCode
