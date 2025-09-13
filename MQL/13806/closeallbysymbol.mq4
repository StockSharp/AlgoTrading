//+------------------------------------------------------------------+
//|                                                    Close All.mq4 |
//|                                 Copyright 2015, @traderconfident |
//|                            https://confident-trader.blogspot.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, @traderconfident"
#property link      "https://confident-trader.blogspot.com"
#property version   "1.1"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//| Modified date:                                                   |
//| Version 1.0 to 1.1 [10 Sep 2015] :                               |
//| Add return value to OrderClose                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   ObjectCreate(0,"CloseButton",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"CloseButton",OBJPROP_XDISTANCE,15);
   ObjectSetInteger(0,"CloseButton",OBJPROP_YDISTANCE,15);
   ObjectSetInteger(0,"CloseButton",OBJPROP_XSIZE,100);
   ObjectSetInteger(0,"CloseButton",OBJPROP_YSIZE,25);
//---
   ObjectSetString(0,"CloseButton",OBJPROP_TEXT,"Close Orders");
//---
   ObjectSetInteger(0,"CloseButton",OBJPROP_COLOR,White);
   ObjectSetInteger(0,"CloseButton",OBJPROP_BGCOLOR,Red);
   ObjectSetInteger(0,"CloseButton",OBJPROP_BORDER_COLOR,Red);
   ObjectSetInteger(0,"CloseButton",OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,"CloseButton",OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,"CloseButton",OBJPROP_STATE,false);
   ObjectSetInteger(0,"CloseButton",OBJPROP_FONTSIZE,12);
//--- Exit
   ObjectCreate(0,"Exit",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"Exit",OBJPROP_XDISTANCE,120);
   ObjectSetInteger(0,"Exit",OBJPROP_YDISTANCE,15);
   ObjectSetInteger(0,"Exit",OBJPROP_XSIZE,80);
   ObjectSetInteger(0,"Exit",OBJPROP_YSIZE,25);
//---
   ObjectSetString(0,"Exit",OBJPROP_TEXT,"Exit");
//---
   ObjectSetInteger(0,"Exit",OBJPROP_COLOR,White);
   ObjectSetInteger(0,"Exit",OBJPROP_BGCOLOR,Green);
   ObjectSetInteger(0,"Exit",OBJPROP_BORDER_COLOR,Green);
   ObjectSetInteger(0,"Exit",OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,"Exit",OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,"Exit",OBJPROP_STATE,false);
   ObjectSetInteger(0,"Exit",OBJPROP_FONTSIZE,12);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   int _ticket=0;
   if(sparam=="CloseButton") // Close button has been pressed
     {
      int total=OrdersTotal();
      int i = 0;
      for(i = total; i >=0; i--)
        {
         if(OrderSelect(i,SELECT_BY_POS) &&  OrderSymbol()==Symbol())
           {
            //OrderSelect(i,SELECT_BY_POS);
            if(OrderType()==OP_BUY || OrderType()==OP_SELL)
              {
               _ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_ASK),5);
               _ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_BID),5);
              }
           }
        }
      if(_ticket>0)
        {
         ObjectSetInteger(0,"CloseButton",OBJPROP_STATE,false);
         ObjectsDeleteAll();
         ExpertRemove();
        }
     }
   if(sparam=="Exit")
     {
      ObjectSetInteger(0,"Exit",OBJPROP_STATE,false);
      ObjectsDeleteAll();
      ExpertRemove();
     }
  }
//+------------------------------------------------------------------+
