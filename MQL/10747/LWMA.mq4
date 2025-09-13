//+------------------------------------------------------------------+
//|                                         LWMA-Crossover_Signal.mq4 |
//|         Copyright © 2005, Jason Robinson (jnrtrading)            |
//|                   http://www.jnrtading.co.uk                     |
//+------------------------------------------------------------------+

/*
  +-------------------------------------------------------------------+
  | Allows you to enter two lwma periods and it will then show you at |
  | Which point they crossed over. It is more usful on the shorter    |
  | periods that get obscured by the bars / candlesticks and when     |
  | the zoom level is out. Also allows you then to remove the emas    |
  | from the chart. (lwmas are initially set at 5 and 6)              |
  +-------------------------------------------------------------------+
*/   
#property copyright "Copyright © 2005, Jason Robinson (jnrtrading)"
#property link      "http://www.jnrtrading.co.uk"

#property indicator_chart_window
#property indicator_buffers 2
#property indicator_color1 SpringGreen
#property indicator_color2 Red

double CrossUp[];
double CrossDown[];

extern int FasterLWMA = 5;
extern int SlowerLWMA = 6;

extern bool soundAlert = true;
extern string soundFile = "lwma_crossover.wav";
extern bool textAlert = true;

string timeString;
datetime timeStamp;
bool silence = false;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- indicators
   SetIndexStyle(0, DRAW_ARROW, EMPTY);
   SetIndexArrow(0, 233);
   SetIndexBuffer(0, CrossUp);
   SetIndexStyle(1, DRAW_ARROW, EMPTY);
   SetIndexArrow(1, 234);
   SetIndexBuffer(1, CrossDown);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
//---- 

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start() {
   int limit, i, counter;
   double fasterLWMAnow, slowerLWMAnow, fasterLWMAprevious, slowerLWMAprevious, fasterLWMAafter, slowerLWMAafter;
   double Range, AvgRange;
   
	timeStamp = iTime(NULL,0,0);
	timeString = "{ "+TimeYear(timeStamp)+"-"+TimeMonth(timeStamp)+"-"+TimeDay(timeStamp)+" "+TimeHour(timeStamp)+":"+TimeMinute(timeStamp)+":"+TimeSeconds(timeStamp)+" }";
   
    fasterLWMAnow = iMA(NULL, 0, FasterLWMA, 0, MODE_LWMA, PRICE_CLOSE, 1);
    fasterLWMAprevious = iMA(NULL, 0, FasterLWMA, 0, MODE_LWMA, PRICE_CLOSE, 2);
    fasterLWMAafter = iMA(NULL, 0, FasterLWMA, 0, MODE_LWMA, PRICE_CLOSE, 0);

    slowerLWMAnow = iMA(NULL, 0, SlowerLWMA, 0, MODE_LWMA, PRICE_CLOSE, 1);
    slowerLWMAprevious = iMA(NULL, 0, SlowerLWMA, 0, MODE_LWMA, PRICE_CLOSE, 2);
    slowerLWMAafter = iMA(NULL, 0, SlowerLWMA, 0, MODE_LWMA, PRICE_CLOSE, 0);

    if ((fasterLWMAnow > slowerLWMAnow) && (fasterLWMAprevious < slowerLWMAprevious) && (fasterLWMAafter > slowerLWMAafter))
    {
      	if(!silence)
      	{
			if(textAlert)
				Print(timeString+" LWMA Crossing Up: "+DoubleToStr(CrossUp[i], Digits));	
			if(soundAlert)
				PlaySound(soundFile);
				
			silence = true;
		}
   	}
    else if ((fasterLWMAnow < slowerLWMAnow) && (fasterLWMAprevious > slowerLWMAprevious) && (fasterLWMAafter < slowerLWMAafter)) {
      	if(!silence)
      	{
			if(textAlert)
				Print(timeString+" LWMA Crossing Down: "+DoubleToStr(CrossDown[i], Digits));	
			if(soundAlert)
				PlaySound(soundFile);
				
			silence = true;
      	}
    }
    else
      silence = false;
   
   int counted_bars=IndicatorCounted();
//---- check for possible errors
   if(counted_bars<0) return(-1);
//---- last counted bar will be recounted
   if(counted_bars>0) counted_bars--;

   limit=Bars-counted_bars;
   
   for(i = 0; i <= limit; i++) {
   

      counter=i;
      Range=0;
      AvgRange=0;
      for (counter=i ;counter<=i+9;counter++) {
         AvgRange=AvgRange+MathAbs(High[counter]-Low[counter]);
      }
      Range=AvgRange/10;
       
      fasterLWMAnow = iMA(NULL, 0, FasterLWMA, 0, MODE_LWMA, PRICE_CLOSE, i);
      fasterLWMAprevious = iMA(NULL, 0, FasterLWMA, 0, MODE_LWMA, PRICE_CLOSE, i+1);
      fasterLWMAafter = iMA(NULL, 0, FasterLWMA, 0, MODE_LWMA, PRICE_CLOSE, i-1);

      slowerLWMAnow = iMA(NULL, 0, SlowerLWMA, 0, MODE_LWMA, PRICE_CLOSE, i);
      slowerLWMAprevious = iMA(NULL, 0, SlowerLWMA, 0, MODE_LWMA, PRICE_CLOSE, i+1);
      slowerLWMAafter = iMA(NULL, 0, SlowerLWMA, 0, MODE_LWMA, PRICE_CLOSE, i-1);
      
      if ((fasterLWMAnow > slowerLWMAnow) && (fasterLWMAprevious < slowerLWMAprevious) && (fasterLWMAafter > slowerLWMAafter)) {
      	CrossUp[i] = Low[i] - Range*0.5;
      }
      else if ((fasterLWMAnow < slowerLWMAnow) && (fasterLWMAprevious > slowerLWMAprevious) && (fasterLWMAafter < slowerLWMAafter)) {
      	CrossDown[i] = High[i] + Range*0.5;
      }
   }
   return(0);
}

