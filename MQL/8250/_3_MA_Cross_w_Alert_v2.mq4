// This is Not Tested , Use At Your Own Risk !

//+--------------------------------------------------------------------------+
//| 3 MA Cross w_Alert v2.mq4                                                |
//| Copyright © 2005, Jason Robinson (jnrtrading)                            |
//| http://www.jnrtading.co.uk                                               |
//| 3 ma conversion and Alert , David Honeywell , transport.david@gmail.com  |
//| http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/ |
//+--------------------------------------------------------------------------+

/*
  +-------------------------------------------------------------------------------+
  | Allows you to enter 3 ma periods and it will then show you and alert you at   |
  | which point the 2 faster ma's "OPEN" are both above or below the Slowest ma . |
  +-------------------------------------------------------------------------------+
*/   

#property copyright "Copyright © 2005, Jason Robinson (jnrtrading)"
#property link      "http://www.jnrtrading.co.uk"

#property indicator_chart_window
#property indicator_buffers 2
#property indicator_color1 Aqua
#property indicator_color2 Coral

double CrossUp[];
double CrossDown[];
double prevtime;
double Range, AvgRange;
double fasterMAnow, fasterMAprevious, fasterMAafter;
double mediumMAnow, mediumMAprevious, mediumMAafter;
double slowerMAnow, slowerMAprevious, slowerMAafter;

extern int FasterMA    =    2;
extern int FasterShift =    0;
extern int FasterMode  =    1; // 0 = sma, 1 = ema, 2 = smma, 3 = lwma

extern int MediumMA    =    4;
extern int MediumShift =    0;
extern int MediumMode  =    1; // 0 = sma, 1 = ema, 2 = smma, 3 = lwma

extern int SlowerMA    =   30;
extern int SlowerShift =    0;
extern int SlowerMode  =    1; // 0 = sma, 1 = ema, 2 = smma, 3 = lwma

extern int SoundAlert =    0; // 0 = disabled

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
int start()
 {
   
   int limit, i, counter;
   
   int counted_bars=IndicatorCounted();
   //---- check for possible errors
   if(counted_bars<0) return(-1);
   //---- last counted bar will be recounted
   if(counted_bars>0) counted_bars--;
   
   limit=Bars-counted_bars;
   
   for(i = 0; i <= limit; i++)
    {
      
      counter=i;
      Range=0;
      AvgRange=0;
      for (counter=i ;counter<=i+9;counter++)
       {
         AvgRange=AvgRange+MathAbs(High[counter]-Low[counter]);
       }
      Range=AvgRange/10;
       
      fasterMAnow      = iMA(NULL, 0, FasterMA, FasterShift, FasterMode, PRICE_CLOSE, i+1);
      fasterMAprevious = iMA(NULL, 0, FasterMA, FasterShift, FasterMode, PRICE_CLOSE, i+2);
      fasterMAafter    = iMA(NULL, 0, FasterMA, FasterShift, FasterMode, PRICE_CLOSE, i-1);
      
      mediumMAnow      = iMA(NULL, 0, MediumMA, MediumShift, MediumMode, PRICE_CLOSE, i+1);
      mediumMAprevious = iMA(NULL, 0, MediumMA, MediumShift, MediumMode, PRICE_CLOSE, i+2);
      mediumMAafter    = iMA(NULL, 0, MediumMA, MediumShift, MediumMode, PRICE_CLOSE, i-1);
      
      slowerMAnow      = iMA(NULL, 0, SlowerMA, SlowerShift, SlowerMode, PRICE_CLOSE, i+1);
      slowerMAprevious = iMA(NULL, 0, SlowerMA, SlowerShift, SlowerMode, PRICE_CLOSE, i+2);
      slowerMAafter    = iMA(NULL, 0, SlowerMA, SlowerShift, SlowerMode, PRICE_CLOSE, i-1);
      
      if ((fasterMAnow     > slowerMAnow       &&
          fasterMAprevious <= slowerMAprevious  &&
          fasterMAafter    > slowerMAafter     &&
          mediumMAnow      > slowerMAnow     )
          ||
          (fasterMAnow     > slowerMAnow       &&
          mediumMAnow      > slowerMAnow       &&
          mediumMAprevious <= slowerMAprevious  &&
          mediumMAafter    > slowerMAafter   ))
       {
         CrossUp[i] = Low[i] - Range*0.5;
       }
       
      if ((fasterMAnow     < slowerMAnow       &&
          fasterMAprevious >= slowerMAprevious  &&
          fasterMAafter    < slowerMAafter     &&
          mediumMAnow      < slowerMAnow     )
          ||
          (fasterMAnow     < slowerMAnow       &&
          mediumMAnow      < slowerMAnow       &&
          mediumMAprevious >= slowerMAprevious  &&
          mediumMAafter    < slowerMAafter   ))
       {
         CrossDown[i] = High[i] + Range*0.5;
       }
      
    }
   
      if ((CrossUp[0] > 2000) && (CrossDown[0] > 2000)) { prevtime = 0; }
      
      if ((CrossUp[0] == Low[0] - Range*0.5) && (prevtime != Time[0]) && (SoundAlert != 0))
       {
         prevtime = Time[0];
         Alert(Symbol()," 3 MA Cross Up @  Hour ",Hour(),"  Minute ",Minute());
       }
      
      if ((CrossDown[0] == High[0] + Range*0.5) && (prevtime != Time[0]) && (SoundAlert != 0))
       {
         prevtime = Time[0];
         Alert(Symbol()," 3 MA Cross Down @  Hour ",Hour(),"  Minute ",Minute());
       }
   
   //Comment("  CrossUp[0]  ",CrossUp[0]," ,  CrossDown[0]  ",CrossDown[0]," ,  prevtime  ",prevtime);
   //Comment("");
   
   return(0);
 }

