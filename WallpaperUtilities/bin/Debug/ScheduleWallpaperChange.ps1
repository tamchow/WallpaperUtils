param (
    [Parameter(Position = 1, HelpMessage = "Choice of latest or random Spotlight wallpaper")]
    [bool]
    $latest=$false,
    [Parameter(Position = 2, HelpMessage = "Daily time schedule")]
    [Datetime]
    $time=(Get-Date 8pm),
    [Parameter(Position = 3, HelpMessage = "Synchronize Desktop and Lockscreen Backgrounds?")]
    [bool]
    $syncWPs=$false
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
$trigger += New-ScheduledTaskTrigger -AtLogon -User "$(whoami)"

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
if($syncWPs)
{
    $task_name = "Sync Wallpaper"
    
    Unregister-ExistingTask($task_name)    

    $action = New-ScheduledTaskAction -Execute $app_name -Argument '-cw --desktop'

    Register-ScheduledTask -Action $action -Trigger $trigger -TaskName $task_name -Description "Synchronizes desktop and lockscreen wallpapers"
}

Write-Host "Scheduled task for $task_name registered."