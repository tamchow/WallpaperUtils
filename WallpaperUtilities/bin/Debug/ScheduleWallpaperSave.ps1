param (
    [Parameter(Position = 1, HelpMessage = "Daily time schedule")]
    [Datetime]
    $time=(Get-Date 7pm),
    [Parameter(Position = 2, HelpMessage = "Save location")]
    [string]
    $location="",
    [Parameter(Position = 3, HelpMessage = "Run on current user login")]
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

$trigger = @()
$trigger += New-ScheduledTaskTrigger -Daily -At $time
if($onLogon){
$trigger += New-ScheduledTaskTrigger -AtLogon -User "$(whoami)"
}

$task_name = "Save Spotlight Wallpapers"

Unregister-ExistingTask($task_name)

$action = New-ScheduledTaskAction -Execute "$(Resolve-Path './WallpaperUtilities.exe')" -Argument "-s ${location} -nm"

Register-ScheduledTask -Action $action -Trigger $trigger -TaskName $task_name -Description "Saves Spotlight Wallpapers"

Write-Host "Scheduled task for $task_name registered."