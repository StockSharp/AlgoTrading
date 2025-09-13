//+------------------------------------------------------------------+
//|                                                         CloseAll |
//|                                   Copyright 2022, Forex Bonnitta |
//|                                        https://t.me/BestAdvisors |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, Forex Bonnitta"
#property link        "https://t.me/BestAdvisors"
#property description "Due to recent popularity of Multi currencies EA, This codes allows to Close Orders or delete Pending orders of a Multi Currencies EA, Single Currency or Manual orders. Help : https://t.me/BestAdvisors"
#property version     "1.00"
#property strict

enum Mode
  {
   CloseALL,
   CloseBUY,
   CloseSELL,
   CloseCurrency,
   CloseMagic,
   CloseTicket,
   ClosePendingByMagic,
   ClosePendingByMagicCurrency,
   CloseALLandPendingByMagic,
   ClosePending,
   CloseALLandPending
  };

input string EAComment = "Bonnitta EA";
input Mode TypeOfClose = CloseALL;
input string Currency  = "";
input int Magic_Ticket = 1;

//+------------------------------------------------------------------+
//|                                                                  |
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
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
  {
   int ticket=0;
   if(sparam=="CloseButton") // Close button has been pressed
     {
      int total = OrdersTotal();
      for(int i=total-1; i>=0; i--)
        {
         if(OrderSelect(i,SELECT_BY_POS))
           {
            if(EAComment !="")
              {
               string cmmt = OrderComment();
               cmmt= StringTrimLeft(cmmt);
               cmmt= StringTrimRight(cmmt);

               if(StringFind(cmmt,EAComment) < 0)
                  continue;
              }
            if(TypeOfClose==CloseALL)
              {
               if(OrderType()==OP_SELL)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5);
               if(OrderType()== OP_BUY)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5);
               //Print("Order : "+i+" TicketClose: "+ticket);
              }

            if(TypeOfClose==CloseBUY)
              {
               if(OrderType()== OP_BUY)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5);
              }

            if(TypeOfClose==CloseSELL)
              {
               if(OrderType()==OP_SELL)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5);
              }

            if(TypeOfClose==CloseMagic && OrderMagicNumber()== Magic_Ticket)
              {
               if(OrderType()==OP_SELL)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5);
               if(OrderType()== OP_BUY)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5);
              }

            if(TypeOfClose==CloseTicket && OrderTicket()== Magic_Ticket)
              {
               if(OrderType()==OP_SELL)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5);
               if(OrderType()== OP_BUY)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5);
              }

            if(TypeOfClose==CloseCurrency)
              {
               if(Currency  == "" && OrderSymbol()==Symbol())
                 {
                  if(OrderType()==OP_SELL)
                     ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5);
                  if(OrderType()== OP_BUY)
                     ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5);
                 }

               if(Currency  != "" && OrderSymbol()==Currency)
                 {
                  if(OrderType()==OP_SELL)
                     ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5);
                  if(OrderType()== OP_BUY)
                     ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5);
                 }
              }

            if(TypeOfClose==ClosePendingByMagic && OrderMagicNumber()== Magic_Ticket)
              {
               if(OrderType()==OP_SELLSTOP)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_BUYSTOP)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()==OP_SELLLIMIT)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_BUYLIMIT)
                  ticket = OrderDelete(OrderTicket());
              }

            if(TypeOfClose==ClosePending)
              {
               if(OrderType()== OP_SELLSTOP)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_BUYSTOP)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_SELLLIMIT)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_BUYLIMIT)
                  ticket = OrderDelete(OrderTicket());
              }

            if(TypeOfClose==CloseALLandPending)
              {
               if(OrderType()== OP_SELLSTOP)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_BUYSTOP)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_SELLLIMIT)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_BUYLIMIT)
                  ticket = OrderDelete(OrderTicket());

               if(OrderType()==OP_SELL)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5);
               if(OrderType()== OP_BUY)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5);

              }

            if(TypeOfClose==CloseALLandPendingByMagic && OrderMagicNumber()== Magic_Ticket)
              {
               if(OrderType()== OP_SELLSTOP)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_BUYSTOP)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_SELLLIMIT)
                  ticket = OrderDelete(OrderTicket());
               if(OrderType()== OP_BUYLIMIT)
                  ticket = OrderDelete(OrderTicket());

               if(OrderType()==OP_SELL)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5);
               if(OrderType()== OP_BUY)
                  ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5);

              }

            if(TypeOfClose==ClosePendingByMagicCurrency  && OrderMagicNumber()== Magic_Ticket)
              {
               if(Currency  == "" && OrderSymbol()==Symbol())
                 {
                  if(OrderType()== OP_SELLSTOP)
                     ticket = OrderDelete(OrderTicket());
                  if(OrderType()== OP_BUYSTOP)
                     ticket = OrderDelete(OrderTicket());
                  if(OrderType()== OP_SELLLIMIT)
                     ticket = OrderDelete(OrderTicket());
                  if(OrderType()== OP_BUYLIMIT)
                     ticket = OrderDelete(OrderTicket());
                 }

               if(Currency  != "" && OrderSymbol()==Currency)
                 {
                  if(OrderType()== OP_SELLSTOP)
                     ticket = OrderDelete(OrderTicket());
                  if(OrderType()== OP_BUYSTOP)
                     ticket = OrderDelete(OrderTicket());
                  if(OrderType()== OP_SELLLIMIT)
                     ticket = OrderDelete(OrderTicket());
                  if(OrderType()== OP_BUYLIMIT)
                     ticket = OrderDelete(OrderTicket());
                 }
              }
           }
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
