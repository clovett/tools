# USB Power Monitor

This app uses a simple Gravity: I2C Digital Wattmeter to monitor the power usage of 
a USB device and the voltage of the batteries providing power to that device, and
therefore it can also measure the quality of the batteries you are using.

You can get the sensor from 
[dfrobot.com](https://www.dfrobot.com/search-Gravity:%20I2C%20Digital%20Wattmeter%20.html) 
for only $6 or so.  

Then you can hook it up to an Arduino using the DFRobot_INA219 library and the code in the
[DigitalWattmeter](DigitalWattmeter/DigitalWattmeter.ino) sketch.

The python code then reads the serial input and logs the data to a file so you can then do
whatever you want with the data.

## Setup

Copy the DFRobot_INA219 library from github to your %USERPROFILE%\Documents\Arduino\libraries folder
then load the Arduino sketch and upload it.

Now run PowerMonitor.py with --port name equal to the COM port for your Arduino.
