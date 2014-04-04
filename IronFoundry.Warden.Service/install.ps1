param(
	$InstallPath,
	$ServiceAccount,
	$ServicePassword,
	$RootDataPath
)

Write-Host "Installing warden service"
#
# Install Warden Service
#

$wardenService = join-path $InstallPath "IronFoundry.Warden.Service.exe"

. $wardenService uninstall
        
. $wardenService install -username:"$env:computername\$ServiceAccount" -password:"$ServicePassword" --autostart
$Global:LastExitCode = $LastExitCode

if ($LastExitCode -ne 0)
{	
	Write-Error "Failed to install service."	
	Exit $LastExitCode
}

Write-Host "Updating configuration file"

$configFile = join-path $Installpath "IronFoundry.Warden.Service.exe.config"

$configContent = [xml](gc $configFile)
$configContent.configuration['warden-server'].SetAttribute('container-basepath', "$RootDataPath\warden\containers")
$configContent.configuration.nlog.targets.SelectSingleNode("./*[local-name()='target' and @name='file']").SetAttribute('fileName', "$RootDataPath\log\if_warden.log")
$configContent.configuration.nlog.targets.SelectSingleNode("./*[local-name()='target' and @name='file']").SetAttribute('archiveFileName', "$RootDataPath\log\if-warden-{#}.log")
$configContent.Save($configFile)