//+------------------------------------------------------------------+
//|                                         FibonacciRetracement.mq5 |
//|                                                        Oschenker |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Oschenker"
#property link      "https://www.mql5.com"
#property version   "1.00"
#define EXPERT_MAGIC 123456   // MagicNumber эксперта
//--- input parameters
input int      ExtDepth = 12;          //ZigZag Depth
input int      ExtDeviation = 5;       //ZigZag Deviation
input int      ExtBackStep = 3;        //ZigZag BackStep
input int      TradeValue = 1;         //Trade volume
input int      StopLossLevel = 15;     //Stop-loss level (points)
input double   TakeProfitAt = 0.2;     //Take profit at FIBO extension
input int      SafetyBuffer = 1;       //Next bar close distance from level (points)
input int      TrendPrecision = -5;    //Next to previous high(low) distance
input int      CloseBarPause = 5;      //Pause from close to trade (bars)
input color    LevelColor = clrBlack;  //Fibo levels colors


int            ZZ_Handle;
int            TrendDirection;
int            CopyNumber;
long           PositionID;
double         ZZ_Buffer[];
double         HL[4];
double         Fibo00;
double         Fibo23;
double         Fibo38;
double         Fibo61;
double         Fibo76;
double         Fibo100;
double         FiboBASE;
double         StopLoss;
double         MAXProfit;
double         SymbolTickValue;
datetime       CloseBarTime;
datetime       HL_Time[4];
datetime       Time00;
datetime       Time100;
bool           PositionChangeFlag;

MqlTradeRequest Request = {0};
MqlTradeResult  Results = {0};
MqlRates RatesArray[];

//--- Включение стандартной торговой библиотеки
#include <Trade\Trade.mqh>;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- get indicator handle
   ZZ_Handle = iCustom( Symbol(), PERIOD_CURRENT, "Examples\\ZigZag", ExtDepth, ExtDeviation, ExtBackStep);
   Print("CustomIndicatorHandle ",ZZ_Handle);
   if(ZZ_Handle <= 0) Print("Indicator Handle Unsuccessful. Error #", GetLastError());
   HL[0] = 666;
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   IndicatorRelease(ZZ_Handle);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- Copy indicator buffer
   CopyNumber = CopyBuffer(ZZ_Handle, 0, 0, 200, ZZ_Buffer);
   if(CopyNumber <= 0) Print("Indicator Bufer Unavailable");
   ArraySetAsSeries(ZZ_Buffer, true);

//--- copying rates 
   if(CopyRates(Symbol(), 0, 0, 200, RatesArray) > 0)  ///---- Proceeds in case price data is available
        {
        //--- set array as time series
        ArraySetAsSeries(RatesArray, true);
        
        
        //--- Initial high-low points mapping
        if(HL[0] == 666)
             {
             int ZCount = 0;
             for(int i=0; i <= CopyNumber && ZCount <= 3 && !IsStopped(); i++) ///----- ZZ legs maping
                      {
                      if(ZZ_Buffer[i] != 0)
                           {
                           HL[ZCount] = ZZ_Buffer[i];
                           HL_Time[ZCount] = RatesArray[i].time;
                           ZCount++;
                           } 
                      }
             TrendDirection = CheckTrend(HL[0], HL[1], HL[2], HL[3]);
             Fibo00 = HL[0];
             Time00 = HL_Time[0];
             Fibo100 = HL[1];
             Time100 = HL_Time[1];
             FiboBASE = fabs(Fibo100 - Fibo00);
             } 

        // high-low points mapping in concequence
        if(ZZ_Buffer[0] != 0 && ZZ_Buffer[0] != HL[0])        
             {
             HL[3] = HL[2];
             HL[2] = HL[1];
             HL[1] = HL[0];
             HL[0] = ZZ_Buffer[0];       
             HL_Time[3] = HL_Time[2];
             HL_Time[2] = HL_Time[1];
             HL_Time[1] = HL_Time[0];
             HL_Time[0] = RatesArray[0].time;       
             TrendDirection = CheckTrend(HL[0], HL[1], HL[2], HL[3]);
             }
        if(!PositionSelect(Symbol()))
            {
            switch(TrendDirection)
             {
               case  1:
                 Comment("Trend is Up");
                 FiboBASE = Fibo00 - Fibo100;        
                 Fibo23   = Fibo00 - 0.236 * FiboBASE;
                 Fibo38   = Fibo00 - 0.382 * FiboBASE;
                 Fibo61   = Fibo00 - 0.618 * FiboBASE;
                 Fibo76   = Fibo00 - 0.764 * FiboBASE;
                 if(HL[0] > Fibo00)
                   {
                    Fibo00 = HL[0];
                    Time00 = HL_Time[0];
                    if(HL[0] - HL[1] > FiboBASE)
                      {
                       Fibo100 = HL[1];
                       Time100 = HL_Time[1];
                      }
                   }
                 if(HL[0] < Fibo100)
                   {
                    Fibo00 = HL[0];
                    Time00 = HL_Time[0];
                    Fibo100 = HL[1];
                    Time100 = HL_Time[1];
                    TrendDirection = CheckTrend(HL[0], HL[1], HL[2], HL[3]);
                    break;
                   }
                 if(!FiboCreate(Time100, Fibo100, Time00, Fibo00)) Print("Fibo create method fails");
                 
                 // check if buy conditions are met             
                 if(((RatesArray[0].close - Fibo76) > Point() * SafetyBuffer && (Fibo76 - RatesArray[1].close) > Point() * SafetyBuffer) ||
                    ((RatesArray[0].close - Fibo61) > Point() * SafetyBuffer && (Fibo61 - RatesArray[1].close) > Point() * SafetyBuffer) ||
                    ((RatesArray[0].close - Fibo38) > Point() * SafetyBuffer && (Fibo38 - RatesArray[1].close) > Point() * SafetyBuffer) ||
                    ((RatesArray[0].close - Fibo23) > Point() * SafetyBuffer && (Fibo23 - RatesArray[1].close) > Point() * SafetyBuffer))
                           {
                           Print("Buy condition is TRUE");
                           if(!TradeCheck(1)) Print("Wait another bar");
                           }
                 break;
               case -1:
                 Comment("Trend is down");
                 FiboBASE = Fibo100 - Fibo00;                    ///--- map FIBO levels
                 Fibo23   = Fibo00 + 0.236 * FiboBASE;
                 Fibo38   = Fibo00 + 0.382 * FiboBASE;
                 Fibo61   = Fibo00 + 0.618 * FiboBASE;
                 Fibo76   = Fibo00 + 0.764 * FiboBASE;
                 if(HL[0] < Fibo00)
                   {
                    Fibo00 = HL[0];
                    Time00 = HL_Time[0];
                    if(HL[1] - HL[0] > FiboBASE)
                      {
                       Fibo100 = HL[1];
                       Time100 = HL_Time[1];
                      }
                   }
                 if(HL[0] > Fibo100)
                   {
                    Fibo00 = HL[0];
                    Time00 = HL_Time[0];
                    Fibo100 = HL[1];
                    Time100 = HL_Time[1];
                    TrendDirection = CheckTrend(HL[0], HL[1], HL[2], HL[3]);
                    break;
                   }
                 if(!FiboCreate(Time100, Fibo100, Time00, Fibo00)) Print("Fibo create method fails");
                 
                 // check if sell conditions are met             
                 if(((Fibo76 - RatesArray[0].close) > Point() * SafetyBuffer && (Fibo76 - RatesArray[1].close) < Point() * SafetyBuffer) ||
                    ((Fibo61 - RatesArray[0].close) > Point() * SafetyBuffer && (Fibo61 - RatesArray[1].close) < Point() * SafetyBuffer) ||
                    ((Fibo38 - RatesArray[0].close) > Point() * SafetyBuffer && (Fibo38 - RatesArray[1].close) < Point() * SafetyBuffer) ||
                    ((Fibo23 - RatesArray[0].close) > Point() * SafetyBuffer && (Fibo23 - RatesArray[1].close) < Point() * SafetyBuffer))
                           {
                           Print("Buy condition is TRUE");
                           if(!TradeCheck(-1)) Print("Wait another bar");
                           }

                 break;
               case  0:
                 Comment("FLAT");
                 FiboBASE = 0;                    ///--- map FIBO levels
                 Fibo23   = 0;
                 Fibo38   = 0;
                 Fibo61   = 0;
                 Fibo76   = 0;
                 if(!ObjectDelete( 0, "MyFIBO")) Print("FIBO delete method is failed");
                 
                 break;
             }
            }

        }
  else
        {
         Print("Time series is not available ", GetLastError());
        }   
  }

//+------------------------------------------------------------------+
//| TradeCheck function                                              |
//+------------------------------------------------------------------+
bool TradeCheck(int deal)
     {
      CTrade  trade;
      if(Bars(Symbol(), 0, CloseBarTime, TimeCurrent()) < CloseBarPause) return(false);
      switch(deal)
           {
            case 1: 
               if(trade.Buy( TradeValue, NULL, 0, SymbolInfoDouble(Symbol(), SYMBOL_BID) - Point() * StopLossLevel, Fibo00 + TakeProfitAt * FiboBASE))
                  {
                  Print("Method Buy is completed");
                  return(true);
                  }
               break;
            case 2:
               if(trade.Sell( TradeValue, NULL, 0, SymbolInfoDouble(Symbol(), SYMBOL_ASK) + Point() * StopLossLevel, Fibo00 - TakeProfitAt * FiboBASE))
                  {
                  Print("Method Sell is completed");
                  return(true);
                  }
           }
      return(false);
     }
//+------------------------------------------------------------------+
//| CheckTrend function                                              |
//+------------------------------------------------------------------+
bool CheckTrend(double hl0,
                double hl1,
                double hl2,
                double hl3)
   {
   int check_trend = 0;
   if(((hl2 - hl0) > Point() * TrendPrecision) && ((hl3 - hl1) > Point() * TrendPrecision)) check_trend = -1; // trend is down
   if(((hl0 - hl2) > Point() * TrendPrecision) && ((hl1 - hl3) > Point() * TrendPrecision)) check_trend =  1; // trend is up
   return(check_trend);
   }              
    
//+------------------------------------------------------------------+
//| FiboCreate function                                              |
//+------------------------------------------------------------------+
bool FiboCreate(datetime time1, double array1, datetime time0, double array0)
    {
     if(!ObjectDelete( 0, "MyFIBO"))  Print("FIBO delete method is failed");
     if(ObjectCreate( 0, "MyFIBO", OBJ_FIBO, 0, time1, array1, time0, array0)) ///----- FIBO retracement creation based on last ZZ leg
               {
               ObjectSetInteger(0, "MyFIBO", OBJPROP_LEVELCOLOR, LevelColor);
               ObjectSetInteger(0, "MyFIBO", OBJPROP_LEVELSTYLE, STYLE_SOLID);
               ObjectSetInteger(0, "MyFIBO", OBJPROP_RAY_RIGHT, true);
               ObjectSetInteger(0, "MyFIBO", OBJPROP_LEVELS, 6);
               ObjectSetDouble(0,  "MyFIBO", OBJPROP_LEVELVALUE, 0, 0.000);
               ObjectSetDouble(0,  "MyFIBO", OBJPROP_LEVELVALUE, 1, 0.236);
               ObjectSetDouble(0,  "MyFIBO", OBJPROP_LEVELVALUE, 2, 0.382);
               ObjectSetDouble(0,  "MyFIBO", OBJPROP_LEVELVALUE, 3, 0.618);
               ObjectSetDouble(0,  "MyFIBO", OBJPROP_LEVELVALUE, 4, 0.764);
               ObjectSetDouble(0,  "MyFIBO", OBJPROP_LEVELVALUE, 5, 1.000);
               ObjectSetString(0,  "MyFIBO", OBJPROP_LEVELTEXT, 0, "0.0% (%$)");
               ObjectSetString(0,  "MyFIBO", OBJPROP_LEVELTEXT, 1, "23.6% (%$)");
               ObjectSetString(0,  "MyFIBO", OBJPROP_LEVELTEXT, 2, "38.2% (%$)");
               ObjectSetString(0,  "MyFIBO", OBJPROP_LEVELTEXT, 3, "61.8% (%$)");
               ObjectSetString(0,  "MyFIBO", OBJPROP_LEVELTEXT, 4, "76.4% (%$)");
               ObjectSetString(0,  "MyFIBO", OBJPROP_LEVELTEXT, 5, "100.0% (%$)");                              
               return(true);
               }
               else {Print("MyFIBO creation is failed"); return (false);}
    }
//+------------------------------------------------------------------+
//| Trade function                                                   |
//+------------------------------------------------------------------+
void OnTrade()
 {
  int      DealsNum;
  ulong    DealTiket;
  PositionChangeFlag = true;
  if(HistorySelectByPosition(PositionID))
   {
    DealsNum = HistoryDealsTotal(); //--- total deals number in the list
    Print("Deal number ", (string)DealsNum);
    for( int i=0; i < DealsNum && !IsStopped(); i++)
      {
       DealTiket = HistoryDealGetTicket(i);
       if(StringFind(HistoryDealGetString(DealTiket, DEAL_COMMENT), "sl", 0))
            {
             CloseBarTime = (datetime)HistoryDealGetInteger(DealTiket,DEAL_TIME);
            }
      }
   }
 }
//+------------------------------------------------------------------+
