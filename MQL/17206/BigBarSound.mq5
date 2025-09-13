//+------------------------------------------------------------------+
//|                                                  BigBarSound.mq4 |
//|                                               Alexey Volchanskiy |
//|                                         http://www.robo-forex.ru |
//+------------------------------------------------------------------+
#property copyright "Alexey Volchanskiy"
#property link      "http://www.robo-forex.ru"
#property version   "1.00"
#property strict
#property description "EA plays WavFile when bar size is lager of BarPoint value"

#include <Trade\SymbolInfo.mqh>

enum StartPoint {OpenClose,HighLow};

input ENUM_TIMEFRAMES TimeFrame = PERIOD_CURRENT;
input int             BarPoint  = 200;
input StartPoint      SP        = HighLow;
input string          WavFile   = "alert.wav";
input bool            ShowAlert = false;

MqlRates rates_array[1];
CSymbolInfo symbolInfo;
//+------------------------------------------------------------------+
//| Detects begin of new bar                                         |
//+------------------------------------------------------------------+
bool NewBar()
  {
   static datetime lastbar=0;
   datetime curbar=rates_array[0].time;
   if(lastbar!=curbar)
     {
      lastbar=curbar;
      return (true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   double diff=0;
   static bool trigger=true; // for one-shot play sound in bar duration
   symbolInfo.RefreshRates();
   if(!CopyRates(_Symbol,TimeFrame,0,1,rates_array))
     {
      Print("ERROR: Can't copy the new bar data!");
      return;
     }
   if(NewBar())
      trigger=true;

   if(SP==OpenClose)
      diff=MathAbs(symbolInfo.Ask()-rates_array[0].open);
   else
      diff=MathAbs(rates_array[0].high-rates_array[0].low);
   if(trigger && diff>=BarPoint*Point())
     {
      PlaySound(WavFile);
      trigger=false;
      if(ShowAlert)
         Alert("Signal!");
     }
  }
//+------------------------------------------------------------------+
