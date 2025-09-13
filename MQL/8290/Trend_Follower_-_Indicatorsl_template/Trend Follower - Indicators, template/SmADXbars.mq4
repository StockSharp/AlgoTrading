//+------------------------------------------------------------------+
//|                                                    ADX BARS.mq4  |
//|                                            Perky Aint no turkey  |
//|                                                                  |
//+------------------------------------------------------------------+
#property  copyright "Perky"
#property  link      "Perky_z@yahoo.com"

//---- indicator settings
#property  indicator_separate_window
#property  indicator_buffers 2
#property  indicator_color1  Blue
#property  indicator_color2  Red
//---- indicator parameters
extern int ADXPeriod = 14;
extern int ADXBARSPeriod = 5;
//---- indicator buffers
double ind_buffer1[];
double ind_buffer2[];
double HighBarBuffer[];
double LowBarBuffer[];
double b4plusdi, b4minusdi, nowplusdi, nowminusdi;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//----additional buffers are used for counting.
   IndicatorBuffers(4);
//---- indicator buffers mapping
   if(!SetIndexBuffer(0,ind_buffer1) && !SetIndexBuffer(1,ind_buffer2) && 
      !SetIndexBuffer(2,HighBarBuffer) && !SetIndexBuffer(3,LowBarBuffer))
       Print("cannot set indicator buffers!");
//---- drawing settings
   SetIndexStyle(0, DRAW_LINE);
   SetIndexStyle(1, DRAW_LINE);
//---- 
   IndicatorDigits(Digits + 1);  
//---- name for DataWindow and indicator subwindow label
   IndicatorShortName("ADXBars(" + ADXPeriod + ", " + ADXBARSPeriod + ")");
   SetIndexLabel(0, "ADXBars");
   SetIndexLabel(1, "Signal");           
//---- initialization done
   return(0);
  }
//+------------------------------------------------------------------+
//|  ADX BARS                                                        |
//+------------------------------------------------------------------+
int start()
  {
    double ArOsc = 0;
    int    ArPer, limit, i; 
    int    counted_bars = IndicatorCounted();
//---- check for possible errors
    if(counted_bars < 0) 
        return(-1);
//---- last counted bar will be recounted
    if(counted_bars > 0) 
        counted_bars--;
    limit = Bars - counted_bars;
//----Calculation---------------------------
    for(i = 0; i < limit; i++)
      {
        nowplusdi = iADX(NULL, 0, ADXPeriod, PRICE_CLOSE, MODE_PLUSDI, i);
        nowminusdi = iADX(NULL, 0, ADXPeriod, PRICE_CLOSE, MODE_MINUSDI, i);
        if(nowminusdi > nowplusdi) 
          {
            LowBarBuffer[i] = Low[i];
            HighBarBuffer[i] = High[i];
          }
        if(nowplusdi > nowminusdi) 
          {
            HighBarBuffer[i] = Low[i];
            LowBarBuffer[i] = High[i];
          }
      }
    for(i = limit - 1; i >=0 ; i--)
      {
        ind_buffer1[i] = iMAOnArray(HighBarBuffer, 0, ADXBARSPeriod, 0, MODE_SMA, i); 
        ind_buffer2[i] = iMAOnArray(LowBarBuffer, 0, ADXBARSPeriod, 0, MODE_SMA, i); 
      }
//---- done
    return(0);
  }