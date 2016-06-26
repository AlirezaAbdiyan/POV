#include <Arduino.h>
#include <SPI.h>
#include <FastLED.h>
#include <SdFat.h>
SdFat sd;

const int chipSelect = SS;
SdFile file;

typedef uint16_t line_t;
//const int IR_PIN = 23;  //Any pin teensy 3.1
#define HalSensor 22    //Any pin teensy 3.1

#define NUM_LEDS  144 //number of leds in strip length on one side
#define DATA_PIN  7   //11 ,  7 = second hardware spi data
#define CLOCK_PIN 14  //13 , 14 = second hardware spi clock
CRGB leds[NUM_LEDS];

boolean autoCycle = true; // Set to true to cycle images by default
#define CYCLE_TIME 15     // Time, in seconds, between auto-cycle images

char str[15],str_num[4],file_name[8];
volatile uint8_t LEDs;
volatile boolean OpenFile=true;
volatile boolean file_ok=false;
volatile uint32_t t_start_round=0,t_stop_round=0;

void setup();
void showTestRGB(void);
void nextImage(void);
void loop();
bool imageInit_MMC(int image_number);
void read_line(line_t Line);
void Rotate_1_round(void);

// -------------------------------------------------------------------------
void setup() 
{
  pinMode(SS,OUTPUT);
  Serial.begin(9600);
  delay(5000);
  Serial.println("\nInitializing SD card...");
  
  // change to SPI_FULL_SPEED for more performance.
  if (!sd.begin(chipSelect, SPI_FULL_SPEED)) //SPI_HALF_SPEED SPI_FULL_SPEED
  {
    sd.initErrorHalt();
    Serial.println("initialization failed!");
    return;
  }
  Serial.println("initialization done");

  digitalWrite(SS,HIGH);
  FastLED.addLeds<APA102,DATA_PIN,CLOCK_PIN,RGB,DATA_RATE_MHZ(20)>(leds,NUM_LEDS);
  FastLED.setBrightness(255);
  FastLED.clear();
  FastLED.show();
  
  showTestRGB();
  imageInit_MMC(0+1);

  pinMode(HalSensor, INPUT); // sets the digital pin as input
  attachInterrupt(digitalPinToInterrupt(HalSensor), Rotate_1_round, FALLING);
}

void showTestRGB(void)
{
  for(int i=0;i<NUM_LEDS;i++)
  {
    leds[i] = CRGB(255,0,0);  
  }
  FastLED.show();  
  delay(1000);     
  
  for(int i=0;i<NUM_LEDS;i++)
  {
    leds[i] = CRGB(0,255,0);  
  }
  FastLED.show();  
  delay(1000);     

  for(int i=0;i<NUM_LEDS;i++)
  {
    leds[i] = CRGB(0,0,255);  
  }
  FastLED.show();  
  delay(1000);     
    
  FastLED.clear();                               
  FastLED.show();  
}

// GLOBAL STATE STUFF ------------------------------------------------------

uint32_t lastImageTime = 0L, // Time of last image change
         lastLineTime  = 0L;
uint8_t  imageNumber   = 0,  // Current image being displayed
         imagePixels[144*3]; // -> pixel data in PROGMEM
line_t   imageLines,         // Number of lines in active image
         imageLine;          // Current line number in image

volatile bool Start_new_round=false;
volatile uint32_t lineInterval;
volatile uint32_t lineInterval_temp;

void Rotate_1_round(void)
{
  cli();
  t_stop_round=micros();
  lineInterval_temp=(t_stop_round-t_start_round)/(imageLines);
  lineInterval=(lineInterval+lineInterval_temp)/2;  
  t_start_round=t_stop_round;
  Start_new_round=true;
  sei();
}

bool imageInit_MMC(int image_number)
{
  imageLine= 0;
  bool ret;   
  OpenFile=true;
  itoa(image_number,str_num,10);
  strcpy(str,"Conf");
  strcat(str,str_num);
  strcat(str,".bin");
  
  strcpy(file_name,str_num);
  strcat(file_name,".bin");
  
  Serial.println(str);
  Serial.println(file_name);
  if (file.open(str))
  {
    if (file.available())
    {
      file.read(imagePixels,3);  
      imageLines = imagePixels[0];
      imageLines*=256;
      imageLines+= imagePixels[1];
      Serial.print("imageLines=");
      Serial.println(imageLines);
      LEDs = imagePixels[2];
      Serial.print("LEDs=");
      Serial.println(LEDs);
      FastLED.clear(); // Make sure strip is clear
      FastLED.show();  // before measuring battery      
      ret=true;
    }
    else
    {
      ret=false;
    }
    file.close();
  }
  else
  {
    ret=false;
  }
  lastImageTime = millis(); // Save time of image init for next auto-cycle
  return ret;
}

void read_line(line_t Line)
{
  if(Line==0)
  {
    if(OpenFile==true)
    {
      if(file.open(file_name,O_READ))
      {
        file_ok=true;
        OpenFile=false;
      }
      else
      {
        file_ok=false;
        OpenFile=true;
      }
    }
    else
    {
      if(file.seekSet(0)==false)
      {        
        file.close();
        if(file.open(file_name,O_READ))
        {
          file_ok=true;
          OpenFile=false;
        }
        else
        {
          file_ok=false;
          OpenFile=true;
        }
      }
    }
  }
  if (file_ok)
  {
    file.read(imagePixels,LEDs*3);
    for(int i = 0; i < LEDs*3; i+=3)
    {
      leds[i/3] = CRGB(imagePixels[i],imagePixels[i+1],imagePixels[i+2]);   
    }
  }
  else
  {
    //Reset micro?!
  } 
}

void nextImage(void) 
{
  FastLED.clear();
  FastLED.show();
  file.close();
  imageNumber++;
  if(!imageInit_MMC(imageNumber+1))
  {
    imageNumber=0; 
    imageInit_MMC(imageNumber+1);
  }  
}

// MAIN LOOP ---------------------------------------------------------------
volatile uint32_t t;
void loop() 
{  
  if(autoCycle) 
  {
    t = millis(); // Current time, milliseconds       
    if((t - lastImageTime) >= (CYCLE_TIME * 1000L)) nextImage();
    // CPU clocks vary slightly; multiple poi won't stay in perfect sync.
    // Keep this in mind when using auto-cycle mode, you may want to cull
    // the image selection to avoid unintentional regrettable combinations.
  }
  read_line(imageLine); 
  if(++imageLine >= imageLines) 
  {
    imageLine = 0; // Next scanline, wrap around    
    while(!Start_new_round);        
  }
  if(!Start_new_round)
  {    
    FastLED.show(); // Refresh LEDs        
    while(((t =micros()) - lastLineTime) < lineInterval) if(Start_new_round) break;
  }
  else
  {
    Start_new_round=false;
  }  
  lastLineTime = micros();
}//void loop() 


