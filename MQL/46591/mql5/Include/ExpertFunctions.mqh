//+------------------------------------------------------------------+
//|                                              ExpertFunctions.mqh |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#include <Trade\Trade.mqh>


// Returns the Profits of all open trades of a symbol with a given Magic Number
double Profits(int MagicNum, string symbol)
{
  double prof = 0;
  CPositionInfo Posinfo;
  for(int i = PositionsTotal()-1;i >= 0; i--)
  {
      if(Posinfo.SelectByIndex(i))
         if(Posinfo.Magic() == MagicNum && Posinfo.Symbol() == symbol)
            prof += Posinfo.Profit()+ Posinfo.Commission() + Posinfo.Swap();
  }
   return prof;
}
//Closing out all Orders with a given symbol and Magic Number
void CloseAllOrders(const string symbol, int MagicNum = -1)
{
   CTrade Trade;
   CPositionInfo PosInfo;
   COrderInfo OrderInfo;
   for(int a = 0; a < 3; a++)
   {
      int i = OrdersTotal()-1;
      for(i; i >= 0;i--)
      {
         bool MagicPass = false;
         if(MagicNum == -1)
            MagicPass = true;
         OrderInfo.SelectByIndex(i);
         if ((OrderInfo.Magic() == MagicNum || MagicPass) && OrderInfo.Symbol() == symbol)
         {
            Trade.OrderDelete(OrderInfo.Ticket());
         }
      }
      i = PositionsTotal()-1;
      for(i; i >= 0;i--)
      {
         bool MagicPass = false;
         if(MagicNum == -1)
            MagicPass = true;
         PosInfo.SelectByIndex(i);
         if ((PosInfo.Magic() == MagicNum || MagicPass) && PosInfo.Symbol() == symbol)
         {
            Trade.PositionClose(PosInfo.Ticket());
         }
      }   
   }
} 


//Calculates position size for a given takeprofit in monetary amount and a startprice and endprice of the trade
double CalcLotWithTP(double TakeProfit, double StartPrice, double EndPrice)
{
   if(StartPrice == EndPrice)
      return -1;
   double ret = TakeProfit/SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_VALUE)/MathAbs(StartPrice-EndPrice)*Point();
   double roundedVal  = 0.0;
   if (SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP) == 0.01)
      roundedVal = Round(ret,2) >= ret? Round(ret,2) : Round(ret,2)+ 0.01;
   else if(SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP) == 0.1)
      roundedVal = Round(ret,1) >= ret? Round(ret,1) : Round(ret,1)+ 0.1;
   else if(SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP) == 0.001)
      roundedVal = Round(ret,3) >= ret? Round(ret,3) : Round(ret,3)+ 0.001;
   else 
      roundedVal = Round(ret,0) >= ret? Round(ret,0) : Round(ret,0)+ 1;
      if(roundedVal < SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN)) return SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   return roundedVal;
}

//returns true if there is a bullbreakout
bool BullBreakout(string symbol, ENUM_TIMEFRAMES Timeframe, int bar = 1)
{
   return(AbnormalCandle(symbol,Timeframe,bar)&& iClose(symbol,Timeframe,bar) > iOpen(symbol,Timeframe,bar) && iClose(symbol,Timeframe,bar) - iOpen(symbol,Timeframe,bar) > 0.5*(iHigh(symbol,Timeframe,bar)-iLow(symbol,Timeframe,bar)));
}
//returns true if there is a bearbreakout.
bool BearBreakout(string symbol, ENUM_TIMEFRAMES Timeframe, int bar = 1)
{
   return(AbnormalCandle(symbol,Timeframe,bar)&& iClose(symbol,Timeframe,bar) < iOpen(symbol,Timeframe,bar) && iOpen(symbol,Timeframe,bar) - iClose(symbol,Timeframe,bar) > 0.5*(iHigh(symbol,Timeframe,bar)-iLow(symbol,Timeframe,bar)));
}
//returns true if the last candle is abnormally big compared to the ones before
bool AbnormalCandle(string symbol, ENUM_TIMEFRAMES Timeframe, int bar = 1)
{     double SavedChange = 0;
      
      for (int i = bar+1; i < bar+11; i ++)
      {
         SavedChange = SavedChange + (iHigh(symbol,Timeframe,i) - iLow(symbol,Timeframe,i));
      }
      double Averagechange = SavedChange/10;
      
      if((iHigh(symbol,Timeframe,bar) - iLow(symbol,Timeframe,bar)) > Averagechange * 3) return true;
      return false;
}

// returns the amount of money it'd take to open a lot
double LotSize(double Price = 0.0)
{   
   double ret;
   double ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);
   if(!OrderCalcMargin(ORDER_TYPE_BUY,_Symbol,1,Price,ret) &&Price)
   {
      Print("Can not calculate size of a lot in your base currency properly for " + _Symbol + " security at this price: " + DoubleToString(Price));
      ret = -1;
   }
   if(ret <= 0)
      if(!OrderCalcMargin(ORDER_TYPE_BUY,_Symbol,1,ask,ret))
      {
         Print("Can not calculate the size of a Lot in your base currency for this security at the Ask Price");
         return -1;
      }
   return ret;
}

bool CheckVolumeValue(double Vol)
{
   double checkvol = Vol;
   double ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);
   double marg;
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(checkvol<min_volume)
     {
      return(false);
     }
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(checkvol>max_volume)
     {
      return(false);
     }
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(checkvol/volume_step);
   if(MathAbs(ratio*volume_step-checkvol)>0.0000001)
     {

      return(false);
     }
   if(!OrderCalcMargin(ORDER_TYPE_BUY,_Symbol,Vol,ask,marg))
      Print("Failed OrderCalcmargin");
   if(marg > AccountInfoDouble(ACCOUNT_MARGIN_FREE))
      return false;
   return(true);
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
  if(SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN) == 0.001)
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

//returns the number of total trades open
int TotalopenOrders(int MagicNum, string symbol)
{
   int ret = 0;
   CPositionInfo Posinfo;
   int i = PositionsTotal()-1;
   for(i;i >= 0;i--)
   {
      if(Posinfo.SelectByIndex(i))
         if(Posinfo.Magic() == MagicNum &&Posinfo.Symbol() == symbol)
            ret++;
   }         
   return ret;
}

bool MarketOpen()
{
   MqlDateTime Time;
   TimeCurrent(Time);
   datetime from, to;
   ENUM_DAY_OF_WEEK DOW = (ENUM_DAY_OF_WEEK) Time.day_of_week;
   SymbolInfoSessionTrade(_Symbol,DOW,0,from,to);
   MqlDateTime FromTime, ToTime;
   TimeToStruct(from,FromTime);
   TimeToStruct(to,ToTime);
   if(Time.hour <= FromTime.hour && Time.min <= FromTime.min)
      return false;
   if(Time.hour >= ToTime.hour && Time.min >= ToTime.min)
      return false;
  return true;
}
