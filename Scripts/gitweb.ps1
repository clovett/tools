$line = &git config --list --local | grep remote.origin.url
$url = $line.Split("=")[1].Replace("git@github.com:", "https://github.com/")
&start $url
