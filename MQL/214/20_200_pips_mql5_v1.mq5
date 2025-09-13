//+------------------------------------------------------------------+
//|                                          20_200_pips_MQL5_v1.mq5 |
//|                                    Copyright 2010, Pavel Smirnov |
//|                                          http://www.autoforex.ru |
//+------------------------------------------------------------------+

#property copyright "Copyright 2010, Pavel Smirnov"
#property link      "http://www.autoforex.ru"
#property version   "1.00"
//--- input parameters
input int      TakeProfit=200;
input int      StopLoss=2000;
input int      TradeTime=18;
input int      t1=7;
input int      t2=2;
input int      delta=70;
input double   lot=0.1;

bool cantrade=true;  // can we trade?
double Ask;          // variable for Ask price of the new tick
double Bid;          // variable for Bid price of the new tick
//+------------------------------------------------------------------+
//| Long position opening function                                   |
//+------------------------------------------------------------------+
int OpenLong(double volume=0.1,
             int slippage=10,
             string comment="EUR/USD 20 pips expert (Long)",
             int magic=0)
  {
//--- declare a structure of MqlTradeRequest type
   MqlTradeRequest my_trade;
   ZeroMemory(my_trade);
   
//--- declare a structure of for trade request result
   MqlTradeResult my_trade_result;

//--- fill all the NECESSARY fields
//--- instant execution
   my_trade.action=TRADE_ACTION_DEAL;
//--- current symbol of the chart
   my_trade.symbol=Symbol();
//---lot size
   my_trade.volume=NormalizeDouble(volume,1);
//--- order price, in our case (TRADE_ACTION_DEAL) it's the current price,
//---  so it isn't necessary to specify it
   my_trade.price=NormalizeDouble(Ask,_Digits);
//--- stop loss price
   my_trade.sl=NormalizeDouble(Ask-StopLoss*_Point,_Digits);
//--- take profit price
   my_trade.tp=NormalizeDouble(Ask+TakeProfit*_Point,_Digits);
//--- slippage in pips
   my_trade.deviation=slippage;
//--- order type (buy)
   my_trade.type=ORDER_TYPE_BUY;
//--- order filling type All Or Nothing) 
   my_trade.type_filling=ORDER_FILLING_FOK;
//--- order comment
   my_trade.comment=comment;
//--- order magic 
   my_trade.magic=magic;
//--- reset last error code
   ResetLastError();
//---sending request to open position and checking the result
   if(OrderSend(my_trade,my_trade_result))
     {
      //--- if the order has been accepted, print the result
      Print("Operation result code - ",my_trade_result.retcode);
     }
   else
     {
      //--- there are some errors in the order, print them in Journal
      Print("Operation result code - ",my_trade_result.retcode);
      Print("Error in request = ",GetLastError());
     }
   return(0);// return from the function
  }
//+------------------------------------------------------------------+
//| Short position opening function (it's similar to OpenLong)       |
//+------------------------------------------------------------------+
int OpenShort(double volume=0.1,
              int slippage=10,
              string comment="EUR/USD 20 pips expert (Short)",
              int magic=0)
  {
   MqlTradeRequest my_trade;
   MqlTradeResult my_trade_result;
   ZeroMemory(my_trade);
   
   my_trade.action=TRADE_ACTION_DEAL;
   my_trade.symbol=Symbol();
   my_trade.volume=NormalizeDouble(volume,1);
   my_trade.price=NormalizeDouble(Bid,_Digits);
   my_trade.sl=NormalizeDouble(Bid+StopLoss*_Point,_Digits);
   my_trade.tp=NormalizeDouble(Bid-TakeProfit*_Point,_Digits);
   my_trade.deviation=slippage;
   my_trade.type=ORDER_TYPE_SELL;
   my_trade.type_filling=ORDER_FILLING_FOK;
   my_trade.comment=comment;
   my_trade.magic=magic;

   ResetLastError();
   if(OrderSend(my_trade,my_trade_result))
     {
      Print("Operation result code - ",my_trade_result.retcode);
     }
   else
     {
      Print("Operation result code - ",my_trade_result.retcode);
      Print("Error in request = ",GetLastError());
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason){}

//+------------------------------------------------------------------+
//| Expert OnTick function                                           |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- array for open prices (Open[t1] and Open[t2] will be used)
   double Open[];
//--- current time
   MqlDateTime mqldt;
//--- update current time
   TimeCurrent(mqldt);
//--- variable for Open[] array size.
   int len;

//---structure for last tick
   MqlTick last_tick;
//--- filling last_tick with recent prices
   SymbolInfoTick(_Symbol,last_tick);
//--- update bid and ask
   Ask=last_tick.ask;
   Bid=last_tick.bid;

//--- set Open[] array as timeseries
//--- calculate size of the array to include, Open[t1] and Open[t2]
   ArraySetAsSeries(Open,true);

//--- t1 and t2 - bar indexes, get the largest value
//--- and add 1 (for 0th bar)
   if(t1>=t2)len=t1+1;
   else len=t2+1;

//---filling the Open[] array with current values
   CopyOpen(_Symbol,PERIOD_H1,0,len,Open);

//--- set cantrade to true, to allow trading of Expert Advisor
   if(((mqldt.hour)>TradeTime)) cantrade=true;

//--- check for position opening:
//--- if there isn't any opened positions
   if(!PositionSelect(_Symbol))
     {
      //--- time to trade
      if((mqldt.hour==TradeTime) && (cantrade))
        {
         //--- check sell conditions
         if(Open[t1]>(Open[t2]+delta*_Point))
           {
            //--- open Short position
            OpenShort(lot,10,"EUR/USD 20 pips expert (Short)",1234);
            //--- reset flag (disable trading until the next day)
            cantrade=false;
            //--- exit
            return;
           }
         //--- check buy conditions  
         if((Open[t1]+delta*_Point)<Open[t2])
           {
            //--- open Long position
            OpenLong(lot,10,"EUR/USD 20 pips expert (Long)",1234);
            //--- reset flag (disable trading until the next day)
            cantrade=false;
            //--- exit
            return;
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
