$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
.\.venv\Scripts\python.exe .\langgraph_runner.py --task .\tasks\inbox\example-maintenance-page.md
