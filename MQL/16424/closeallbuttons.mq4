//+------------------------------------------------------------------+
//|                                              CloseAllButtons.mq4 |
//|                                       Copyright 2016, soubra2003 |
//|                         https://www.mql5.com/en/users/soubra2003 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, Soubra2003"
#property link      "https://www.mql5.com/en/users/soubra2003/seller"
#property version   "1.00"
#property strict

//for button(s)
bool   EnableTrade=true;  //enable/disable Button
int    TradeButtonLoc   = 0;
string TradeObjName     = "Trade Button0";
string BtnName          = "BuyStp";
//
bool   EnableTrade1=true;  //enable/disable Button
int    TradeButtonLoc1   = 25;
string TradeObjName1     = "Trade Button1";
string BtnName1          = "SellStp";
//
bool   EnableTrade2=true;  //enable/disable Button
int    TradeButtonLoc2   = 50;
string TradeObjName2     = "Trade Button2";
string BtnName2          = "ALL";
//+------------------------------------------------------------------+
//| Expert initialization function
//+------------------------------------------------------------------+
int OnInit()
  {
   TradingButton();
   TradingButton1();
   TradingButton2();

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| ChartEvent function
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   if(sparam==TradeObjName) TradeButtonClick();
   if(sparam==TradeObjName1) TradeButtonClick1();
   if(sparam==TradeObjName2) TradeButtonClick2();
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   for(int cnt=0; cnt<=2; cnt++)
      ObjectDelete(ChartID(),"Trade Button"+(string)cnt);

//---
   Print("Bye. Skype: onesoubra");
  }
//+------------------------------------------------------------------+
//| Expert tick function
//+------------------------------------------------------------------+
void OnTick()
  {

  }
//+------------------------------------------------------------------+
//| TradeButtonClick function
//+------------------------------------------------------------------+
void TradeButtonClick()
  {
   ObjectSetInteger(0,TradeObjName,OBJPROP_STATE,false);
   ObjectSet(TradeObjName,OBJPROP_YDISTANCE,-30); //set distance property
   CloseAllBuyStop();
   ObjectSet(TradeObjName,OBJPROP_YDISTANCE,TradeButtonLoc); //set distance property
   ObjectSetInteger(0,TradeObjName,OBJPROP_STATE,true);
  }
//+------------------------------------------------------------------+
//| Expert TradingButton function
//+------------------------------------------------------------------+
void TradingButton()
  {
   long   current_chart_id=ChartID();

   ObjectCreate(current_chart_id,TradeObjName,OBJ_BUTTON,0,0,0); //creating label object (it does not have time/price coordinates)
   ObjectSetInteger(current_chart_id,TradeObjName,OBJPROP_COLOR,clrRed); //set color to Red
   ObjectSetString(current_chart_id,TradeObjName,OBJPROP_TEXT,BtnName); //set text property
   ObjectSet(TradeObjName,OBJPROP_YDISTANCE,TradeButtonLoc); //set distance property
   ObjectSetInteger(current_chart_id,TradeObjName,OBJPROP_COLOR,clrBlue); //set color to Blue
  }
//+------------------------------------------------------------------+
//| TradeButtonClick function
//+------------------------------------------------------------------+
void TradeButtonClick1()
  {
   ObjectSetInteger(0,TradeObjName1,OBJPROP_STATE,false);
   ObjectSet(TradeObjName1,OBJPROP_YDISTANCE,-30); //set distance property
   CloseAllSellStop();
   ObjectSet(TradeObjName1,OBJPROP_YDISTANCE,TradeButtonLoc1); //set distance property
   ObjectSetInteger(0,TradeObjName1,OBJPROP_STATE,true);
  }
//+------------------------------------------------------------------+
//| Expert TradingButton function
//+------------------------------------------------------------------+
void TradingButton1()
  {
   long   current_chart_id=ChartID();

   ObjectCreate(current_chart_id,TradeObjName1,OBJ_BUTTON,0,0,0); //creating label object (it does not have time/price coordinates)
   ObjectSetInteger(current_chart_id,TradeObjName1,OBJPROP_COLOR,clrRed); //set color to Red
   ObjectSetString(current_chart_id,TradeObjName1,OBJPROP_TEXT,BtnName1); //set text property
   ObjectSet(TradeObjName1,OBJPROP_YDISTANCE,TradeButtonLoc1); //set distance property
   ObjectSetInteger(current_chart_id,TradeObjName1,OBJPROP_COLOR,clrBlue); //set color to Blue
  }
//+------------------------------------------------------------------+
//| TradeButtonClick function
//+------------------------------------------------------------------+
void TradeButtonClick2()
  {
   ObjectSetInteger(0,TradeObjName2,OBJPROP_STATE,false);
   ObjectSet(TradeObjName2,OBJPROP_YDISTANCE,-30); //set distance property
   CloseAll();
   ObjectSet(TradeObjName2,OBJPROP_YDISTANCE,TradeButtonLoc2); //set distance property
   ObjectSetInteger(0,TradeObjName2,OBJPROP_STATE,true);
  }
//+------------------------------------------------------------------+
//| Expert TradingButton function
//+------------------------------------------------------------------+
void TradingButton2()
  {
   long   current_chart_id=ChartID();

   ObjectCreate(current_chart_id,TradeObjName2,OBJ_BUTTON,0,0,0); //creating label object (it does not have time/price coordinates)
   ObjectSetInteger(current_chart_id,TradeObjName2,OBJPROP_COLOR,clrRed); //set color to Red
   ObjectSetString(current_chart_id,TradeObjName2,OBJPROP_TEXT,BtnName2); //set text property
   ObjectSet(TradeObjName2,OBJPROP_YDISTANCE,TradeButtonLoc2); //set distance property
   ObjectSetInteger(current_chart_id,TradeObjName2,OBJPROP_COLOR,clrBlue); //set color to Blue
  }
//+------------------------------------------------------------------+
//| CLOSE ALL OPENED POSITIONS
//+------------------------------------------------------------------+
void CloseAllBuyStop()
  {
   int total= OrdersTotal();
   for(int i=total-1; i>=0; i--)
     {
      int  ticket=OrderSelect(i,SELECT_BY_POS);
      if(OrderType()==OP_BUYSTOP)
        {
         //Delete opened buystop orders
         bool result=OrderDelete(OrderTicket(),clrNONE);
         if(result==false)
           {
            Print("Order ",OrderTicket()," failed to close. Error: ",GetLastError());
            Sleep(300);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| CLOSE ALL OPENED POSITIONS
//+------------------------------------------------------------------+
void CloseAllSellStop()
  {
   int total= OrdersTotal();
   for(int i=total-1; i>=0; i--)
     {
      int  ticket=OrderSelect(i,SELECT_BY_POS);
      if(OrderType()==OP_SELLSTOP)
        {
         //Delete opened buystop orders
         bool result=OrderDelete(OrderTicket(),clrNONE);
         if(result==false)
           {
            Print("Order ",OrderTicket()," failed to close. Error: ",GetLastError());
            Sleep(300);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| CLOSE ALL OPENED POSITIONS
//+------------------------------------------------------------------+
void CloseAll()
  {
   int total= OrdersTotal();
   for(int i=total-1; i>=0; i--)
     {
      int  ticket = OrderSelect(i,SELECT_BY_POS);
      int  type   = OrderType();
      bool result = false;

      switch(type)
        {
         //Delete opened buystop orders
         case OP_BUYSTOP  : result=OrderDelete(OrderTicket(),clrNONE);
         break;

         //Delete opened buylimit orders
         case OP_BUYLIMIT  : result=OrderDelete(OrderTicket(),clrNONE);
         break;

         //Close opened long positions
         case OP_BUY  : result=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5,clrNONE);

         //Close opened short positions
         case OP_SELL : result=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5,clrNONE);
         break;

         //Delete opened sellstop orders
         case OP_SELLSTOP : result=OrderDelete(OrderTicket(),clrNONE);
         break;

         //Delete opened selllimit orders
         case OP_SELLLIMIT : result=OrderDelete(OrderTicket(),clrNONE);
        }

      if(result==false)
        {
         Print("Order ",OrderTicket()," failed to close. Error: ",GetLastError());
         Sleep(100);
        }
     }
  }


//+------------------------------------------------------------------+
//Bye
