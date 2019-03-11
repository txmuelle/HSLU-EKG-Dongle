/*
 * Application.c
 *
 *  Created on: 02.12.2018
 *      Author: Müller Urs
 *      based on Demo Projects for FSL_USB_Stack Component mcuoneclipse.com by Erich Styger
 */

#include "Application.h"
#include "Platform.h"
#include "shell.h"
#include "FRTOS1.h"
#include "ADS1298.h"
#include "Blue_LED.h"

static void led_blinky_task(void *param) {
  (void)param;
  int i, j = 0;

  for(;;) {

	  //Power on Led leuchtet 2s
	  if( i < 1){
		  Green_LED_Neg();
		  i++;
		  vTaskDelay(pdMS_TO_TICKS(2000));
	  }

	  //disconnected
	  if(status == 0){
			  Green_LED_Off();
			  vTaskDelay(pdMS_TO_TICKS(2000));
	  }

	  //Connected, sampling stopped -> Led blinkt
	  if(status == 1){
			  Green_LED_Neg();
			  j=0;
			  vTaskDelay(pdMS_TO_TICKS(500));
	  }

	  //sampling
	  if(status == 2){
		  if(j == 0){
			  Green_LED_Off();
			  j++;
			  vTaskDelay(pdMS_TO_TICKS(2000));
		  }

		  else{
			  vTaskDelay(pdMS_TO_TICKS(2000));
		  }
	  }

	  //Error
	  if(status == 3){
		    Green_LED_Neg();
		    vTaskDelay(pdMS_TO_TICKS(500));
	  }


    //Green_LED_Neg();
    //vTaskDelay(pdMS_TO_TICKS(2000));
  } /* for */
}

/* APP_Run - Main Application
 *
 * Programmablauf:
 *
 *	  1. SPI Daten empfangen      				- mittlere Prio
 *
 *	  2. Daten an PC weiterleiten 				- mittlere Prio
 *
 *	  3. Befehle von PC empfangen "ShellTask" 	- hohe Prio
 *
 *	  4. Herzpuls LED blinken     				- niedrige Prio
 *
 *	  5. Software Freischalten  				- niedrige Prio
 *
 */
void APP_Run(void) {


  FRTOS1_vTaskStartScheduler();



}

void APP_Init(void){

	  /* ToDo init of EKG Dongle here:

	    * Init:
	    * 	io
	    * 	uart
	    * 	ads1298
	    * 	...
	    * */


#if PL_CONFIG_HAS_ADS1298
	 ADS1298_Init();
#endif
#if PL_CONFIG_HAS_SHELL
	 Shell_Init();
#endif

#if PL_CONFIG_HAS_LEDBLINKY
  if (xTaskCreate(led_blinky_task, "Led", configMINIMAL_STACK_SIZE, NULL, tskIDLE_PRIORITY, NULL) != pdPASS) {
    for(;;){} /* error! probably out of memory */
  }
#endif

}

