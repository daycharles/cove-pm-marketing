$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$python = ".\.venv\Scripts\python.exe"
& $python .\control_room.py dashboard
& $python .\control_room.py run-next
& $python .\control_room.py dashboard
