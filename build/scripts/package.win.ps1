Remove-Item -Path build\SourceGit\*.pdb -Force
Copy-Item -Path LICENSE, THIRD-PARTY-LICENSES.md -Destination build\SourceGit
Compress-Archive -Path build\SourceGit -DestinationPath "build\sourcegit-hopoduck_${env:VERSION}.${env:RUNTIME}.zip" -Force
