$SecureString = ConvertTo-SecureString "Quick_Silver" -AsPlainText -Force
Unlock-BitLocker -MountPoint "E:" -Password $SecureString