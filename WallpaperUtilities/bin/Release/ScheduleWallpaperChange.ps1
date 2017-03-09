param (
    [Parameter(Position = 1, HelpMessage = "Choice of latest or random Spotlight wallpaper")]
    [bool]
    $latest=$false,
    [Parameter(Position = 2, HelpMessage = "Daily time schedule")]
    [Datetime]
    $time=(Get-Date 8pm),
    [Parameter(Position = 3, HelpMessage = "Set lockscreen image as desktop wallpaper")]
    [bool]
    $syncDesktopLockscreen=$false,
    [Parameter(Position = 4, HelpMessage = "Set desktop wallpaper as lockscreen image")]
    [bool]
    $syncLockscreenDesktop=$false,
    [Parameter(Position = 5, HelpMessage = "Extra arguments for executable")]
    [string]
    $extraArgs="",
    [Parameter(Position = 6, HelpMessage = "Run on current user login")]
    [bool]
    $onLogon=$true
)

function Unregister-ExistingTask($task_name)
{
    try
    {
        $tasks = (Get-ScheduledTask | Where-Object {$_.TaskName -like $task_name})
        if(($tasks -ne $null))
        {
            foreach($task in $tasks){
                Unregister-ScheduledTask -TaskName $task_name -Confirm:$false
            }
        }
    }
    catch{
        #Do nothing - expected
    }
}

Write-Host "Time = $time"

$app_name = "$(Resolve-Path './WallpaperUtilities.exe')"

$task_name = ""

$trigger = @()
$trigger += New-ScheduledTaskTrigger -Daily -At $time
if($onLogon){
$trigger += New-ScheduledTaskTrigger -AtLogon -User "$(whoami)"
}

if($latest)
{

    $task_name = "Latest Spotlight Wallpaper"

    Unregister-ExistingTask($task_name)

    $action = New-ScheduledTaskAction -Execute $app_name -Argument '-slw'
    
    Register-ScheduledTask -Action $action -Trigger $trigger -TaskName $task_name -Description "Changes Wallpaper to latest Spotlight"
}
else
{
    $task_name = "Random Spotlight Wallpaper"
    
    Unregister-ExistingTask($task_name)    

    $action = New-ScheduledTaskAction -Execute $app_name -Argument '-srw'

    Register-ScheduledTask -Action $action -Trigger $trigger -TaskName $task_name -Description "Changes Wallpaper to random Spotlight"
}
if($syncDesktopLockscreen)
{
    $task_name = "Set lockscreen image as desktop wallpaper"
    
    Unregister-ExistingTask($task_name)
    
    $action = New-ScheduledTaskAction -Execute $app_name -Argument "-cw --desktop $extraArgs"
    
    Register-ScheduledTask -Action $action -Trigger $trigger -TaskName $task_name -Description "Sets lockscreen image as desktop wallpaper"
}
if($syncLockscreenDesktop)
{
    $task_name = "Set desktop wallpaper as lockscreen image"
    
    Unregister-ExistingTask($task_name)
    
    $action = New-ScheduledTaskAction -Execute $app_name -Argument "-cw --lockscreen $extraArgs"
    
    Register-ScheduledTask -Action $action -Trigger $trigger -TaskName $task_name -Description "Sets desktop wallpaper as lockscreen image"
}


Write-Host "Scheduled task for $task_name registered."