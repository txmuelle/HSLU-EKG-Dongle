/*
 * \file Platform.c
 * \brief project configuration file.
 *  Created on: 11.12.2018
 * \author : Mueller Urs
 */

/*includes*/
#include "Platform.h"

#if PL_CONFIG_HAS_EVENTS
#include "Events.h"
#endif
#if PL_CONFIG_HAS_GREEN_LED
#include "Green_LED.h"
#endif
#if PL_CONFIG_HAS_BLUE_LED
#include "Blue_LED.h"
#endif
#if PL_CONFIG_HAS_ADS1298
#include "ADS1298.h"
#include "ADS_CS.h"
#endif
#if PL_CONFIG_HAS_SHELL
#include "Shell.h"
#endif

/*methods*/
void PL_Init(void) {

	/* ToDo all inits here */
}

void PL_Deinit(void){
	/* ToDo all Deinits here */
}
