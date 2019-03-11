/*
 * \file ADS1298.h
 * \brief header File for driver for communication to the ADS1298 ECG Chip by Texas Instruments
 *
 *  Created on: 02.11.2018
 * \author :  Mueller Urs
 */

#ifndef SOURCES_ADS1298_H_
#define SOURCES_ADS1298_H_

//includes:
#include "stdint.h"

//Funktionen:
uint8_t  reg_read(uint8_t addr);
void SPI_send_8(uint8_t wert);
void reg_write (uint8_t addr, uint8_t data);
void ADS1298_Init(void);
void send_sample(void);
uint8_t setChannels (uint8_t cmd);

#endif /* SOURCES_ADS1298_H_ */
