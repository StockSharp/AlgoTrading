//+------------------------------------------------------------------+
//|                                              ExpertFunctions.mqh |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#include <Trade\Trade.mqh>

bool CheckVolumeValue(double Vol)
{
   double checkvol = Vol;
   double ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);
   double marg;
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   
   if(checkvol<min_volume)  // Volume smaller than the allowed minimum volume by the broker
      return false;

   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(checkvol>max_volume) // Volume bigger than the allowed maximum volume by the broker
      return false;
      
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(checkvol/volume_step);
   if(MathAbs(ratio*volume_step-checkvol)>0.0000001)        //Volume is not valid, it is not a valid volume step: for example you can only trade 0.01 or 0.02 EURUSD but not 0.013452 Lot.
      return false;
      
   if(!OrderCalcMargin(ORDER_TYPE_BUY,_Symbol,Vol,ask,marg))
      Print("Failed OrderCalcmargin");
      
   if(marg > AccountInfoDouble(ACCOUNT_MARGIN_FREE)) // There is not enough margin to execute a trade with the given volume
      return false;
      
   return true; 
}

// Basic round 0.5-> 1 0.49 -> 0
double Round(double value, int decimals)
{     
   double timesten, truevalue;
   if (decimals < 0) 
   {  
      Print("Wrong decimals input parameter, paramater cant be below 0");
      return 0;
   }
   timesten = value * MathPow(10,decimals);
   timesten = MathRound(timesten);
   truevalue = timesten/MathPow(10,decimals);
   return truevalue;      
}

double RoundDown(double val,int decim)
{
   if(Round(val,decim) > val) return Round(val,decim)-MathPow(10,-decim);
   else return Round(val,decim); 
}
   
// Rounds the parameter to a valid Lot amount
double RoundtoLots(double Val)        
{  
   double ret = 0.0;
   Val = Round(Val,6);
   if(SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN) == 0.01)
      ret = RoundDown(Val,2);
   if(SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN) == 0.1)
      ret = RoundDown(Val,1);
   if(SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN) == 0.001)
      ret = RoundDown(Val,3);
  ret = RoundDown(Val,0);
  return MathMax(ret,SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN));
}


//if there is a new candle on the chart returns true
bool IsNewCandle()
{
   static int Numberofbars = iBars(_Symbol,PERIOD_CURRENT);
   if(iBars(_Symbol,PERIOD_CURRENT) != Numberofbars)
   {
      Numberofbars = iBars(_Symbol,PERIOD_CURRENT);
      return true;
   }
   return false;
}
 
// returns true if the Market is open otherwise false.
bool MarketOpen()
{
    datetime from = NULL;
    datetime to = NULL;
    datetime serverTime = TimeTradeServer();

    // Get the day of the week
    MqlDateTime dt;
    TimeToStruct(serverTime,dt);
    const ENUM_DAY_OF_WEEK day_of_week = (ENUM_DAY_OF_WEEK) dt.day_of_week;

    // Get the time component of the current datetime
    const int time = (int) MathMod(serverTime,PeriodSeconds(PERIOD_D1));

    // Brokers split some symbols between multiple sessions.
    // One broker splits forex between two sessions (Tues thru Thurs on different session).
    // 2 sessions (0,1,2) should cover most cases.
    int session=2;
    while(session > -1)
    {
        if(SymbolInfoSessionTrade(_Symbol,day_of_week,session,from,to ))
        {
            if(time >=from && time <= to )
            {
                return true;
            }
        }
        session--;
    }
    return false;
}

//get the profit of the last trade.
double HistoryLastProfit()
{
   static datetime Last = 0;
   HistorySelect(Last,TimeCurrent()); //first a history cache needs to be selected.
   if(!HistoryDealsTotal())         // if there is no trade here, return 0.
      return 0.0;
   int Ticket = (int) HistoryDealGetTicket(HistoryDealsTotal()-1); // get the ticket of the last trade
   if(HistoryDealGetInteger(Ticket,DEAL_TIME) > Last)  //update the last time
      Last = (datetime) HistoryDealGetInteger(Ticket,DEAL_TIME);
   return HistoryDealGetDouble(Ticket,DEAL_PROFIT); // get the profits.
}