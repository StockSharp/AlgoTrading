//+------------------------------------------------------------------+
//|                                                 bb-automated.mq4 |
//|                                  Copyright 2017, Mohammad Soubra |
//|                  https://www.mql5.com/en/users/soubra2003/seller |
//+------------------------------------------------------------------+


#property copyright "Copyright 2017, Mohammad Soubra"
#property link      "https://www.mql5.com/en/users/soubra2003/seller"
#property version   "1.00"
#property strict

#include <stdlib.mqh>


input int      BB_period   =  20;
input double   BB_dev      =  2;

double   BB_UPPER, BB_SMA, BB_LOWER;


//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   HideTestIndicators(true);
   //This function sets a flag hiding indicators called by the Expert Advisor
   
   BB_UPPER   = iBands(NULL ,PERIOD_M15 ,BB_period ,BB_dev ,0 ,PRICE_CLOSE ,MODE_UPPER ,0);
   BB_SMA     = iBands(NULL ,PERIOD_M15 ,BB_period ,BB_dev ,0 ,PRICE_CLOSE ,MODE_SMA   ,0);
   BB_LOWER   = iBands(NULL ,PERIOD_M15 ,BB_period ,BB_dev ,0 ,PRICE_CLOSE ,MODE_LOWER ,0);

   //Close executed Buy/Sell when touch the center MA
   if(Close[0]>=BB_SMA)
      CloseAllBuy();
   else if(Close[0]<=BB_SMA)
      CloseAllSell();
   
   if(iVolume(NULL,PERIOD_M15,0)==1)
   //New candle
   {
      if(OrdersTotal()>0)  CloseAllPending();
   
    //---
      BuyLimitExecute();
      SellLimitExecute();
   }
}


//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
//---
   
   Comment("");
}


//+------------------------------------------------------------------+
//| Expert BuyLimitExecute function
//+------------------------------------------------------------------+
void BuyLimitExecute()
{
   int buylimit=  OrderSend(OrderSymbol(),            //Pair
                  /*DOWN*/  OP_BUYLIMIT,              //Command Type
                            0.01,                     //Lot Size
                            BB_LOWER,                 //Needed Price
                            3,                        //Max. Slippage
                            0,                        //Stop Loss
                            0,                        //Take Profit
                            "BB Automated",           //Comment
                            1221,                     //Magic No.
                            0,                        //Expiration (Only Pending Orders)
                            clrNONE);                 //Arrow Color
               
   if(buylimit>0)
      Print("Buy-Limit order successfully placed.");
   else
      Print("Error in placing buy-limit order: ", ErrorDescription(GetLastError()));
}


//+------------------------------------------------------------------+
//| Expert SellLimitExecute function
//+------------------------------------------------------------------+
void SellLimitExecute()
{
   int selllimit = OrderSend(OrderSymbol(),           //Pair
                     /*UP*/  OP_SELLLIMIT,            //Command Type
                             0.01,                    //Lot Size
                             BB_UPPER,                //Needed Price
                             3,                       //Max. Slippage
                             0,                       //Stop Loss
                             0,                       //Take Profit
                             "BB Automated",          //Comment
                             1221,                    //Magic No.
                             0,                       //Expiration (Only Pending Orders)
                             clrNONE);                //Arrow Color

   if(selllimit>0)
      Print("Sell-Limit order successfully placed.");
   else
      Print("Error in placing sell-limit order: ", ErrorDescription(GetLastError()));
}


//+------------------------------------------------------------------+
//| CLOSE ALL OPENED BUY
//+------------------------------------------------------------------+
void CloseAllBuy()
{
   int total = OrdersTotal();
   for(int i=total-1; i>=0; i--)
   {
      int  ticket = OrderSelect(i,SELECT_BY_POS);
      int  type   = OrderType();
      bool result = false;

      if( OrderMagicNumber() == 1221 )
      {
         switch(type)
         {
            //Close opened long positions
            case OP_BUY  : result = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),6,clrNONE);
         }
   
         if(!result)
         {
            Alert("Order ",OrderTicket()," failed to close. Error: ",ErrorDescription(GetLastError()));
            Sleep(750);
         }
      }
   }
}


//+------------------------------------------------------------------+
//| CLOSE ALL OPENED SELL
//+------------------------------------------------------------------+
void CloseAllSell()
{
   int total = OrdersTotal();
   for(int i=total-1; i>=0; i--)
   {
      int  ticket = OrderSelect(i,SELECT_BY_POS);
      int  type   = OrderType();
      bool result = false;

      if( OrderMagicNumber() == 1221 )
      {
         switch(type)
         {
            //Close opened short positions
            case OP_SELL : result = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),6,clrNONE);
         }
   
         if(!result)
         {
            Alert("Order ",OrderTicket()," failed to close. Error: ",ErrorDescription(GetLastError()));
            Sleep(750);
         }
      }
   }
}


//+------------------------------------------------------------------+
//| CLOSE ALL PENDING POSITIONS
//+------------------------------------------------------------------+
void CloseAllPending()
{
   int total = OrdersTotal();
   for(int i=total-1; i>=0; i--)
   {
      int  ticket = OrderSelect(i,SELECT_BY_POS);
      int  type   = OrderType();
      bool result = false;

      switch(type)
      {
         case OP_BUYLIMIT  : result = OrderDelete(OrderTicket(),clrNONE);  break;
         case OP_SELLLIMIT : result = OrderDelete(OrderTicket(),clrNONE);
      }

      if(result==false)
      {
         Comment("Order ",OrderTicket()," failed to close. Error: ",ErrorDescription(GetLastError()));
         Sleep(750);
      }
   }
}


//+------------------------------------------------------------------+
