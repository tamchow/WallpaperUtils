param (
    [Parameter(Position = 1, HelpMessage = "Daily time schedule")]
    [Datetime]
    $time=(Get-Date 8pm)
)

function Unregister-ExistingTask($task_name)
{
    try
    {
        if((Get-ScheduledTask -TaskName $task_name) -ne $null)
        {
            Unregister-ScheduledTask -TaskName $task_name -Confirm:$false
        }
    }
    catch{
        #Do nothing - expected
    }
}

Write-Host "Time = $time"

$trigger = @()
$trigger += New-ScheduledTaskTrigger -Daily -At $time
$trigger += New-ScheduledTaskTrigger -AtLogon

$task_name = "Latest Spotlight Wallpaper"

Unregister-ExistingTask($task_name)

$action = New-ScheduledTaskAction -Execute './WallpaperUtilities.exe' -Argument '-s'

Register-ScheduledTask -Action $action -Trigger $trigger -TaskName $task_name -Description "Saves Spotlight Wallpapers"