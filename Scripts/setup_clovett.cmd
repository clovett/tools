@echo off
set keyfile=~/.ssh/clovett
if not exist "%keyfile%" goto :nokey
if "%clovett_email%" == "" goto :noemail
git config core.sshCommand "ssh -i %keyfile%"
git config user.name %USERNAME%
git config user.email "%clovett_email%"
goto :eof
:nokey
echo Please configure your custom key in %keyfile%"
echo using ssh-keygen -C %clovett_email% -f %keyfile%"
echo then register that on your github account.
goto :eof
:noemail
echo Please configure your 'clovett_email' environment variable.
goto :eof
