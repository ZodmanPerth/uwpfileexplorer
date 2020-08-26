$root = "C:\dev\-- Data\10000 folders"

$numberOfFolders = 10000;

for($i = 0; $i -lt $numberOfFolders; $i++)
{
    $name = "Folder #$i"
    New-Item -Path $root -Name $name -ItemType "directory"
}