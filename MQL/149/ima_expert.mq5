//+------------------------------------------------------------------+
//|                                                          ima.mq5 |
//|                                                      Vladimir M. |
//|                                                mikh.vl@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Vladimir M."
#property link      "mikh.vl@gmail.com"
#property version   "1.00"

#include <Trade\Trade.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\AccountInfo.mqh>

input int n=5;            // MA Period
input int take=50;        // Take Profit
input int drop=1000;      // Stop Loss
input double k=0.5;       // Signal level
input double r=0.01;      // Risk level
input double max_lots=1;  // Maximal lot (0-unlimited)

int imaHandle,sp=30;
bool position_opened,f_buy,f_sell,f_buy_modify,f_sell_modify,f_close;
double lot,k1,aIMA[];
datetime h,aT[1];

MqlTick current_price;
CPositionInfo current_order;
CAccountInfo account_info;
CTrade trade;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   imaHandle=iCustom(_Symbol,0,"ima",n);// //get handle of the indicator
   if(imaHandle==INVALID_HANDLE)
     {
      Print("ima indicator not found!");
      return(1);
     }
   trade.SetDeviationInPoints(sp);
   CopyTime(_Symbol,0,0,1,aT);
   h=aT[0];
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   IndicatorRelease(imaHandle);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//+------------------------------------------------------------------+
//| Select opened positions
//+------------------------------------------------------------------+
   position_opened=(PositionSelect(_Symbol));
//+------------------------------------------------------------------+
//| Signals
//+------------------------------------------------------------------+
//if there isn't any opened positions and new bar has formed, we check the signals
   CopyTime(_Symbol,0,0,1,aT);  //get the last bar time
   if(!position_opened && h!=aT[0])
     {

      CopyBuffer(imaHandle,0,0,2,aIMA);  //copying the indicator's data to the buffer
      ArraySetAsSeries(aIMA,true);  //set array indexation as timeseries

      k1=(aIMA[0]-aIMA[1])/MathAbs(aIMA[1]);
      f_buy=(k1>=k);   //signal to open long position
      f_sell=(k1<=-k); //signal to open short position
      h=aT[0];
     }
//+------------------------------------------------------------------+
//| opened position control                                          |
//+------------------------------------------------------------------+   
   if(position_opened && current_order.Profit()>0)
     {

      SymbolInfoTick(_Symbol,current_price);
      //position management - trailing stop
      f_buy_modify=(current_order.Type()==POSITION_TYPE_BUY && 
                    (current_price.bid-current_order.PriceOpen())/_Point>take &&
                    (current_price.bid-current_order.StopLoss())/_Point>take);
      f_sell_modify=(current_order.Type()==POSITION_TYPE_SELL && 
                     (current_order.PriceOpen()-current_price.ask)/_Point>take && 
                     (MathAbs(current_order.StopLoss()-current_price.ask))/_Point>take);

      if(f_buy_modify)
         trade.PositionModify(_Symbol,current_price.bid-take*_Point,current_order.TakeProfit());
      else if(f_sell_modify)
         trade.PositionModify(_Symbol,current_price.ask+take*_Point,current_order.TakeProfit());
     }

//close loss position  
   f_close=(position_opened && current_order.Profit()<0 && 
            MathAbs(current_order.Profit()/current_order.Volume()/SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_VALUE))>=drop);
   if(f_close)
      trade.PositionClose(_Symbol,sp);
//+------------------------------------------------------------------+
//| Lot size management                                              |
//+------------------------------------------------------------------+
   if(!position_opened && (f_buy || f_sell))
     {
      SymbolInfoTick(_Symbol,current_price);
      //determine lot size depending on r and drop levels
      lot=account_info.FreeMargin()*r/drop/SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_VALUE);
      //round it
      lot/=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
      lot=MathFloor(lot);
      lot*=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
      //if necessary, we decrease lot size to the necessary value with the minimal lot step
      if(f_buy)
         while(account_info.FreeMarginCheck(_Symbol,ORDER_TYPE_BUY,lot,current_price.ask)<=0 && lot>0)
            lot-=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
      if(f_sell)
         while(account_info.FreeMarginCheck(_Symbol,ORDER_TYPE_SELL,lot,current_price.bid)<=0 && lot>0)
            lot-=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
      //setting the maximal lot size
      if(lot>max_lots && max_lots>0)
         lot=max_lots;
      if(lot>SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX))
         lot=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX);
      if(lot<SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN))
         lot=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
     }
//+------------------------------------------------------------------+
//| open position                                                    |
//+------------------------------------------------------------------+
   if(!position_opened && (f_sell || f_buy) && lot>=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN))
     {
      SymbolInfoTick(_Symbol,current_price);
      if(f_buy)
         trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,lot,current_price.ask,0,0,"");
      if(f_sell)
         trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,lot,current_price.bid,0,0,"");

      //if(trade.ResultDeal()>0)// doesn't works in testing mode
      fdrop();
     }
//+------------------------------------------------------------------+
//| Comments                                                         |
//+------------------------------------------------------------------+
   Comment("k_current="+DoubleToString(k1,4)+"\n"+
           "lot="+DoubleToString(lot,2));
  }
//+------------------------------------------------------------------+
//| Functions                                                        |
//+------------------------------------------------------------------+   
void fdrop(){f_buy=0;f_sell=0;}
//+------------------------------------------------------------------+
