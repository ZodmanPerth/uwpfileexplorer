$root = "C:\dev\-- Data\1000 folders"

$numberOfFolders = 1000;

for($i = 0; $i -lt $numberOfFolders; $i++)
{
    $name = "Folder #$i"
    New-Item -Path $root -Name $name -ItemType "directory"
}