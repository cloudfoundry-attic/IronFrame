param(
	$InstallPath,
	$ServiceAccount,
	$ServicePassword,
	$RootDataPath,
    $UsersGroupName = "WardenUsers"
)



<#
.summary
  Create a new Access Control Rule
#>
function New-AccessRule
{
    [CmdletBinding()]
    param(
        [System.Security.AccessControl.AccessControlType] $AccessType, # Allow or Deny 
        [System.Security.AccessControl.FileSystemRights] $Rights,
        [string] $Account
    )

    $inheritance = [System.Security.AccessControl.InheritanceFlags] "ContainerInherit", "ObjectInherit"
    $propagation = [System.Security.AccessControl.PropagationFlags]::None 
    $ntAccount = New-Object System.Security.Principal.NTAccount($Account)
  
    $ace = new-object System.Security.AccessControl.FileSystemAccessRule `
                      @($ntAccount, $rights, $inheritance, $propagation, $accessType)

    return $ace
}


<#
.summary
  Create an Access Rule that denies read access to an account
#>
function Deny-ReadAccess
{
    [CmdletBinding()]
    param(
        [string] $Account
    )

    New-AccessRule -AccessType Deny -Rights ReadAndExecute -Account $Account
}

<#
.summary
  Create an Access Rule that denies write access to an account
#>
function Deny-WriteAccess
{
    [CmdletBinding()]
    param(
        [string] $Account
    )

    New-AccessRule -AccessType Deny -Rights Write -Account $Account
}

<#
.Sumary
    Create an Access Rule that allows read access to an account
#>
function Allow-ReadAccess
{
    [CmdletBinding()]
    param(
        [string] $Account
    )

    New-AccessRule -AccessType Allow -Rights ReadAndExecute -Account $Account
}


<#
.Summary
  Add new access control rules to a set of paths. 

.Description
  The rules are specified in a hashtable with the following format:
  @{ "Path1" = { [Deny-WriteAccess] [; Deny-ReadAcccess] } }
#>
function Apply-AclMap
{
    [CmdletBinding()]
    param(
        [System.Collections.IDictionary] $AclMap
    )

    Write-Verbose "Setting up ACLs for $UsersGroupN..."

    foreach($kv in $AclMap.GetEnumerator())
    {
        $path, $aclScript = $kv.Key, $kv.Value;

        Write-Verbose "${path} -> ${aclScript}";

        # Invoke the scriptblocks which should generate the rules to add.
        $PSDefaultParameterValues = @{ "*:Account" = $UsersGroupName };
        $rulesToAdd = @(& $aclScript);
        
        # Add the new rules to the acl list on the specified path
        # Do NOT use get-acl here because: http://stackoverflow.com/questions/6622124/why-does-set-acl-on-the-drive-root-try-to-set-ownership-of-the-object
        $acl = (Get-Item $path -Force).GetAccessControl('Access') 
        $rulesToAdd | foreach { $acl.AddAccessRule($_) }
        Set-Acl -Path $path -AclObject $acl
    }
}



Write-Host "Installing warden service"
#
# Install Warden Service
#

$wardenService = join-path $InstallPath "IronFoundry.Warden.Service.exe"

. $wardenService stop
. $wardenService uninstall
        
. $wardenService install -username:"$env:computername\$ServiceAccount" -password:"$ServicePassword" --autostart
$Global:LastExitCode = $LastExitCode

if ($LastExitCode -ne 0)
{	
	Write-Error "Failed to install service."	
	Exit $LastExitCode
}

Write-Host "Creating Group for Warden Users: $UsersGroupName"
try {
    . net.exe localgroup $UsersGroupName /ADD 2>&1
}
catch {
    if ($Global:LastExitCode -ne 0){
        if ($Error -match '1379') {
            Write-Host "Group already exists."
        }
        else {
            Write-Error "Failed to add group: $UsersGroupName"
            Exit $Global:LastExitCode
        }
    }
}

# Set restrictions on what the WardenUsers can access.
$AclMap = [ordered] @{ 
  "${env:SystemRoot}\TEMP"                  = { Deny-WriteAccess; Deny-ReadAccess }
  "${env:SystemDrive}\"                     = { Deny-WriteAccess }
  "${env:ProgramData}"                      = { Deny-WriteAccess }
  <# Deny read access to some of the IronFoundry directories #>
  <# Access will be granted by the warden as needed #>
  "${RootDataPath}\log"                     = { Deny-WriteAccess; Deny-ReadAccess }
  "${RootDataPath}\dea_ng"                  = { Deny-WriteAccess; Deny-ReadAccess }
  "${RootDataPath}\dea_ng\admin_buildpacks" = { Allow-ReadAccess }
}

#
# TODO : The container library doesn't yet support bindmounts so we can't restrict write permissions.
#
#Apply-AclMap -Verbose $AclMap


$Global:LastExitCode = 0

Write-Host "Updating configuration file"
$configFile = join-path $Installpath "IronFoundry.Warden.Service.exe.config"

$configContent = [xml](gc $configFile)
$configContent.configuration['warden-server'].SetAttribute('container-basepath', "$RootDataPath\warden\containers")
$configContent.configuration['warden-server'].SetAttribute('warden-users-group', "$UsersGroupName")
$configContent.configuration.nlog.targets.SelectSingleNode("./*[local-name()='target' and @name='file']").SetAttribute('fileName', "$RootDataPath\log\if_warden.log")
$configContent.configuration.nlog.targets.SelectSingleNode("./*[local-name()='target' and @name='file']").SetAttribute('archiveFileName', "$RootDataPath\log\if-warden-{#}.log")
$configContent.Save($configFile)