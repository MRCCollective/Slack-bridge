param(
    [string]$HostName = "82.70.47.203",
    [string]$UserName = "ubuntu",
    [string]$KeyPath = "$env:USERPROFILE\.ssh\oracle.key",
    [string]$RemoteAppDir = "/opt/stacks/slackbridge",
    [string]$PublicUrl = "https://slackbridge.mrcheng.se/",
    [switch]$SkipBuildCheck,
    [switch]$UploadCompose
)

$ErrorActionPreference = "Stop"

$ProjectDir = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$ArchivePath = Join-Path ([System.IO.Path]::GetTempPath()) "slackbridge-deploy.tar"
$RemoteArchive = "/tmp/slackbridge-deploy.tar"

if (-not $SkipBuildCheck) {
    dotnet build (Join-Path $ProjectDir "SlackBridge.Web.csproj") -c Release -o (Join-Path $ProjectDir "bin_verify")
}

if (Test-Path $ArchivePath) {
    Remove-Item $ArchivePath -Force
}

Push-Location $ProjectDir
try {
    tar `
        --exclude="./bin" `
        --exclude="./bin_verify" `
        --exclude="./obj" `
        --exclude="./*.log" `
        --exclude="./appsettings.*.local.json" `
        -cf $ArchivePath .
}
finally {
    Pop-Location
}

$SshTarget = "$UserName@$HostName"
ssh -i $KeyPath $SshTarget "sudo mkdir -p $RemoteAppDir/src && sudo chown -R $UserName`:$UserName $RemoteAppDir"
scp -i $KeyPath $ArchivePath "$SshTarget`:$RemoteArchive"
ssh -i $KeyPath $SshTarget "rm -rf $RemoteAppDir/src/* && tar -xf $RemoteArchive -C $RemoteAppDir/src && rm $RemoteArchive"

if ($UploadCompose) {
    scp -i $KeyPath (Join-Path $PSScriptRoot "docker-compose.yml") "$SshTarget`:$RemoteAppDir/docker-compose.yml"
}

ssh -i $KeyPath $SshTarget "cd $RemoteAppDir && sudo docker compose up -d --build slackbridge-web && sudo docker compose ps && sudo docker logs --tail 120 slackbridge-web"

if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
    curl.exe -I $PublicUrl
}
else {
    curl -I $PublicUrl
}
