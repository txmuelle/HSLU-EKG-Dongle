/*
 * Application.h
 *
 *  Created on: 02.12.2018
 *      Author: Müller Urs
 *      based on Demo Projects for FSL_USB_Stack Component mcuoneclipse.com by Erich Styger
 */

#ifndef SOURCES_APPLICATION_H_
#define SOURCES_APPLICATION_H_

#include <stdbool.h>


int status; // 0 = Disconnected, 1= Connected+Sampling stopped, 2=Sampling Started, 3= Error
bool enable_sampling;

void APP_Run(void);
void APP_Init(void);

#endif /* SOURCES_APPLICATION_H_ */
