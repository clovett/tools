/*!
   file getVoltageCurrentPower.ino
   SEN0291 Gravity: I2C Digital Wattmeter
   The module is connected in series between the power supply and the load to read the voltage, current and power
   The module has four I2C, these addresses are:
   INA219_I2C_ADDRESS1  0x40   A0 = 0  A1 = 0
   INA219_I2C_ADDRESS2  0x41   A0 = 1  A1 = 0
   INA219_I2C_ADDRESS3  0x44   A0 = 0  A1 = 1
   INA219_I2C_ADDRESS4  0x45   A0 = 1  A1 = 1

   Copyright    [DFRobot](http://www.dfrobot.com), 2018
   Copyright    GNU Lesser General Public License
   version  V0.1
   date  2018-12-13
*/

#include <DFRobot_INA219.h>

DFRobot_INA219 ina219;

// Revise the following two paramters according to actula reading of the INA219 and the multimeter
// for linearly calibration
float ina219Reading_mA = 1000;
float extMeterReading_mA = 1000;

void setup(void)
{
    Serial.begin(115200);
    while (!Serial) {
        delay(1);
    }

    Serial.println("Hello!");
    ina219.setI2cAddr(INA219_I2C_ADDRESS4);     //Change I2C address by dialing DIP switch
    while(!ina219.begin()){                     //Begin return True if succeed, otherwise return False
        delay(2000);
    }
    ina219.linearCal(ina219Reading_mA, extMeterReading_mA);
    //ina219.reset();                           //Reset all registers to default values

    Serial.println("Measuring voltage and current with INA219 ...");
}

void loop(void)
{
    float shuntVoltage = 0;
    float busVoltage = 0;
    float current_mA = 0;
    float power_mW = 0;

    shuntVoltage = ina219.getShuntVoltage_mV();
    busVoltage = ina219.getBusVoltage_V();
    current_mA = ina219.getCurrent_mA();
    power_mW = ina219.getPower_mW();

    Serial.print("Bus Voltage:   "); Serial.print(busVoltage,3); Serial.println(" V");
    Serial.print("Shunt Voltage: "); Serial.print(shuntVoltage,2); Serial.println(" mV");
    Serial.print("Current:       "); Serial.print(current_mA,0); Serial.println(" mA");
    Serial.print("Power:         "); Serial.print(power_mW,0); Serial.println(" mW");
    Serial.println("");

    delay(1000);
}
