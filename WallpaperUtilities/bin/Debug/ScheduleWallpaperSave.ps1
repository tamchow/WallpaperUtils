param (
    [Parameter(Position = 1, HelpMessage = "Daily time schedule")]
    [Datetime]
    $time=(Get-Date 8pm),
    [Parameter(Position = 2, HelpMessage = "Save location")]
    [string]
    $location=""
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

$trigger = @()
$trigger += New-ScheduledTaskTrigger -Daily -At $time
$trigger += New-ScheduledTaskTrigger -AtLogon -User "$(whoami)"

$task_name = "Save Spotlight Wallpapers"

Unregister-ExistingTask($task_name)

$action = New-ScheduledTaskAction -Execute "$(Resolve-Path './WallpaperUtilities.exe')" -Argument "-s $location"

Register-ScheduledTask -Action $action -Trigger $trigger -TaskName $task_name -Description "Saves Spotlight Wallpapers"

Write-Host "Scheduled task for $task_name registered."