#define F_CPU 4000000UL
#define __DELAY_BACKWARD_COMPATIBLE__

#include <avr/io.h>
#include <util/delay.h>
#include <string.h>
#include <stdlib.h>

//Declaration of global values
#define USART0_BAUD_RATE(BAUD_RATE)     ((float)(64 * F_CPU / (16 * (float)BAUD_RATE)) + 0.5)
#define M1            PIN_PD0   //PC0 -> PD0
#define M2            PIN_PA6
#define M3            PIN_PA5
#define TQ            PIN_PD7
#define Latch         PIN_PD1
#define Enable        PIN_PA4
#define Reset         PIN_PA2
#define CLK           PIN_PA3
#define CWCCW         PIN_PA7
#define maxDepth      25        //cm
#define steps_cm      251       //steps per cm
#define LIMIT_SWITCH  PIN_PD2

#define ADC_MISO_DOUT PIN_PD6   //PD6 -> PC1
#define ADC_MOSI_DIN  PIN_PC1   //PC1 -> PC0
#define ADC_SCLK      PIN_PD0   //PD0 -> PC2
#define ADC_SS_CS     PIN_PC3
#define ADC_NREADY    PIN_PC2   //PC2 -> PD6
#define ADC_RESET     PIN_PD5
#define ADC_DELAY     20

#define WRITE_ADCCON_REG    0b00000010
#define READ_ADCDATA_REG   0b01000100
#define REG_READ      0b00000000
#define CHAN_LDCELL   0b00011111  //AIN2-AINCOM, bipolar encoding, +/- 2.56 V input range
#define CHAN_PROBE    0b01101111  //AIN7-AINCOM, bipolar encoding, +/- 2.56 V input range

int probeLoc = 0;

/* Setup Functions */

void setup(void){
  USART0_init();
  SPI1_init();
  motor_init();
  ADC_init();
}

void SPI1_init(void){
  pinMode(ADC_NREADY, INPUT);
  pinMode(ADC_RESET, OUTPUT);
  pinMode(ADC_MISO_DOUT, INPUT);
  pinMode(ADC_MOSI_DIN, OUTPUT);
  pinMode(ADC_SCLK, OUTPUT);
  pinMode(ADC_SS_CS, OUTPUT);
  digitalWrite(ADC_RESET, LOW);
  _delay_ms(10);
  digitalWrite(ADC_RESET, HIGH);
  digitalWrite(ADC_SS_CS, LOW);
  digitalWrite(ADC_SCLK, HIGH);

  //PORTC.DIRSET = PIN0_bm | PIN2_bm | PIN3_bm;           /* set PC0, PC2, PC3 as output */
  //PORTC.DIRCLR = PIN1_bm;                               /* set PC1 as input */
  /* set this device to master, enable SPI, set prescaling to be 1/16 of MCU clock */
  //SPI1.CTRLA = SPI_MASTER_bm | SPI_ENABLE_bm | SPI_PRESC0_bm;
}

void ADC_init(void) {
  readwrite_spi_byte(0b00000111); //next write to IOCON
  readwrite_spi_byte(0b00000000); //IOCON does not use P1 or P2

  readwrite_spi_byte(0b00000011); //next write to FILTER
  readwrite_spi_byte(0b00001101); //set ADC update time to 9.52 ms

  /* Select Load Cell channel */
  readwrite_spi_byte(WRITE_ADCCON_REG); //next write to ADCCON
  readwrite_spi_byte(CHAN_LDCELL); //select AIN2, bipolar coding, +/- 2.56 V input range

  readwrite_spi_byte(0b00000001); //next write to MODE
  readwrite_spi_byte(0b00010011); //begin continuous conversion
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
  pinMode(LIMIT_SWITCH, INPUT);

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
/*char SPI1_receiveChar(void){
  SPI1.DATA = 0xFF;
  while(!(SPI1.INTFLAGS & SPI_IF_bm));
  return SPI1.DATA;
}

//must wrap with setting CS/SS to low then high again
void SPI1_sendChar(char c){
  SPI1.DATA = c;
  while(!(SPI1.INTFLAGS & SPI_IF_bm));
}*/

unsigned char readwrite_spi_byte(unsigned char out_byte) {
  unsigned char in_byte = 0x00;
  //char test[8];
  for (int i = 0; i < 8; i++) {
    digitalWrite(ADC_SCLK, LOW);
    digitalWrite(ADC_MOSI_DIN, ((out_byte & 0x80) ? 1:0));
    _delay_ms(1);
    digitalWrite(ADC_SCLK, HIGH);
    in_byte <<= 1;
    out_byte <<= 1;
    in_byte |= digitalRead(ADC_MISO_DOUT);
    //test[i] = digitalRead(ADC_MISO_DOUT) + '0';
    _delay_ms(1);
  }
  //USART0_sendLine(test, 8);
  return in_byte;
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
void getAdcVals(unsigned long* vals) { //expects vals to be of size 2
  //Read LoadCell data
  readwrite_spi_byte(WRITE_ADCCON_REG);
  readwrite_spi_byte(CHAN_LDCELL);
  _delay_ms(10);
  readwrite_spi_byte(READ_ADCDATA_REG);
  unsigned long temp;
  temp = readwrite_spi_byte(READ_ADCDATA_REG);
  vals[0] += temp << 16;
  vals[0] += readwrite_spi_byte(READ_ADCDATA_REG) << 8;
  vals[0] += readwrite_spi_byte(REG_READ);
  //Read Probe data
  readwrite_spi_byte(WRITE_ADCCON_REG);
  readwrite_spi_byte(CHAN_LDCELL);
  _delay_ms(10);
  readwrite_spi_byte(READ_ADCDATA_REG);
  temp = readwrite_spi_byte(READ_ADCDATA_REG);
  vals[1] += temp << 16;
  vals[1] += readwrite_spi_byte(READ_ADCDATA_REG) << 8;
  vals[1] += readwrite_spi_byte(REG_READ);
}

void driveProbe(int dist, bool down, double speed, bool measuring) {
  // check that probe is not to go deeper than the pot depth
  // if dist is would go greater than pot depth, set dist to
  // remaining room
  if ((maxDepth - probeLoc) < dist) {
    dist = maxDepth - probeLoc;
  }

  if (measuring && (speed < ADC_DELAY)) {
    speed = ADC_DELAY;
  }

  // if down = true spin CW, else, spin CCW
  if (!down){
    digitalWrite(CWCCW, HIGH);
  } else {
    digitalWrite(CWCCW, LOW);
  }

  //Start power up
  digitalWrite(Enable, HIGH);
  _delay_ms(1);
  digitalWrite(Reset, HIGH);
  _delay_ms(1);
  digitalWrite(M3, HIGH);
  _delay_ms(1);

  //calculate number of steps
  unsigned int numSteps = dist * steps_cm;
  char buf[8] = {};
  size_t bufSize = 8;
  for (unsigned int i = 0; i < numSteps; i++) {   //This could be sped up but I'm about to graduate
    digitalWrite(CLK, HIGH);
    _delay_ms(speed);
    digitalWrite(CLK, LOW);
    //Collect data every mm of travel
    if (measuring && ((i % (steps_cm / 10)) == 0)) {
      _delay_ms(speed - ADC_DELAY);
      unsigned long vals[2] = {0, 0};
      getAdcVals(vals);
      char s1[15] = {0};
      char s2[15] = {0};
      char s3[15] = {0};
      char output[45] = {0};
      itoa(10*i / steps_cm, s1, 10); //converts to mm first
      strcat(output, s1);
      strcat(output, ",");
      ltoa(vals[0], s2, 10);
      strcat(output, s2);
      strcat(output, ",");
      ltoa(vals[1], s3, 10);
      strcat(output, s3);
      USART0_sendLine(output, strlen(output));
    } else {
      _delay_ms(speed);
    }
    bufSize = USART0_receiveLine(buf, 8);
    if ((digitalRead(LIMIT_SWITCH) == HIGH) || (strncmp(buf, "%stop", bufSize) == 0)) {
      break;
    }
  }

  //turn off stepper motor
  _delay_ms(1);
  digitalWrite(Enable, LOW);
  _delay_ms(1);
  digitalWrite(Reset, LOW);
  _delay_ms(1);
  digitalWrite(M3, LOW);
  //update probe location
  if (!down){
    probeLoc -= dist;
  } else {
    probeLoc += dist;
  }
}

void resetProbe() {
  digitalWrite(CWCCW, HIGH);   //go up
  digitalWrite(Enable, HIGH);
  _delay_ms(1);
  digitalWrite(Reset, HIGH);
  _delay_ms(1);
  digitalWrite(M3, HIGH);
  _delay_ms(1);
  while (digitalRead(LIMIT_SWITCH) == LOW) {
    digitalWrite(CLK, HIGH);
    _delay_ms(1);
    digitalWrite(CLK, LOW);
    _delay_ms(1);
  }
  _delay_ms(1);
  digitalWrite(Enable, LOW);
  _delay_ms(1);
  digitalWrite(Reset, LOW);
  _delay_ms(1);
  digitalWrite(M3, LOW);
  probeLoc = 0;
}

void startMeasurement(const char* buf, size_t bufSize) {
  int distLen = bufSize - 7;    //7 is the length of "%start "
  char dist[distLen];
  strncpy(dist, buf+7, distLen);
  resetProbe();
  USART0_sendLine("done", strlen("done"));
  driveProbe(atoi(dist), true, 1, true);
}

void testADC() {
  //Read Probe1 data
  readwrite_spi_byte(WRITE_ADCCON_REG);
  readwrite_spi_byte(CHAN_LDCELL);
  while(digitalRead(ADC_NREADY));
  readwrite_spi_byte(READ_ADCDATA_REG);
  unsigned long num1 = readwrite_spi_byte(READ_ADCDATA_REG);
  num1 <<= 16;
  num1 += readwrite_spi_byte(READ_ADCDATA_REG) << 8;
  num1 += readwrite_spi_byte(REG_READ);
  char s[10];
  ltoa(num1, s, 10);
  USART0_sendLine(s, strlen(s));
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
      driveProbe(1, false, 1, false);
      //USART0_sendLine("raised", strlen("raised"));
    } else if (strncmp(buf, "%lower", bufSize) == 0) {
      driveProbe(1, true, 1, false);
      //USART0_sendLine("lowered", strlen("lowered"));
    } else if (strncmp(buf, "%start", strlen("%start")) == 0) {
      startMeasurement(buf, bufSize);
    } else if (strncmp(buf, "%reset", bufSize) == 0) {
      resetProbe();
    } else if (strncmp(buf, "%adc", bufSize) == 0) {
      testADC();
    } else if (strncmp(buf, "%switch", bufSize) == 0) {
      bool pressed = digitalRead(LIMIT_SWITCH);
      if (pressed) {
        USART0_sendLine("Pressed", strlen("Pressed"));
      } else {
        USART0_sendLine("Not Pressed", strlen("Not Pressed"));
      } 
    } else {
      USART0_sendLine(buf, bufSize);
    }
  }
  return 0;
}
