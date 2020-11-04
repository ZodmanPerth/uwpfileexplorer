$root = "C:\dev\-- Data\1000 files"
if (-not (Test-Path -Path $root)) {
    New-Item -Path $root -ItemType Directory | Out-Null
}

$numberOfFiles = 1000;

for($i = 0; $i -lt $numberOfFiles; $i++)
{
    $name = "File #$i"
    New-Item -Path $root -Name $name -ItemType File
}