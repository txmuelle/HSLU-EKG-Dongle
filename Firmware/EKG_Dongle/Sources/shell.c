/*
 * \file shell.c
 * \brief This is the implementation module of the Shell console
 *
 *  This interface file is used for a console and terminal.
 *  Created on: 28.11.2018
 *      Author: Müller Urs
 *      based on Demo Projects for FSL_USB_Stack Component mcuoneclipse.com Erich Styger
 */



//#include "UART_PDD.h"
#include "shell.h"
#include "Green_LED.h"
#include "Platform.h"
#include "FRTOS1.h"
#include <string.h>
#include "Blue_LED.h"
#if PL_CONFIG_HAS_USBSerial
#include "RxBuf.h"
#include "AS1.h"
#endif
#if PL_CONFIG_HAS_USBCDC
#include "CLS1.h"
#include "USB1.h"
#endif


/* ******************************************************************
 * UART Standard I/O
 * ******************************************************************/


#if PL_CONFIG_HAS_SHELL

#if PL_CONFIG_HAS_USBSerial
static UART_Desc deviceData;
#endif

#if PL_CONFIG_HAS_USBSerial
/*
 * SendChar - Sends one char thru UART to the PC
 *
 * Parameters  :
 * 		char 	- 	the char to send
 * 		*desc	-   Pointer to handler struct
 *
 * Returns     : Nothing
 */
static void USB_SendChar(unsigned char ch, UART_Desc *desc) {

  desc->isSent = FALSE;  /* this will be set to 1 once the block has been sent */
  while(AS1_SendBlock(desc->handle, (LDD_TData*)&ch, 1)!=ERR_OK) {} /* Send char */
  while(!desc->isSent) {} /* wait until we get the green flag from the TX interrupt */

}
#endif
/*
 * SendString - Sends one string thru UART to the PC
 *
 * Parameters  :
 * 		*str 	- 	the String to send
 *
 *
 * Returns     : Nothing
 */
static void USB_SendString(const unsigned char *str) {
  while(*str!='\0') {
#if PL_CONFIG_HAS_USBSerial
    USB_SendChar(*str++, &deviceData);
#endif
#if PL_CONFIG_HAS_USBCDC
    CDC1_SendChar(*str++);
#endif
  }
}


void USB_SendChr(unsigned char ch){
#if PL_CONFIG_HAS_USBSerial
	USB_SendChar(ch, &deviceData);
#endif
#if PL_CONFIG_HAS_USBCDC
	CDC1_SendChar(ch);
#endif
}

void wait_for_command(void){

	//Warte auf einen Befehl
	//while((USIC0_CH0->TRBSR & 0x00000008) != 0)
	//while(cdc2_GetCharsInRxBuf()<8)
//	{
//
//	}

	//commandswitch((USIC0_CH0->OUTR & 0x0000FFFF));
}

void uart_poll(void){

	//if((USIC0_CH0->TRBSR & 0x00000008) == 0)
	//{
		//commandswitch((USIC0_CH0->OUTR & 0x0000FFFF));
	//}
}




void commandswitch(char command){

	switch(command)
	{
		/*Connect Cmd*/
		case 1:
			//USB_SendChar(6, &deviceData); //send ACK to GUI
			USB_SendChr(6); //send ACK to GUI
			enable_sampling = FALSE;
			status = 1;
			break;

		/*Disconnect Cmd*/
		case 2:
			USB_SendChr(6); //send ACK to GUI
			status = 0;
			enable_sampling = FALSE;
			break;

		/*Read ID Cmd*/
		case 4:
			USB_SendChr(reg_read(0));
			break;

		/*Start Sampling Cmd*/
		case 5:
			USB_SendChr(6); //send ACK to GUI
			SPI_send_8(0x08); //Start measurement
			enable_sampling = TRUE;
			status = 2;
			break;

		/*Stop Sampling Cmd*/
		case 6:
			USB_SendChr(6); //send ACK to GUI
			enable_sampling = FALSE;
			SPI_send_8(0x0A); //Stop measurement
			status = 1;
			break;

		/*Green LED on */
		case 7:
			//USB_SendChar(6, &deviceData); //send ACK to GUI
			Green_LED_On();
			break;

		/*Green LED off */
		case 8:
			//USB_SendChar(6, &deviceData); //send ACK to GUI
			Green_LED_Off();
			break;

		/*Channel with Testsignal*/
		case 9:
			if(setChannels(0x05)==0x00) {
				USB_SendString("channels set to test signal");
			}
			else {
				USB_SendString("Error, test signal not started \n");
				setChannels(0x00);
			}
			break;

		/*Channel normal*/
		case 10:
			if(setChannels(0x00)==0x00){
				USB_SendString("channels set to normal");
		}
		else {
			USB_SendString("Error, not started \n");
			setChannels(0x00);
		}
			break;

		/*Channel shorted*/
		case 11:
			if(setChannels(0x01)==0x00){
				USB_SendString("channels set to shorted");
		}
		else {
			USB_SendString("Error, shorted not started \n");
			setChannels(0x00);
		}
		break;
		/*Channel power down*/
		case 12:
			if(setChannels(0x81)==0x00){
				USB_SendString("channels set to power down");
		}
		else {
			USB_SendString("Error, pwdwn not started \n");
			setChannels(0x00);
		}
			break;

		/*Channel special*/
		case 13:
			if(setChannels(0x10)==0x00){
				USB_SendString("channels set to special");

				//LOFF
				reg_write(0x04, 0xff);

				//LOFF_SENDP
				reg_write(0x0F, 0xFF);

				//LOFF_SENDN
				reg_write(0x10, 0xFF);

				//LOFF_FLIP
				reg_write(0x11, 0xff);
		}
		else {

			USB_SendString("Error, special not started \n");
			setChannels(0x00);
		}
		break;


		default:
			status = 3; //ERROR
			break;
	}

}





/*
 * ShellTask - Daten über UART vom Rechner empfangen und senden
 *
 * Parameters  :
 * 		ShellTask 	- 	TaskName
 * 		pvParameter -   not used
 *
 * Returns     : Nothing
 */

static portTASK_FUNCTION(ShellTask, pvParameters) {
	(void)pvParameters;

#if PL_CONFIG_HAS_USBSerial
	  Blue_LED_Neg();

	  if(status != 1)USB_SendString((unsigned char*)"Shell task started!\r\n", &deviceData);

	  for(;;) {
	    if (RxBuf_NofElements()!=0) {
	    	//if(status != 1) USB_SendString((unsigned char*)"echo: ", &deviceData);
	      while (RxBuf_NofElements()!=0) {
	        unsigned char ch;

	        (void)RxBuf_Get(&ch);
	       // if(status != 1) USB_SendChar(ch, &deviceData);
	        commandswitch(ch);
	      }
	      USB_SendString((unsigned char*)"\r\n", &deviceData);
	    }

	    FRTOS1_vTaskDelay(100/portTICK_RATE_MS);
	  }
#endif

#if PL_CONFIG_HAS_USBCDC
	  static unsigned char cmd_buf[32];
	    uint8_t buf[USB1_DATA_BUFF_SIZE];
	    bool startup = TRUE;

	    cmd_buf[0]='\0';
		Blue_LED_Neg();

		if(status != 1)USB_SendString((unsigned char*)"Shell task started!\r\n");

	    for(;;) {

	    //initialisiereung
	      if (CDC1_App_Task(buf, sizeof(buf))!=ERR_OK) {
	        /* Call the USB application task, wait until enumeration has finished */
	        while(CDC1_App_Task(buf, sizeof(buf))!=ERR_OK) {
	          Blue_LED_Neg(); /* flash LED fast to indicate that we are not communicating */
	          FRTOS1_vTaskDelay(20/portTICK_RATE_MS);
	        }
	      }
	      //Ausführen
	        if(CDC1_GetCharsInRxBuf()!=0){

	        while(CDC1_GetCharsInRxBuf()!=0){

	        	unsigned char ch;
	        	//CLS1_ReadChar(&ch);
	        	CDC1_GetChar(&ch);
	        	commandswitch(ch);
	        	}
	        USB_SendString((unsigned char*)"\r\n");
	        }
	      Blue_LED_Neg();
	      //(void)CLS1_ReadAndParseLine(cmd_buf, sizeof(cmd_buf), CLS1_GetStdio(), ParseCommand);


	      FRTOS1_vTaskDelay(100/portTICK_RATE_MS);
	    }
#endif

	}

/*
 * ShellInite - Initializes the ShellTask and the RxBuffer for UART Communication
 *
 * Parameters  : None
 *
 * Returns     : Nothing
 */
void Shell_Init(void) {

#if PL_CONFIG_HAS_USBSerial
  /* initialize struct fields */
  deviceData.handle = AS1_Init(&deviceData);
  deviceData.isSent = FALSE;
  deviceData.rxChar = '\0';
  deviceData.rxPutFct = RxBuf_Put;
  /* set up to receive RX into input buffer */
  RxBuf_Init(); /* initialize RX buffer */
  /* Set up ReceiveBlock() with a single byte buffer. We will be called in OnBlockReceived() event. */
  while(AS1_ReceiveBlock(deviceData.handle, (LDD_TData *)&deviceData.rxChar, sizeof(deviceData.rxChar))!=ERR_OK) {} /* initial kick off for receiving data */
#endif

#if PL_CONFIG_HAS_USBCDC
  CLS1_Init(); /* create mutex/semaphore */
#endif

  if (FRTOS1_xTaskCreate(ShellTask, (signed portCHAR *)"Shell", configMINIMAL_STACK_SIZE+50, NULL, tskIDLE_PRIORITY+1, NULL) != pdPASS) {
      for(;;){} /* error */
    }
}

#endif
