//+------------------------------------------------------------------+
//|                                                 NightScalper.mq5 |
//|                                              Copyright 2016, AM2 | 
//|                                     https://www.forexsystems.biz | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright 2016, AM2" 
#property link "https://www.forexsystems.biz" 
#property version "1.00" 

#include <Trade\Trade.mqh>                 // Use CTrade Class

input string Symbol1     = "USDCAD";       // Symbol1 Name
input int    StopLoss1   = 370;            // StopLoss1
input int    TakeProfit1 = 20;             // TakeProfit1
input int    BBPeriod1   = 40;             // Bands Period1
input double BBDev1      = 1;              // Bands Deviation1
input double Razmah1     = 450;            // Bands Deviation1 in Points
input int    Start1      = 19;             // Start Time1

input string Symbol2     = "GBPUSD";       // Symbol2 Name
input int    StopLoss2   = 450;            // StopLoss2
input int    TakeProfit2 = 80;             // TakeProfit2
input int    BBPeriod2   = 8;              // Bands Period2
input double BBDev2      = 1;              // Bands Deviation2
input double Razmah2     = 200;            // Bands Deviation2 in Points
input int    Start2      = 20;             // Start Time2

input string Symbol3     = "NZDUSD";       // Symbol3 Name
input int    StopLoss3   = 410;            // StopLoss3
input int    TakeProfit3 = 40;             // TakeProfit3
input int    BBPeriod3   = 4;              // Bands Period3
input double BBDev3      = 1.2;            // Bands Deviation3
input double Razmah3     = 450;            // Bands Deviation3 in Points
input int    Start3      = 19;             // Start Time3

input string Symbol4     = "";             // Symbol4 Name
input int    StopLoss4   = 500;            // StopLoss4
input int    TakeProfit4 = 40;             // TakeProfit4
input int    BBPeriod4   = 24;             // Bands Period4
input double BBDev4      = 1;              // Bands Deviation4
input double Razmah4     = 200;            // Bands Deviation4 in Points
input int    Start4      = 20;             // Start Time4

input string Symbol5     = "";             // Symbol5 Name
input int    StopLoss5   = 500;            // StopLoss5
input int    TakeProfit5 = 40;             // TakeProfit5
input int    BBPeriod5   = 24;             // Bands Period5
input double BBDev5      = 1;              // Bands Deviation5
input double Razmah5     = 200;            // Bands Deviation5 in Points
input int    Start5      = 20;             // Start Time5

input string Symbol6     = "";             // Symbol6 Name
input int    StopLoss6   = 500;            // StopLoss6
input int    TakeProfit6 = 40;             // TakeProfit6
input int    BBPeriod6   = 24;             // Bands Period6
input double BBDev6      = 1;              // Bands Deviation6
input double Razmah6     = 200;            // Bands Deviation6 in Points
input int    Start6      = 20;             // Start Time6

input string Symbol7     = "";             // Symbol7 Name
input int    StopLoss7   = 500;            // StopLoss7
input int    TakeProfit7 = 40;             // TakeProfit7
input int    BBPeriod7   = 24;             // Bands Period7
input double BBDev7      = 1;              // Bands Deviation7
input double Razmah7     = 200;            // Bands Deviation7 in Points
input int    Start7      = 20;             // Start Time7

input string Symbol8     = "";             // Symbol8 Name
input int    StopLoss8   = 500;            // StopLoss8
input int    TakeProfit8 = 40;             // TakeProfit8
input int    BBPeriod8   = 24;             // Bands Period8
input double BBDev8      = 1;              // Bands Deviation8
input double Razmah8     = 200;            // Bands Deviation8 in Points
input int    Start8      = 20;             // Start Time8

input string Symbol9     = "";             // Symbol9 Name
input int    StopLoss9   = 500;            // StopLoss9
input int    TakeProfit9 = 40;             // TakeProfit9
input int    BBPeriod9   = 24;             // Bands Period9
input double BBDev9      = 1;              // Bands Deviation9
input double Razmah9     = 200;            // Bands Deviation9 in Points
input int    Start9      = 20;             // Start Time9

input double Lot         = 1;              // Trade Volume

int BBHandle=0,bars=0;
double up[1],dn[1];
CTrade trade;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Count Trades on Symbol                                           |
//+------------------------------------------------------------------+
int CountTrades(string Symb)
  {
   int count=0;
   for(int i=PositionsTotal()-1;i>=0;i--)
     {
      if(PositionGetSymbol(i)==Symb)
        {
         if(PositionGetInteger(POSITION_TYPE)<2) count++;
        }
     }
   return(count);
  }
//+------------------------------------------------------------------+
//|    Symbols Trade Function                                        |
//+------------------------------------------------------------------+
void SymbolTrade(string symb,int stop,int take,int per,double dev,double razmah,int start)
  {
   string s=(string)start+":00";
   double Ask=SymbolInfoDouble(symb,SYMBOL_ASK);
   double Bid=SymbolInfoDouble(symb,SYMBOL_BID);

   BBHandle=iBands(symb,0,per,0,dev,0);
   CopyBuffer(BBHandle,1,0,1,up);
   CopyBuffer(BBHandle,2,0,1,dn);

   double r=up[0]-dn[0];

   if(CountTrades(symb)<1 && TimeCurrent()>StringToTime(s))
     {
      if(Ask<dn[0] && r<razmah*_Point) trade.PositionOpen(symb,0,Lot,Ask,Ask-stop*_Point,Ask+take*_Point);
      if(Bid>up[0] && r<razmah*_Point) trade.PositionOpen(symb,1,Lot,Bid,Bid+stop*_Point,Bid-take*_Point);
     }
   else if(CountTrades(symb)>0 && TimeCurrent()<StringToTime(s)) CloseAll(symb);
  }
//+------------------------------------------------------------------+
//|    Close All Positions on Symbol                                 |
//+------------------------------------------------------------------+
void CloseAll(string Symb)
  {
   for(int i=PositionsTotal()-1;i>=0;i--)
     {
      if(PositionGetSymbol(i)==Symb)
        {
         if(PositionGetInteger(POSITION_TYPE)<2) trade.PositionClose(Symb);
        }
     }
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(bars!=Bars(NULL,0))
     {
      if(Symbol1!="") SymbolTrade(Symbol1,StopLoss1,TakeProfit1,BBPeriod1,BBDev1,Razmah1,Start1);
      if(Symbol2!="") SymbolTrade(Symbol2,StopLoss2,TakeProfit2,BBPeriod2,BBDev2,Razmah2,Start2);
      if(Symbol3!="") SymbolTrade(Symbol3,StopLoss3,TakeProfit3,BBPeriod3,BBDev3,Razmah3,Start3);
      if(Symbol4!="") SymbolTrade(Symbol4,StopLoss4,TakeProfit4,BBPeriod4,BBDev4,Razmah4,Start4);
      if(Symbol5!="") SymbolTrade(Symbol5,StopLoss5,TakeProfit5,BBPeriod5,BBDev5,Razmah5,Start5);
      if(Symbol6!="") SymbolTrade(Symbol6,StopLoss6,TakeProfit6,BBPeriod6,BBDev6,Razmah6,Start6);
      if(Symbol7!="") SymbolTrade(Symbol7,StopLoss7,TakeProfit7,BBPeriod7,BBDev7,Razmah7,Start7);
      if(Symbol8!="") SymbolTrade(Symbol8,StopLoss8,TakeProfit8,BBPeriod8,BBDev8,Razmah8,Start8);
      if(Symbol9!="") SymbolTrade(Symbol9,StopLoss9,TakeProfit9,BBPeriod9,BBDev9,Razmah9,Start9);
     }
   bars=Bars(NULL,0);
  }
//+------------------------------------------------------------------+
