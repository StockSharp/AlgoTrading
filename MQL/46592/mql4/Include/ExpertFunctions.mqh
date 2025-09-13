//+------------------------------------------------------------------+
//|                                              ExpertFunctions.mqh |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property strict

// Returns the Profits of all open trades of a symbol with a given Magic Number
double Profits(int MagicNum, const string symbol)
{
   double cnt = 0;
   
   for (int i = OrdersTotal(); i >= 0; i--)
   {
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
         if (OrderMagicNumber() == MagicNum && OrderSymbol() == symbol)
            cnt += OrderProfit() + OrderCommission();
   }
   return cnt;
}

//Closing out all Orders with a given symbol and Magic Number
void CloseAllOrders(const string symbol, int MagicNum = -1)
{
   for(int a = 0; a < 3; a++)
   {
      int i = OrdersTotal()-1;
      for(i; i >= 0;i--)
      { 
         bool MagicPass = false;
         if(MagicNum == -1)
            MagicPass = true;
         //Print("Magicpass: "+IntegerToString(MagicPass));
         bool check = OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
         if (check && (OrderMagicNumber() == MagicNum || MagicPass) && OrderSymbol() == symbol)
         {
            //Print("egyet closeolni kene");
            if (OrderType() <= 1)
            { 
               if(OrderType() == OP_BUY)
                  if(!OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK), 5000, clrAqua)) Print("Can't close all orders" + IntegerToString(GetLastError()));
               if(OrderType() == OP_SELL)
                  if(!OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID), 5000, clrAqua)) Print("Can't close all orders" + IntegerToString(GetLastError()));
            }  
            else 
            {
               if (!OrderDelete(OrderTicket())) Print("Can't close all orders" + IntegerToString(GetLastError()));
            }
         }
      }
   }
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

// checking if a volume is a valid Volume
bool CheckVolumeValue(double volume)
{
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      return(false);
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      return(false);
     }

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {

      return(false);
     }

   return(true);
}

// returns the amount of money it'd take to open a lot
double LotSize(double Price = 0.0)
{  
   double Cost = AccountFreeMargin() - AccountFreeMarginCheck(_Symbol,OP_BUY,SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN));
   return Cost / SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
}

// Calculating the Lotsize of a Trade with a TakeProfit Parameter in Monetary amount
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

//if there is a new candle on the chart returns true
bool IsNewCandle()
{
   static int Numberofbars = Bars;
   if(Bars != Numberofbars)
   {
      Numberofbars = Bars;
      return true;
   }
   return false;
}

//returns the number of total trades open
int TotalopenOrders(int MagicNum, string symbol)
{
   int ret = 0;
   for(int i = OrdersTotal()-1;i >= 0;i--)
   {
      if(OrderSelect(i,SELECT_BY_POS))
         if(OrderMagicNumber() == MagicNum && OrderSymbol() == symbol)
            ret++;
   }         
   return ret;
}
