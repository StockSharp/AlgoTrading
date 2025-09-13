//+------------------------------------------------------------------+
//|                                              MeanReversion.mq5   |
//|                                  Copyright 2025, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                          Author: Yashar Seyyedin |
//|       Web Address: https://www.mql5.com/en/users/yashar.seyyedin |
//+------------------------------------------------------------------+

#include <Trade\Trade.mqh>
CTrade trade;

input group "EA Setting"
input int lookback = 200;
input double risk_per_trade = 1;


//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(PositionsTotal()>0)
      return;

   if(iLowest(_Symbol, PERIOD_CURRENT, MODE_LOW, lookback, 0)==0)
      Buy();

   if(iHighest(_Symbol, PERIOD_CURRENT, MODE_HIGH, lookback, 0)==0)
      Sell();
  }
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Buy()
  {
   double Ask=SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   double tp=Mean();
   double sl=2*Ask-tp;
   double lot=CalculateLots(Ask, sl, ORDER_TYPE_BUY);
   if(CheckMoneyForTrade(_Symbol, lot, ORDER_TYPE_BUY))
      trade.Buy(lot, _Symbol, Ask,sl,tp);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Sell()
  {
   double Bid=SymbolInfoDouble(_Symbol, SYMBOL_BID);
   double tp=Mean();
   double sl=2*Bid-tp;
   double lot=CalculateLots(Bid, sl, ORDER_TYPE_SELL);
   if(CheckMoneyForTrade(_Symbol, lot, ORDER_TYPE_SELL))
      trade.Sell(lot, _Symbol, Bid,sl,tp);
  }
//+------------------------------------------------------------------+

//|                                                                  |
//+------------------------------------------------------------------+
double CalculateLots(double op, double sl, ENUM_ORDER_TYPE order_type)
  {
   double lot=SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   while(true)
     {
      double pnl=0;
      if(OrderCalcProfit(order_type, _Symbol, lot, op, sl, pnl)==false)
         return lot;
      if(pnl>=0)
         return lot;
      if(pnl<-1*AccountInfoDouble(ACCOUNT_BALANCE)*risk_per_trade/100)
         return lot;
      lot+=SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
     }
   return NormalizeVolume(lot);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double NormalizeVolume(double lot)
  {
   double vol_step=SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
   lot=int(lot / vol_step)*vol_step;
   if(lot>SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX))
      return SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   if(lot<SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN))
      return SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   return lot;

  }

double Mean()
  {
   double high[];
   double low[];
   double highest_price;
   double lowest_price;

   CopyHigh(_Symbol, PERIOD_CURRENT, 0, lookback, high);
   CopyLow(_Symbol, PERIOD_CURRENT, 0, lookback, low);

   highest_price = high[ArrayMaximum(high, 0, lookback)];
   lowest_price = low[ArrayMinimum(low, 0, lookback)];
   return (highest_price+lowest_price)/2;
  }
//+------------------------------------------------------------------+

bool CheckMoneyForTrade(string symb,double lots,ENUM_ORDER_TYPE type)
  {
//--- Getting the opening price
   MqlTick mqltick;
   SymbolInfoTick(symb,mqltick);
   double price=mqltick.ask;
   if(type==ORDER_TYPE_SELL)
      price=mqltick.bid;
//--- values of the required and free margin
   double margin,free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   //--- call of the checking function
   if(!OrderCalcMargin(type,symb,lots,price,margin))
     {
      //--- something went wrong, report and return false
      Print("Error in ",__FUNCTION__," code=",GetLastError());
      return(false);
     }
   //--- if there are insufficient funds to perform the operation
   if(margin>free_margin)
     {
      //--- report the error and return false
      Print("Not enough money for ",EnumToString(type)," ",lots," ",symb," Error code=",GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
  }