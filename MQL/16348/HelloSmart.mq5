//+------------------------------------------------------------------+
//|                                                   HelloSmart.mq5 |
//|                                              Copyright 2016, AM2 | 
//|                                     https://www.forexsystems.biz | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright 2016, AM2" 
#property link "https://www.forexsystems.biz" 
#property version "1.00" 

#include <Trade\Trade.mqh> 
#include <Trade\PositionInfo.mqh>   

CTrade trade;
CPositionInfo pos;

input int BuySell     = 2;    // 1-Only Buy 2-Only Sell
input int Step        = 1000; // Step
input double Lot      = 0.1;  // Volume
input double BigLot   = 0.5;  // Big Lot
input double MaxLots  = 5;    // Maximum lot
input double Profit   = 60;   // Dollars Profit 
input double Loss     = 5100; // Dollars Loss
input double Mnogitel = 10;   // Lots Mnogitel 

int bars=0;
double lot=Lot,pr=0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Comment("");
  }
//+------------------------------------------------------------------+
//|  Position Profit                                                 |
//+------------------------------------------------------------------+
double SymbProfit(string Symb)
  {
   double p=0;
   for(int i=PositionsTotal()-1;i>=0;i--)
     {
      if(pos.Select(Symb))
        {
         p+=pos.Profit();
        }
     }
   return(p);
  }
//+------------------------------------------------------------------+
//|  Position Volume                                                 |
//+------------------------------------------------------------------+
double Lots(string Symb)
  {
   double lots=0;

   for(int i=PositionsTotal()-1;i>=0;i--)
     {
      if(pos.Select(Symb))
        {
         lots+=pos.Volume();
        }
     }
   return(lots);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseAll(string symb)
  {
   for(int i=PositionsTotal()-1;i>=0;i--)
     {
      if(pos.Select(symb))
        {
         trade.PositionClose(symb);
        }
     }
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   string symb=Symbol();
   double Ask=SymbolInfoDouble(symb,SYMBOL_ASK);
   double Bid=SymbolInfoDouble(symb,SYMBOL_BID);

   if(bars!=Bars(symb,0))
     {
      if(BuySell==1 && (PositionsTotal()<1 || (PositionsTotal()>0 && (pr-Ask)/_Point>=Step))) {trade.PositionOpen(symb,0,lot,Ask,0,0); pr=Bid;}//buy
      if(BuySell==2 && (PositionsTotal()<1 || (PositionsTotal()>0 && (Bid-pr)/_Point>=Step))) {trade.PositionOpen(symb,1,lot,Bid,0,0); pr=Bid;}//sell

      if(Lots(symb)>=BigLot) lot=lot*Mnogitel;
      if(lot>MaxLots) lot=Lot;

      if(SymbProfit(symb)>Profit || SymbProfit(symb)<-Loss) {CloseAll(symb); lot=Lot;}
      bars=Bars(symb,0);
     }

   Comment("\n Lots: ",Lots(symb),
           "\n Profit: ",SymbProfit(symb),
           "\n Price: ",pr);
  }
//+------------------------------------------------------------------+
