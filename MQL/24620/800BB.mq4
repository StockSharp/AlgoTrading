//+------------------------------------------------------------------+
//|                                                        800BB.mq4 |
//|                        Copyright 2019, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2019, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
#property description "EA to trade when price breaks through BB and returns back into the bands. Customizable period."
#include "../Libraries/util.mq4"
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

extern double Risk_Pct = 2; //Risk percentage per trade.
extern int MAGIC = 68792;   // Magic number
extern double TP_ATR_Multiplier = 1.5; // ATR multiplier for profit target
extern double SL_ATR_Multiplier = 1;   // ATR multiplier for sl
extern int ATR_PERIOD = 14;            // ATR period
extern bool DRAW_POSSIBLE_TRADES=false; // Draw a vertical line if there is a possible trade.

extern int BB_PERIOD = 800; // BB period
extern int DEVIATION = 2;   // STD deviation for BB
double Slippage=5;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum MODE_PRICE_STATUS
  {
   NOTHING,
   CROSSED_BELOW_LOWER,
   CROSSED_ABOVE_LOWER,
   CROSSED_ABOVE_HIGHER,
   CROSSED_BELOW_HIGHER,
  };

int PreviousPriceStatus=NOTHING,CurrentPriceStatus=NOTHING;

double point;
static datetime lastbar;
bool TradesOpen;
string longLine="",shortLine="";
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   lastbar=Time[0];
   TradesOpen=false;
   double NrOfDigits=Digits;
   double PipAdjust = 0;
   if(NrOfDigits==5 || NrOfDigits==3) PipAdjust=10;
   else
      if(NrOfDigits==4 || NrOfDigits==2) PipAdjust=1;
   point=Point*PipAdjust;
   Slippage*=PipAdjust;
   return 0;

//---
   return(0);
  }

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+

bool ExitSignal() {return false;}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(NewCandle(lastbar))
     {
      double upperBand = iBands(NULL, 0, BB_PERIOD, DEVIATION, 0, PRICE_CLOSE, MODE_UPPER, 1);
      double lowerBand = iBands(NULL, 0, BB_PERIOD, DEVIATION, 0, PRICE_CLOSE, MODE_LOWER, 1);

      if(OrdersTotal()==0)
        {
         TradesOpen=false;
         Comment("No trades open");
           } else {
         Comment("TradesOpen");
        }
      // if the current candle opened above lowerBand   
      if(Open[0]>=lowerBand)
        {  // Long trades
         if(Open[1]<=lowerBand || PreviousPriceStatus==CROSSED_BELOW_LOWER)
           {
            Print("PRICE closed below BBAND");
            if(DRAW_POSSIBLE_TRADES)
              {
               longLine="GOLONG"+DoubleToStr(Time[0]);
               ObjectCreate(longLine,OBJ_VLINE,0,Time[0],0);
               ObjectSetInteger(0,longLine,OBJPROP_COLOR,clrGreen);
              }
            MakeTrade(false,TradesOpen);
           }
        }

      if(Open[0]<=upperBand)
        {  // Short trades
         if(Open[1]>=upperBand || PreviousPriceStatus==CROSSED_ABOVE_HIGHER)
           {
            Print("Price closed above BBAND");
            if(DRAW_POSSIBLE_TRADES)
              {
               shortLine="GOLONG"+DoubleToStr(Time[0]);
               ObjectCreate(shortLine,OBJ_VLINE,0,Time[0],0);
               ObjectSetInteger(0,shortLine,OBJPROP_COLOR,clrRed);
              }
            MakeTrade(true,TradesOpen);
           }
        }
      if(Open[1] > upperBand || Close[1] > upperBand) {PreviousPriceStatus = CROSSED_ABOVE_HIGHER;}
      if(Open[1] < lowerBand || Close[1] < lowerBand) {PreviousPriceStatus = CROSSED_BELOW_LOWER;}
      if(Open[1]<upperBand && Close[1]<upperBand && Open[1]>lowerBand && Close[1]>lowerBand)
        {PreviousPriceStatus=NOTHING;}
     }
  }
//+------------------------------------------------------------------+

int MakeTrade(bool sell,bool tradesOpen)
  {
   double ATR=iATR(NULL,0,ATR_PERIOD,1);
   double SlPriceDiff=ATR*SL_ATR_Multiplier;
   double pipsToSl=SlPriceDiff/point;
   double lots=CalculateLotSize(Risk_Pct,pipsToSl);
   double sl,tp,openPrice;
   int tradeType,result=-1;
   string description="";
   bool volumeCheck=CheckVolumeValue(lots,description);
   if(!volumeCheck)
     {
      Print(description);
      return -1;
     }

   RefreshRates();
   if(sell)
     {
      sl = Bid + pipsToSl*point;
      tp = Bid - TP_ATR_Multiplier * ATR;
      tradeType = OP_SELL;
      openPrice = Bid;
        } else {
      sl =  Ask - pipsToSl*point;
      tp = Ask + TP_ATR_Multiplier * ATR;
      tradeType = OP_BUY;
      openPrice = Ask;
     }
   if(!tradesOpen)
     {
      result=OrderSend(Symbol(),tradeType,lots,openPrice,Slippage,sl,tp,NULL,MAGIC,0,clrLightBlue);
      TradesOpen=true;
      NotifySmartPhone("800BB");
     }
   return result;
  }
//+------------------------------------------------------------------+
