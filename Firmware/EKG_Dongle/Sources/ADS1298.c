/*
 * \file ADS1298.c
 * \brief driver for communication to the ADS1298 ECG Chip by Texas Instruments
 *
 * Created on: 02.11.2018
 * \author : Mueller Urs
 *      based on the ECG Project Kommunikation File by Jost H. found on https://www.mikrocontroller.net/articles/EKG_mit_XMC_%C2%B5C
 */


/*includes*/
#include "Platform.h"

#if PL_CONFIG_HAS_ADS1298
#include "ADS1298.h"
#include "ADS_CS.h"
#include "ADS_SM1.h"
#include "ADS_DRDY.h"
#include "shell.h"
#include "GPIO_PDD.h"


/*Variables*/
uint8_t ErrorCode;

/*
** ===================================================================
**     Method      :  SPIWrite (component ADS1298)
**
**     Description :
**         Write a byte to the SPI
**
** ===================================================================
*/
static uint8_t SPIWriteRead(uint8_t val){

  uint8_t ch; /*temp. Variable */

  while(ADS_SM1_GetCharsInTxBuf()!=0) {} 	/* wait until tx is empty */
  while(ADS_SM1_SendChar(val)!=ERR_OK) {} 	/* send character */
  while(ADS_SM1_GetCharsInTxBuf()!=0) {} 	/* wait until data has been sent */
  while(ADS_SM1_GetCharsInRxBuf()==0) {} 	/* wait until we receive data */
  while(ADS_SM1_RecvChar(&ch)!=ERR_OK) {} 	/* get data */

  return ch;
}

uint8_t setChannels (uint8_t cmd){

	reg_write(0x05, cmd); //Channel 1
	reg_write(0x06, cmd); //Channel 2
	reg_write(0x07, cmd); //Channel 3
	reg_write(0x08, cmd); //Channel 4
	reg_write(0x09, cmd); //Channel 5
	reg_write(0x0A, cmd); //Channel 6
	reg_write(0x0B, cmd); //Channel 7
	reg_write(0x0C, cmd); //Channel 8

	if(reg_read(0x05)==cmd){
		return ERR_OK;
	}
	else {
		return ERR_FAILED;
	}
}

void send_sample(void){

		//temporary Variable for received char
		uint8_t Chr;

		//restart (synchronize) conversions
		(void) SPIWriteRead(0x08);

		if((ADS_DRDY_GetVal()==0))	{ //The next falling edge of DRDY indicates that data are ready.


			// send StartBit
			USB_SendChr(0xAB);

			//set CS LOW
			ADS_CS_ClrVal();

			//clear Buffer
			ADS_SM1_ClearRxBuf();

			//send RDATA - Read data by command; supports multiple read back.
			(void) SPIWriteRead(0x12);

			//temporary Variable for count
			uint8_t i = 0;

			while(i != 27)
			{

					Chr = SPIWriteRead(0);

					if( Chr == 0x0A){
						Chr++;
					}
					if( Chr == 0xAB){
						Chr++;
					}

					USB_SendChr(Chr);

				//Zähler hochzählen
				i++;
			}

			//always wait four or more tCLK periods before taking CS high.
			WAIT1_Waitus(1);

			//set CS HIGH
			ADS_CS_SetVal();

			// send line feed as endmark
			USB_SendChr(0x0A);

		}

}

void SPI_send_8(uint8_t wert)
{

	//CS auf LOW
	ADS_CS_ClrVal();

	//Wert über SPI raussenden
	(void) SPIWriteRead(wert);

	//always wait four or more tCLK periods before taking CS high.
	WAIT1_Waitus(1);

	//CS auf HIGH
	ADS_CS_SetVal();

	ADS_SM1_ClearRxBuf();

}



/*
** ===================================================================
**     Method      :  reg_read  (component ADS1298)
**     Description :
**         Read From Register
**     Parameters  :
**         addr             - Register address
**     Returns     :
**         ---             	- value read
** ===================================================================
*/
uint8_t  reg_read(uint8_t addr){
	uint8_t ch;

	//set CS Low : CS must be low for the entire command.
	ADS_CS_ClrVal();

	//Buffer reinigen
	ADS_SM1_ClearRxBuf();

	// Addr über SPI senden : First opcode byte: 001r rrrr, where r rrrr is the starting register address.
	(void) SPIWriteRead(0x20 | addr);

	//Wert über SPI raussenden : Second opcode byte: 000n nnnn, where n nnnn is the number of registers to read – 1.
	(void) SPIWriteRead(0);

	//Wert über SPI lesen : The 17th SCLK rising edge of the operation clocks out the MSB of the first register
	ch = SPIWriteRead(0); /* write dummy */

	//always wait four or more tCLK periods before taking CS high.
	//WAIT1_Waitus(1);



	//set CS high
	ADS_CS_SetVal();


	return ch;
}

/*
** ===================================================================
**     Method      :  reg_write  (component ADS1298)
**     Description : send data to ADS1298 -> Write to Register
**
** 	  Parameters  :
** 			addr	-	Register Address
** 			data 	- 	Data to send
**
** 	  Returns     : Nothing
** ===================================================================
*/
void reg_write (uint8_t addr, uint8_t data){

	//CS auf LOW : CS must be low for the entire command.
	ADS_CS_ClrVal();

	//Wert über SPI raussenden : First opcode byte: 010r rrrr, where r rrrr is the starting register address.
	(void) SPIWriteRead(0x40 | addr);

	//Wert über SPI raussenden : Second opcode byte: 000n nnnn, where n nnnn is the number of registers to write – 1.
	(void) SPIWriteRead(0);

	//Wert über SPI raussenden : the register data (in MSB-first format),
	(void) SPIWriteRead(data);

	//always wait four or more tCLK periods before taking CS high.
	//AIT1_Waitus(1);

	//CS auf HIGH
	ADS_CS_SetVal();

	ADS_SM1_ClearRxBuf();

}

/*-------------------------------------------------------
 * Method      : ADS1298Task (component ADS1298)
 *
 *Description : reads Samples form the ECG Chip and sends over USB to GUI
 *
 * Parameters  :
 * 			ADS1298Task 	- the taks
 * 			pvparameters 	- not used
 *
 * Returns     : Nothing
 * -------------------------------------------------------
 */
static portTASK_FUNCTION(ADS1298Task, pvParameters) {
	(void)pvParameters;

	TickType_t xLastWakeTime = xTaskGetTickCount ( ) ;

	  for(;;) {

		  if(enable_sampling == TRUE)
		  		{


		  			send_sample(); /*calls one sample from ADS198*/


		  		}
		  else{

		  }

	    //FRTOS1_vTaskDelay(100/portTICK_RATE_MS);
	    FRTOS1_vTaskDelayUntil(&xLastWakeTime, 20/portTICK_RATE_MS);
	  }
	}

/*-------------------------------------------------------
 * ADS1298 - initialize the ADS1298 ECG Chip
 *
 * Parameters  : None
 * Returns     : Nothing
 * -------------------------------------------------------
 */
void ADS1298_Init(void){

	// Dummy to set CLKI1 high -> ProcessorExpert Bug
	ADS_SM1_SendChar(0x00);


	/*Startup Code for ADS1298:*/

	  //reg_write(addr, data);

		reg_write(0x01, 0x06);//Config1
		reg_write(0x02, 0x10);//Config2 -> changed from 31 and 32 to 10
		reg_write(0x03, 0xCC);//Config3
		reg_write(0x17, 0x0A);//Config4
		reg_write(0x05, 0x00);//CH1SET
		reg_write(0x06, 0x00);//CH2SET
		reg_write(0x07, 0x00);//CH3SET
		reg_write(0x08, 0x00);//CH4SET
		reg_write(0x09, 0x00);//CH5SET
		reg_write(0x0A, 0x00);//CH6SET
		reg_write(0x0B, 0x00);//CH7SET
		reg_write(0x0C, 0x00);//CH8SET
		reg_write(0x0D, 0x03);//RLD_SENSP
		reg_write(0x0E, 0x03);//RLD_SENSN
		reg_write(0x04, 0xff);//LOFF
		reg_write(0x0F, 0xFF);//LOFF_SENDP
		reg_write(0x10, 0xFF);//LOFF_SENDN
		reg_write(0x11, 0xff);//LOFF_FLIP
		reg_write(0x18, 0x0A);//WCT
		reg_write(0x19, 0xC1);//WCT

		SPI_send_8(0x11);//free run stoppen

		(void) SPIWriteRead(0);

		//Startbefehl senden entweder SPI_send_8(0x08) oder ADS_START_ClrVal();
		SPI_send_8(0x08);

		 /*set Status*/
		  if(reg_read(0)== 0x92) //read ID - Register should be 92
		  {
			  status = 0; //it's the correct Chip
		  }

		  else
		  {
			  status = 3; //wrong Chip
		  }



		  //ADS1298Task starten

		  if (FRTOS1_xTaskCreate(ADS1298Task, (signed portCHAR *)"ADS1298", configMINIMAL_STACK_SIZE, NULL, tskIDLE_PRIORITY+2, NULL) != pdPASS) {
		      for(;;){} /* error */
		    }

}

#endif
