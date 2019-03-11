/*
 * \file Platform.h
 * \brief Common platform configuration interface.
 * \Created on: 11.12.2018
 * \author : Mueller Urs
 */

#ifndef SOURCES_PLATFORM_H_
#define SOURCES_PLATFORM_H_

/* include shared header files */
#include "PE_Types.h" /* common Processor Expert types: bool, NULL, ... */

/* driver configuration: first entry (0 or 1) disables or enables the driver. Using the _DISABLED define the local configuration can disable it too */
/* general features */
#define PL_CONFIG_HAS_GREEN_LED         (1) /* LED driver */
#define PL_CONFIG_HAS_BLUE_LED          (1) /* LED driver */
#define PL_CONFIG_HAS_EVENTS            (1) /* event driver */
#define PL_CONFIG_HAS_RTOS              (1) /* RTOS support */
#define PL_CONFIG_HAS_ADS1298			(1) /* ECG Chip Driver: ADS1298 by Texas Instruments*/
#define PL_CONFIG_HAS_USBCDC            (1)
#define PL_CONFIG_HAS_USBSerial			(0)
#define PL_CONFIG_HAS_SHELL				(1)
#define PL_CONFIG_HAS_LEDBLINKY			(1) /*Makes a BlinkyTask only for DeBug purpose*/


/*!
 * \brief Driver de-initialization
 */
void PL_Deinit(void);

/*!
 * \brief  driver initialization
 */
void PL_Init(void);


#endif /* SOURCES_PLATFORM_H_ */
