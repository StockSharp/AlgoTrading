//+------------------------------------------------------------------+
//|                                                  MyLineOrder.mq4 |
//|                                                 Chris Smallpeice |
//|                                                                  |
//+------------------------------------------------------------------+
/*
Version 2.1

Inital line name variables

#buy = Open a buy market trade
#sell = Open a sell market trade
#buypend = Open a buy pending order at that price
#sellpend = Open a sell pending order at that price
Todo: #buytp = Open a buy market trade with the line as take profit
Todo: #buysl = Open a buy market trade with line as stop loss
Todo: #selltp = Open a sell market trade with the line as take profit
Todo: #sellsl = Open a sell market trade with line as stop loss

After trade variables(without quotes)
"sl=" = Stop loss in pips. Can have multiple orders. To have no stop loss use "N"
"tp=" = Stop loss in pips. Can have multiple orders. To have no take profit use "N"
"ts=" = Trailing stop in pips. Can have multiple orders
"lo=" = The lots which are open. (Todo: If changed then order modify the lots in order)
"alarm" = For values view the comment on LO_ALARM



*/
#property copyright "Chris Smallpeice"
#include <LineOrderLibrary.mqh>
/*
   Possible updates
   
   OCO (Order cancels other)
   Multiple SL, TP 
   To change SL, TP, TS and LO at set levels - done apart from LO
   Line Alarms(non-trade)
   Local pending orders
*/
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
initVar();
start();
//----
   return(0);
  }

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
deinitVar();
//----
   return(0);
  }

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----

//if(count<5)count++;else{
processLines(); 
//count=0;

//}
//----
   return(0);
  }
//+------------------------------------------------------------------+

