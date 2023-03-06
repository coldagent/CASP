#include <avr/io.h>
#include <avr/interrupt.h>
#include <util/delay.h>
#include <string.h>
#include <stdlib.h>

//Declaration of global values
#define F_CPU 4000000UL
#define USART0_BAUD_RATE(BAUD_RATE)     ((float)(64 * F_CPU / (16 * (float)BAUD_RATE)) + 0.5)
#define M1 PIN_PD0              //PC0 -> PD0
#define M2 PIN_PA6
#define M3 PIN_PA5
#define TQ PIN_PD7
#define Latch PIN_PD1
#define Enable PIN_PA4
#define Reset PIN_PA2
#define CLK PIN_PA3
#define CWCCW PIN_PA7
#define maxDepth 10
#define steps_cm 251

#define ADC_MISO_DOUT PIN_PC1   //PD6 -> PC1
#define ADC_MOSI_DIN PIN_PC0    //PC1 -> PC0
#define ADC_SCLK PIN_PC2        //PD0 -> PC2
#define ADC_SS_CS PIN_PC3
#define ADC_READY PIN_PD6       //PC2 -> PD6
#define ADC_RESET PIN_PD5

bool starting_position = false;
int probeLoc = 0;

/* Setup Functions */

void setup(void){
  USART0_init();
  SPI1_init();
  motor_init();
}

void SPI1_init(void){
  pinMode(ADC_READY, INPUT);
  pinMode(ADC_RESET, OUTPUT);
  digitalWrite(ADC_RESET, LOW);
  digitalWrite(ADC_SS_CS, HIGH);
  PORTC.DIRSET = PIN0_bm | PIN2_bm | PIN3_bm;           /* set PC0, PC2, PC3 as output */
  PORTC.DIRCLR = PIN1_bm;                               /* set PC1 as input */
  /* set this device to master, enable SPI, set prescaling to be 1/16 of MCU clock */
  SPI1.CTRLA = SPI_MASTER_bm | SPI_ENABLE_bm | SPI_PRESC0_bm;
}

void USART0_init(void){
  PORTA.DIRSET = PIN0_bm;                               /* set pin 0 of PORT A (TXd) as output*/
  PORTA.DIRCLR = PIN1_bm;                               /* set pin 1 of PORT A (RXd) as input*/
  USART0.BAUD = (uint16_t)(USART0_BAUD_RATE(9600));     /* set the baud rate*/
  USART0.CTRLC = USART_CHSIZE0_bm | USART_CHSIZE1_bm;   /* set the data format to 8-bit*/
  USART0.CTRLB |= USART_TXEN_bm | USART_RXEN_bm;        /* enable transmitter, receiver, and */
}

void motor_init(void) {
  pinMode(M1, OUTPUT);
  pinMode(M2, OUTPUT);
  pinMode(M3, OUTPUT);
  pinMode(TQ, OUTPUT);
  pinMode(Latch, OUTPUT);
  pinMode(Enable, OUTPUT);
  pinMode(Reset, OUTPUT);
  pinMode(CLK, OUTPUT);
  pinMode(CWCCW, OUTPUT);

  // set stepper controls
  // M1: L, M2: L, M3: H = Full steps
  digitalWrite(M1, LOW);
  digitalWrite(M2, LOW);
  digitalWrite(M3, LOW);
  digitalWrite(TQ, HIGH); // low torque setting
  digitalWrite(Latch, LOW);
  digitalWrite(Enable, LOW);
  digitalWrite(Reset, LOW);
}

/* SPI Functions */
// must wrap with check if ~RDY then setting CS/SS to low then high again
char SPI1_receiveChar(void){
  SPI1.DATA = 0xFF;
  while(!(SPI1.INTFLAGS & SPI_IF_bm));
  return SPI1.DATA;
}

//must wrap with setting CS/SS to low then high again
void SPI1_sendChar(char c){
  SPI1.DATA = c;
  while(!(SPI1.INTFLAGS & SPI_IF_bm));  
}
 
/* USART Functions */

char USART0_receiveChar(void){
  while(!(USART0.STATUS & USART_RXCIF_bm)); // while receive complete interrupt has not fired
  return USART0.RXDATAL;
}
 
void USART0_sendChar(char c){
  while(!(USART0.STATUS & USART_DREIF_bm));
  USART0.TXDATAL = c;
}
 
void USART0_sendLine(const char* str, size_t size){
  for(size_t i = 0; i < size; i++){        
    USART0_sendChar(str[i]);  
  }
  USART0_sendChar('\n');
}

size_t USART0_receiveLine(char* buf, size_t buf_size){
  // Receives a line into the buffer and returns the received line size
  for (size_t i = 0; i < buf_size; i++) {
    char c = USART0_receiveChar();
    if (c == '\n'){
      return i;
    }
    memcpy(buf+i, &c, 1);
  }
  return buf_size;
}

/* Peripherals Functions */

void driveProbe(double probeLoc, int dist, bool down, double speed, bool measuring) {
  // check that probe is not to go deeper than the pot depth
  // if dist is would go greater than pot depth, set dist to
  // remaining room
  if ((maxDepth - probeLoc) < dist) {
    dist = maxDepth - probeLoc;
  }

  // if down = true spin CW, else, spin CCW
  if (!down){
    digitalWrite(CWCCW, HIGH);
  } else {
    digitalWrite(CWCCW, LOW);
  }

  //Start power up
  digitalWrite(Enable, HIGH);
  _delay_ms(1);   // wait 10 miliseconds
  digitalWrite(Reset, HIGH);
  _delay_ms(1);   // wait 10 miliseconds
  digitalWrite(M3, HIGH);
  _delay_ms(1);   // wait 10 miliseconds


  //calculate number of steps
  int numSteps = dist * steps_cm;
  for (int i = 0; i < numSteps; i++) {
    digitalWrite(CLK, HIGH);
    _delay_ms(speed);
    digitalWrite(CLK, LOW);
    _delay_ms(speed);
    //Collect data every mm of travel
    if (measuring && (i % (steps_cm / 10)) == 0) {
      // call data collection functions
    }
  }

  //turn off stepper motor
  _delay_ms(1);   // wait 10 miliseconds
  digitalWrite(Enable, LOW);
  _delay_ms(1);   // wait 10 miliseconds
  digitalWrite(Reset, LOW);
  _delay_ms(1);   // wait 10 miliseconds
  digitalWrite(M3, LOW);
  //update probe location
  if (!down){
    probeLoc -= dist;
  } else {
    probeLoc += dist;
  }
}

void resetProbe() {

}

void startMeasurement(const char* buf, size_t bufSize) {
  int distLen = bufSize - strlen("%start ");
  char dist[distLen] = {};
  for (int i = 0; i < distLen; i++) {
    dist[i] = buf[i+distLen];
  }
  resetProbe();
  driveProbe(probeLoc, atoi(dist), true, 1, true);
}

/* Main code */

int main(void){
  setup();                 //Initializes everything
  while(1){
    char buf[50] = {};
    size_t bufSize = USART0_receiveLine(buf, 50);
    if (strncmp(buf, "%handshake", bufSize) == 0) {
      USART0_sendLine("connected", strlen("connected"));     //Pass the string to the USART_putstring function and sends it over the serial
    } else if (strncmp(buf, "%raise", bufSize) == 0) {
      driveProbe(probeLoc, 1, false, 1, false);
      USART0_sendLine("raised", strlen("raised"));
    } else if (strncmp(buf, "%lower", bufSize) == 0) {
      driveProbe(probeLoc, 1, true, 1, false);
      USART0_sendLine("lowered", strlen("lowered"));
    } else if (strncmp(buf, "%start", strlen("%start")) == 0) {
      startMeasurement(buf, bufSize);
    } else {
      USART0_sendLine(buf, bufSize);
    }
  }
  return 0;
}
