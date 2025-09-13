//+------------------------------------------------------------------+
//|                                                 JMCloseOrders.mq4|
//|                                             Jedimedic77@gmail.com|
//|             Origional Coding Before Chop Copyright © 2007, sx ted|
//|                                                                  |
//| Several Functions were removed from the origonal to speed it up  |
//| and K.I.S.S. its my first attempt at leaening MQL4 good luck guys|
//|                                                     Jim Malwitz  |
//+------------------------------------------------------------------+
#property show_inputs
//+------------------------------------------------------------------+
//| EX4 imports                                                      |
//+------------------------------------------------------------------+
#include <stdlib.mqh>
//+------------------------------------------------------------------+
//| input parameters:                                                |
//+-----------0---+----1----+----2----+----3]------------------------+
extern string _ = "Select One of the Following:";
extern bool   CloseOpenOrders = true;
extern bool   CloseOrdersWithPlusProfit = false;
extern bool   CloseOrdersWithMinusProfit = false;
//+------------------------------------------------------------------+
//| global variables to program:                                     |
//+------------------------------------------------------------------+
double Price[2];
int    giSlippage;
//+------------------------------------------------------------------+
//| script program start function                                    |
//+------------------------------------------------------------------+
void start() {
  int iOrders=OrdersTotal()-1, i;
  
  if(CloseOpenOrders) {
    for(i=iOrders; i>=0; i--) {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) && (OrderType()<=OP_SELL) && GetMarketInfo() && !OrderClose(OrderTicket(),OrderLots(),Price[1-OrderType()],giSlippage)) Print(OrderError());
    }
  }
  else if(CloseOrdersWithPlusProfit) {
    for(i=iOrders; i>=0; i--) {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) && (OrderProfit() >= 0)) {
        if((OrderType()<=OP_SELL) && GetMarketInfo()) {
          if(!OrderClose(OrderTicket(),OrderLots(),Price[1-OrderType()],giSlippage)) Print(OrderError());
        }
      }
    }
  }
  else if(CloseOrdersWithMinusProfit) {
    for(i=iOrders; i>=0; i--) {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) && (OrderProfit() <= 0)) {
        if((OrderType()<=OP_SELL) && GetMarketInfo()) {
          if(!OrderClose(OrderTicket(),OrderLots(),Price[1-OrderType()],giSlippage)) Print(OrderError());
        }
      }
    }
  }
}
//+------------------------------------------------------------------+
//| Function..: OrderError                                           |
//+------------------------------------------------------------------+
string OrderError() {
  int iError=GetLastError();
  return(StringConcatenate("Order:",OrderTicket()," GetLastError()=",iError," ",ErrorDescription(iError)));
}
//+------------------------------------------------------------------+
//| Function..: GetMarketInfo                                        |
//+------------------------------------------------------------------+
bool GetMarketInfo() {
  RefreshRates();
  Price[0]=MarketInfo(OrderSymbol(),MODE_ASK);
  Price[1]=MarketInfo(OrderSymbol(),MODE_BID);
  double dPoint=MarketInfo(OrderSymbol(),MODE_POINT);
  if(dPoint==0) return(false);
  giSlippage=(Price[0]-Price[1])/dPoint;
  return(Price[0]>0.0 && Price[1]>0.0);
}
//+------------------------------------------------------------------+