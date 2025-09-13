//+-------------------------------------------------------+
//|                                InverseReaction_EA.mq5 |
//|                        Copyright 2013-2014, Erdem Sen |
//+-------------------------------------------------------+
#property copyright "Copyright 2013-2014 Erdem Sen"
#property link      "https://login.mql5.com/en/users/erdogenes"
#property version   "1.2"
//---
#property description "IREA is based on the idea of that an unusual impact in" 
#property description "price changes will be corrected by an inverse reaction."
#property description "It works with the indicator InverseReaction"
//--- include trade library
#include <Trade\Trade.mqh>

//--- Trade parameters
input int         StopLoss    =  1000;          // Stop loss
input int         TakeProfit  =  250;           // Take profit
input double      TrVol       =  1.0;           // Trade volume (lot size)
input int         Slipagge    =  3;             // Affordable order slippage
//--- Signal parameters
input int         MinCriteria =  300;           // Minimum bar size for signal
input int         MaxCriteria =  2000;          // Maximum bar size for signal
//--- Indicator parameters
input double      Coefficient =  1.618;         // Confidence coefficient
input int         MaPeriod    =  3;             // Moving average period

//--- Globals
CTrade   trade;
int      IR_handle;
bool     signal[2];
double   maxcriteria,mincriteria,IRC[],IRL[],change,ask,bid,buy_sl,buy_tp,sell_sl,sell_tp;
datetime t1;
//+-------------------------------------------------------+
int OnInit()
  {
//--- Check MaPeriod
   if(MaPeriod<3)
     {   Print(" Init Error!! The value of MaPeriod must be at least 3!!");
         return(INIT_FAILED);
     }
//--- Check the number of bars
   if(Bars(_Symbol,_Period)<MaPeriod)
     {   PrintFormat("Init Error!! There are less than %d bars, EA will now exit!!",MaPeriod);
         return(INIT_FAILED);
     }
//--- Indicator handle
   ResetLastError();
   IR_handle=iCustom(_Symbol,_Period,"InverseReaction",MaPeriod,Coefficient);
   if(IR_handle==INVALID_HANDLE)
     {   Print("invalid handle error!! Error code: ",GetLastError());
         return(-1);
     }
//---
   mincriteria   = MinCriteria*_Point;
   maxcriteria   = MaxCriteria*_Point;
//--- Trade settings
   trade.SetExpertMagicNumber(783);
   trade.SetDeviationInPoints(Slipagge);
   trade.SetTypeFilling(ORDER_FILLING_RETURN);
   trade.SetAsyncMode(false);
//---
   return(INIT_SUCCEEDED);
  }
//+-------------------------------------------------------+
void OnDeinit(const int reason)
  {
   IndicatorRelease(IR_handle);
  }
//+-------------------------------------------------------+
void OnTick()
  {
//---   
   t1=(datetime)SeriesInfoInteger(_Symbol,_Period,SERIES_LASTBAR_DATE);
   static datetime  t0=t1;
//--- check for new bar and if there is any open position
   if(t1!=t0 && PositionsTotal()==0)
     {   //--- copy the neccessary buffers
         if(CopyBuffer(IR_handle,0,1,2,IRC)<=0 || CopyBuffer(IR_handle,1,1,2,IRL)<=0) return;
         ArraySetAsSeries(IRC,true);
         ArraySetAsSeries(IRL,true);
         //--- if there is a signal, send order
         if(IsIR(signal)) SendOrder();
         //--- set the time again
         t0=t1;
     }
//---  
  }
//-------------Checks if there is a signal----------------+
bool IsIR(bool &sign[])
  {
   for(int i=0;i<2;i++)
     {   change   =  fabs(IRC[i]);
         sign[i]  = (change>IRL[i] && change<maxcriteria && change>mincriteria);
     }
   return(sign[0]!=0 && (sign[0]+sign[1])!=2);
  }
//---------------------Sends Order------------------------+
bool SendOrder()
  {
   ask      = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
   bid      = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
   buy_sl   = NormalizeDouble(ask-StopLoss*_Point,_Digits);
   buy_tp   = NormalizeDouble(ask+TakeProfit*_Point,_Digits);
   sell_sl  = NormalizeDouble(bid+StopLoss*_Point,_Digits);
   sell_tp  = NormalizeDouble(bid-TakeProfit*_Point,_Digits);
//-->
   if(IRC[0]<0) //--- buy order: Buy(volume,symbol,price,sl,tp)
     {   if(!trade.Buy(TrVol,_Symbol,ask,buy_sl,buy_tp))
            {  Print("failed to send order. Error: ",GetLastError());
               return(false);
     }      }           
   else         //--- sell order: Sell(volume,symbol,price,sl,tp)
     {   if(!trade.Sell(TrVol,_Symbol,bid,sell_sl,sell_tp))
            {  Print("failed to send order. Error: ",GetLastError());
               return(false);
     }      }
//---
   return(true);
  }
//+-------------------------------------------------------+