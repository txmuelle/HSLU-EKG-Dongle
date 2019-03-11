/*
 * shell.h
 *
 *  Created on: 28.11.2018
 *      Author: Müller Urs
 *      based on Demo Projects for FSL_USB_Stack Component mcuoneclipse.com Erich Styger
 */

#ifndef SOURCES_SHELL_H_
#define SOURCES_SHELL_H_

#include <stdio.h>
#include <string.h> /* for strcmp() */
#include <stdbool.h>
#include <stdlib.h>
#include "ADS1298.h"
#include "Application.h"
#include "PE_Types.h"
#include "PE_LDD.h"


typedef struct {
  LDD_TDeviceData *handle; /* LDD device handle */
  volatile uint8_t isSent; /* this will be set to 1 once the block has been sent */
  uint8_t rxChar; /* single character buffer for receiving chars */
  uint8_t (*rxPutFct)(uint8_t); /* callback to put received character into buffer */
} UART_Desc;

/*! \brief Shell driver initialization */
void Shell_Init(void);
//static portTASK_FUNCTION(ShellTask, pvParameters);/*runs the UART*/

//void wait_for_command(void);
//void commandswitch(int command);
//void uart_poll(void);

void USB_SendChr(unsigned char ch);


#endif /* SOURCES_SHELL_H_ */
