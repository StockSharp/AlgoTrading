//+------------------------------------------------------------------+
//|                         Futures Portfolio Control Expiration.mq5 |
//|                                         Copyright 2017, Serj_Che |
//|                           https://www.mql5.com/ru/users/serj_che |
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, Serj_Che"
#property link      "https://www.mql5.com/ru/users/serj_che"
#property version   "1.00"

#include <Trade\Trade.mqh>

enum esym { Si, Eu, ED, AUDU, GBPU, BR, GOLD, SBRF, GAZR, MXI, RTS, LKOH, ROSN, VTBR };

input esym     Symbol1     = MXI;
input esym     Symbol2     = BR;
input esym     Symbol3     = SBRF;
input int      Lot1        = -4.0;
input int      Lot2        = -1.0;
input int      Lot3        = 5.0;
input int      hours_before_expiration = 25;

CTrade trade;
string Futures1;
string Futures2;
string Futures3;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   Futures1=CurrFutures(EnumToString(Symbol1));
   Futures2=CurrFutures(EnumToString(Symbol2));
   Futures3=CurrFutures(EnumToString(Symbol3));
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   CheckPos(EnumToString(Symbol1),Futures1,Lot1);
   CheckPos(EnumToString(Symbol2),Futures2,Lot2);
   CheckPos(EnumToString(Symbol3),Futures3,Lot3);
  }
//+------------------------------------------------------------------+
void CheckPos(string sym, string & TradeFutures, double lot)
  {
   if(SymbolInfoInteger(TradeFutures,SYMBOL_EXPIRATION_TIME)-TimeCurrent()<hours_before_expiration*60*60)
     {
      trade.PositionClose(TradeFutures);
         Sleep(3000);
      SymbolSelect(TradeFutures,false);
      TradeFutures=NextFutures(sym);
     }
   if(!PositionSelect(TradeFutures))
     {
      if(lot>0) trade.Buy(lot,TradeFutures);
      if(lot<0) trade.Sell(-lot,TradeFutures);
         Sleep(3000);
     }
  }
//+------------------------------------------------------------------+
string CurrFutures(string short_name)
  {
   string long_name;
   MqlDateTime time;
   TimeCurrent(time);
   int year=time.year;
   int mon=time.mon;
   for(int i=0; i<12; i++)
     {
      if(mon>12) { mon=1; year++; }
      StringConcatenate(long_name,short_name,"-",mon,".",year%100);
      if(SymbolSelect(long_name,true))
        {
         if(SymbolInfoInteger(long_name,SYMBOL_EXPIRATION_TIME)>TimeCurrent()) break;
        }
      mon++;
      long_name="";
     }
   return(long_name);
  }
//+------------------------------------------------------------------+
string NextFutures(string short_name)
  {
   string long_name;
   MqlDateTime time;
   TimeCurrent(time);
   int year=time.year;
   int mon=time.mon;
   datetime currtime=0;
   for(int i=0; i<12; i++)
     {
      if(mon>12) { mon=1; year++; }
      StringConcatenate(long_name,short_name,"-",mon,".",year%100);
      //Print(long_name);
      if(SymbolSelect(long_name,true))
        {
         int expirat=(int)SymbolInfoInteger(long_name,SYMBOL_EXPIRATION_TIME);
         if(currtime==0)
           if(expirat>TimeCurrent()) { currtime=expirat; mon++; continue; }
         if(currtime!=0) if(expirat>currtime) break;
        }
      mon++;
      long_name="";
     }
   return(long_name);
  }
//+------------------------------------------------------------------+
