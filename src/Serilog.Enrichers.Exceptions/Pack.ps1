param(
    [string] $project = "."
)

if( -Not (Test-Path $project) )
{
    throw "$project is not existed"
}

$csproj = ""

if( Test-Path $project -PathType Container)
{
    $csproj = (Get-ChildItem -Path $project -Filter *.csproj).FullName
}
else
{
    $csproj = $project
}

dotnet build -c Release $csproj
if( -Not $? )
{
    throw "build failed"
}

$releasePath = Join-Path (Split-Path $csproj) "bin\Release"

Remove-Item -Path $releasePath\*.nupkg -Force -ErrorAction Ignore

$pack = dotnet pack -c Release $csproj
if( -Not $? )
{
    throw "pack failed"
}

if( [string]$pack -match ".*(?<n>[C-Z]\:\\.*\.nupkg).*" ) 
{    
    Write-Host pack: $Matches.n
    dotnet nuget push $Matches.n -s http://nuget.ytzx.com -k ytzx1234    
}
else
{
    Write-Host $pack
    throw ".nupkg not found"
}