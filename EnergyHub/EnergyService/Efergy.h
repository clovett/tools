#ifndef _EFERGY_H
#define _EFERGY_H

// Read standard input from RTL-SDR library and print out the watts
void ReadEnergyData(volatile sig_atomic_t* cancelSignal);

#endif
