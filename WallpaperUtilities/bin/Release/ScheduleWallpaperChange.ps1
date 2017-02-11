param (
    [Parameter(Position = 1, HelpMessage = "Choice or latest or random Spotlight wallpaper")]
    [bool]
    $latest=$false,
    [Parameter(Position = 2, HelpMessage = "Daily time schedule")]
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

$task_name = ""

$trigger = @()
$trigger += New-ScheduledTaskTrigger -Daily -At $time
$trigger += New-ScheduledTaskTrigger -AtLogon

if($latest)
{

    $task_name = "Latest Spotlight Wallpaper"

    Unregister-ExistingTask($task_name)

    $action1 = New-ScheduledTaskAction -Execute './WallpaperUtilities.exe' -Argument '-slw'

    $trigger1 = @()
    $trigger1 += New-ScheduledTaskTrigger -Daily -At $time
    $trigger1 += New-ScheduledTaskTrigger -AtLogOn

    Register-ScheduledTask -Action $action1 -Trigger $trigger -TaskName $task_name -Description "Changes Wallpaper to latest Spotlight"
}
else
{
    $task_name = "Random Spotlight Wallpaper"
    
    Unregister-ExistingTask($task_name)    

    $action2 = New-ScheduledTaskAction -Execute './WallpaperUtilities.exe' -Argument '-srw'

    Register-ScheduledTask -Action $action2 -Trigger $trigger -TaskName $task_name -Description "Changes Wallpaper to random Spotlight"
}

Write-Host "Scheduled task for $task_name registered."