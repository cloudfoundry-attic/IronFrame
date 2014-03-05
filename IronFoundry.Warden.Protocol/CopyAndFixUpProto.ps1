Set-StrictMode -Version Latest

$warden_proto_dir = '.\warden\warden-protocol\lib\warden\protocol\pb'
$target_proto_dir = '.\pb'

if (!(Test-Path $warden_proto_dir))
{
    Write-Error "Directory '$warden_proto_dir' containing *.proto files does not exist, exiting."
    exit 1
}

mkdir -ErrorAction SilentlyContinue $target_proto_dir

$warden_proto_files = Get-ChildItem $warden_proto_dir -File -Filter '*.proto'
foreach ($proto_file in $warden_proto_files)
{   
    (Get-Content -Path $proto_file.FullName) | ForEach-Object {
        $_ -replace 'package warden;', 'package IronFoundry.Warden.Protocol;'
    } | Set-Content (Join-Path $target_proto_dir $proto_file.Name)
}

# Per-file tweaks
# info.proto
(Get-Content -Path .\pb\info.proto) | ForEach-Object {
    $_ -replace 'memory_stat', 'memory_stat_info' `
       -replace 'cpu_stat',    'cpu_stat_info' `
       -replace 'disk_stat',   'disk_stat_info' `
       -replace 'bandwidth_stat', 'bandwidth_stat_info'
} | Set-Content .\pb\info.proto

# create.proto
(Get-Content -Path .\pb\create.proto) | ForEach-Object { $_ -replace 'Mode mode', 'Mode bind_mount_mode' } | Set-Content .\pb\create.proto

# message.proto
(Get-Content -Path .\pb\message.proto) | ForEach-Object { $_ -replace 'Type type', 'Type message_type' } | Set-Content .\pb\message.proto
