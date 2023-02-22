#define F_CPU 4000000UL
#include <avr/io.h>
#include <util/delay.h>
#include <string.h>

#define USART0_BAUD_RATE(BAUD_RATE)     ((float)(64 * F_CPU / (16 * (float)BAUD_RATE)) + 0.5)
 
//Declaration of our functions
void USART0_init(void);
char USART0_receiveChar(void);
void USART0_sendChar(char c);
void USART0_sendLine(const char* str);
size_t USART0_receiveLine(char* buf, size_t size); 
 
int main(void){
  USART0_init();                 //Call the USART initialization code
  while(1){
    char buf[50] = {};
    size_t bufSize = USART0_receiveLine(buf, 50);
    if (strncmp(buf, "%handshake", bufSize) == 0) {
      USART0_sendLine("connected", strlen("connected"));     //Pass the string to the USART_putstring function and sends it over the serial
    } else {
      USART0_sendLine(buf, bufSize);
    }
  }
  return 0;
}
 
void USART0_init(void){
  PORTA.DIRSET = PIN0_bm;                               /* set pin 0 of PORT A (TXd) as output*/
  PORTA.DIRCLR = PIN1_bm;                               /* set pin 1 of PORT A (RXd) as input*/
  USART0.BAUD = (uint16_t)(USART0_BAUD_RATE(9600));     /* set the baud rate*/
  USART0.CTRLC = USART_CHSIZE0_bm | USART_CHSIZE1_bm;  /* set the data format to 8-bit*/
  USART0.CTRLB |= USART_TXEN_bm | USART_RXEN_bm;        /* enable transmitter, receiver, and */
}
 
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

size_t USART0_receiveLine(char* buf, size_t size){
  for (size_t i = 0; i < size; i++) {
    char c = USART0_receiveChar();
    if (c == '\n'){
      return i;
    }
    memcpy(buf+i, &c, 1);
  }
  return size;
}
