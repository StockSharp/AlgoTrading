//+------------------------------------------------------------------+
//|                                                   chtrailing.mq5 |
//|    Copyright 2014, Andrey Litvichenko, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, Andrey Litvichenko, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

#include <Trade\Trade.mqh>

CTrade trade;
CPositionInfo position;
COrderInfo order;
//--- declaration structures and enumerations
struct ind_data
  {
   double            tr_h;
   double            tr_l;
  };
  
struct dot
  {
   datetime          time;
   double            price;
  };
//--- input parameters
input int   trail_period=5;         // Channel period
input int   trail_stop=50;          // Level trail in points
input bool  noose_trailing=true;    // Use "noose" trailing
input bool  channel_trailing=true;  // Use channel trailing
input bool  ord_delete=true;        // Delete pending orders
//--- declaration of variables
datetime nb[1];
double   trs_lvl;
string   prefix;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---initialize variables
   prefix="chtrEa "+_Symbol+" "+EnumToString(_Period)+" ";
   trs_lvl=trail_stop*_Point;
   CopyTime(_Symbol,_Period,1,1,nb);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   ClearObjects(prefix);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   ind_data i_data;
//---check to see if the stop loss level
   if(PositionSelect(_Symbol))
      if(position.StopLoss()==0)
        {
         GetIndicatorData(i_data);
         CheckTrailing(i_data);
        }
//---ñhecking trailing "noose"
   if(noose_trailing)
      CheckNoose();

   datetime inb[1];
//---check the event "new bar"
   CopyTime(_Symbol,_Period,0,1,inb);
   if(inb[0]>nb[0])
      nb[0]=inb[0];
   else
      return;
//--- retrieve data indicator
   if(!GetIndicatorData(i_data))
      return;
//--- check for position
   if(position.Select(_Symbol))
      if(channel_trailing)
         CheckTrailing(i_data); //channel trailing
  }
//+------------------------------------------------------------------+
//| GetIndicatorData function                                        |
//+------------------------------------------------------------------+
bool GetIndicatorData(ind_data &data)
  {
   double cb[];
   datetime time[];
   dot      ds,de;
//--- obtain top-level channel
   ArrayResize(cb,trail_period);
   if(CopyHigh(_Symbol,_Period,1,trail_period,cb)<=0)
      return(false);
   data.tr_h=cb[ArrayMaximum(cb)]+trs_lvl;
//--- obtain low-level channel 
   if(CopyLow(_Symbol,_Period,1,trail_period,cb)<=0)
      return(false);
   data.tr_l=cb[ArrayMinimum(cb)]-trs_lvl;
//--- rendering levels on the chart
   CopyTime(_Symbol,_Period,trail_period,1,time);
   ds.time=time[0];
   CopyTime(_Symbol,_Period,0,1,time);
   de.time=time[0];
   ds.price=data.tr_h;
   de.price=data.tr_h;
   DrawTLine(ds,de,"hi_line",clrBlue,1);
   ds.price=data.tr_l;
   de.price=data.tr_l;
   DrawTLine(ds,de,"low_line",clrFireBrick,1);
   return(true);
  }
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &trans,
                        const MqlTradeRequest &request,
                        const MqlTradeResult &result)
  {
//---
   int ord_total;
   ulong tickets[];

   if(ord_delete && trans.type==TRADE_TRANSACTION_DEAL_ADD && trans.symbol==_Symbol)
     {
      if(PositionSelect(_Symbol))
        {
         //---get the number and tickets of pending orders
         ord_total=GetOrderOpen(_Symbol,tickets);
         for(int i=0;i<ord_total;i++)
            trade.OrderDelete(tickets[i]); //delete pending orders
        }
     }
  }
//+------------------------------------------------------------------+
//| GetOrderOpen function                                            |
//+------------------------------------------------------------------+
int GetOrderOpen(string symb,ulong &tickets[])
  {
   int ot;
   int oo=0;
   ulong ticket;

   ot=OrdersTotal();
   ArrayResize(tickets,ot);

   for(int i=ot-1;i>=0;i--)
     {
      order.SelectByIndex(i);
      ticket=order.Ticket();
      if(order.Symbol()==symb)
        {
         tickets[oo]=ticket;
         oo++;
        };
     };
   ArrayResize(tickets,oo);
   return(oo);
  }
//+------------------------------------------------------------------+
//| CheckStopLevel function                                          |
//+------------------------------------------------------------------+
bool CheckStopLevel(ENUM_POSITION_TYPE type,double sl)
  {
   double   sl_l=SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL)*_Point;//stop level
   double   fr_l=SymbolInfoInteger(_Symbol,SYMBOL_TRADE_FREEZE_LEVEL)*_Point;//freeze level
   MqlTick  tick;

   SymbolInfoTick(_Symbol,tick);
   switch(type)
     {
      case POSITION_TYPE_BUY:
        {
         if(sl<tick.bid-sl_l && sl<tick.bid-fr_l)
            return(true);
        }
      ;break;
      case POSITION_TYPE_SELL:
        {
         if(sl>tick.ask+sl_l && sl>tick.ask+fr_l)
            return(true);
        }
      ;break;
     }
   return(false);
  }
//+------------------------------------------------------------------+
//| CheckNoose function                                              |
//+------------------------------------------------------------------+
void CheckNoose()
  {
   MqlTick  tick;
   double   n_stop_loss;
   dot      ds,de;
   datetime time[1];

   if(position.Select(_Symbol))
     {
      if(position.TakeProfit()==0)
         return;
      SymbolInfoTick(_Symbol,tick);
      switch(position.PositionType())
        {
         case POSITION_TYPE_BUY:
           {
            n_stop_loss=tick.bid-(position.TakeProfit()-tick.bid);
            if(position.StopLoss()<n_stop_loss-_Point)
              {
               if(!CheckStopLevel(POSITION_TYPE_BUY,n_stop_loss))
                  return;
               trade.PositionModify(_Symbol,n_stop_loss,position.TakeProfit());
              }
            CopyTime(_Symbol,_Period,0,1,time);
            ds.time=time[0];
            de.time=time[0]+PeriodSeconds()*3;
            ds.price=n_stop_loss;
            de.price=n_stop_loss;
            DrawTLine(ds,de,"noose",clrBrown,1);
           };
         break;
         case POSITION_TYPE_SELL:
           {
            n_stop_loss=tick.ask+(tick.ask-position.TakeProfit());
            if(position.StopLoss()>n_stop_loss+_Point)
              {
               if(!CheckStopLevel(POSITION_TYPE_SELL,n_stop_loss))
                  return;
               trade.PositionModify(_Symbol,n_stop_loss,position.TakeProfit());
              }
            CopyTime(_Symbol,_Period,0,1,time);
            ds.time=time[0];
            de.time=time[0]+PeriodSeconds()*3;
            ds.price=n_stop_loss;
            de.price=n_stop_loss;
            DrawTLine(ds,de,"noose",clrTeal,1);
           };
         break;
        }
     }
   else
     {
      if(ObjectFind(0,prefix+"noose")==0)
         ObjectDelete(0,prefix+"noose");
     }
  }
//+------------------------------------------------------------------+
//| CheckTrailing function                                           |
//+------------------------------------------------------------------+
void CheckTrailing(ind_data &data)
  {

   if(position.Select(_Symbol))
     {
      switch(position.PositionType())
        {
         case POSITION_TYPE_BUY:
           {
            if(position.StopLoss()<data.tr_l-_Point || position.StopLoss()==0)
              {
               if(!CheckStopLevel(POSITION_TYPE_BUY,data.tr_l))
                  return;
               trade.PositionModify(_Symbol,data.tr_l,position.TakeProfit());
              }
           };
         break;
         case POSITION_TYPE_SELL:
           {
            if(position.StopLoss()>data.tr_h+_Point || position.StopLoss()==0)
              {
               if(!CheckStopLevel(POSITION_TYPE_SELL,data.tr_h))
                  return;
               trade.PositionModify(_Symbol,data.tr_h,position.TakeProfit());
              }
           };
         break;
        }
     }
  }
//+------------------------------------------------------------------+
//| DrawTLine function                                               |
//+------------------------------------------------------------------+
void DrawTLine(dot &dstr,dot &dend,string title,color l_clr,int width)
  {
   string name=prefix+title;

   ObjectCreate(0,name,OBJ_TREND,0,dstr.time,dstr.price,dend.time,dend.price);
   ObjectSetInteger(0,name,OBJPROP_COLOR,l_clr);
   ObjectSetInteger(0,name,OBJPROP_WIDTH,width);
   ObjectSetInteger(0,name,OBJPROP_RAY_LEFT,false);
   ObjectSetInteger(0,name,OBJPROP_RAY_RIGHT,false);
   ObjectSetInteger(0,name,OBJPROP_HIDDEN,false);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,true);
  }
//+------------------------------------------------------------------+
//| ClearObjects function                                            |
//+------------------------------------------------------------------+
void ClearObjects(string p)
  {
   string obj_name;

   for(int i=ObjectsTotal(0,0,-1)-1;i>=0;i--)
     {
      obj_name=ObjectName(0,i,0,-1);
      if(StringFind(obj_name,p)>=0)
         ObjectDelete(0,obj_name);
     };
  }
//+------------------------------------------------------------------+
