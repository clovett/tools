var arg = WScript.Arguments(0);

if (arg == null || arg == "/?" || arg == "-?") 
{
  WScript.echo("Usage: hresult code");
  WScript.echo("Converts decimal hresult like -2147009293 to the more recognizable hex version 0x80073cf3");
} 

var hr = parseInt(arg);
if (hr < 0) {
  hr += 0x100000000;
}
WScript.echo(hr.toString(16));