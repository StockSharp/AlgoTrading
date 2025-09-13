//+------------------------------------------------------------------+
//|                                   Martingale Trade Simulator.mq4 |
//|                               Copyright 2023, drdz9876@gmail.com |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, drdz9876@gmail.com"
#property version   "1.2"
#property strict

input const string Initial="//---------------- Initial Trade Settings ----------------//";
input double Lots          = 0.01;  //Lots
input double StopLoss      = 500;   //Stoploss
input double TakeProfit    = 500;   //TakeProfit

input const string Trail="//---------------- Trail Settings ----------------//";
input int TrailingStop     = 50;     //Trailing Stop
input int TrailingStep     = 20;     //Trailing Step

input const string Martingale="//---------------- Martingale Settings ----------------//";
input double nextLot       = 1.2;     //Lot Multiplier
input int  StepPips        = 150;     //Pip Step
input int  TPPlus          = 50;      //TP Average

long chartID = 0;
string prefix = "Martingale Trade Sim";

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(MQLInfoInteger(MQL_TESTER))
     {
      testerPanel();
     }
   else
     {
      Alert("Only works on Strategy Tester Mode");
      return(INIT_FAILED);
     }
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   string lookFor       = prefix;
   ObjectsDeleteAll(chartID,lookFor,0,-1);

   return;
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if(MQLInfoInteger(MQL_TESTER))
     {
      if(MQLInfoInteger(MQL_VISUAL_MODE))
        {
         int send = 0;
         ResetLastError();
         ChartRedraw(chartID);

         long   lparam = 0;
         double dparam = 0.0;
         string sparam1 = "";
         string sparam2 = "";
         string sparam3 = "";
         string sparam4 = "";
         //---
         sparam1 = prefix+"Buy";
         sparam2 = prefix+"Sell";
         sparam3 = prefix+"Martingale";
         sparam4 = prefix+"Trail";
         
         double SLBuy = 0, SLSell = 0, TPBuy = 0, TPSell = 0;
         if(StopLoss > 0)
         {
            SLBuy = Ask - StopLoss*Point();
            SLSell = Bid + StopLoss*Point();
         }
         if(TakeProfit > 0)
         {
            TPBuy = Ask + TakeProfit*Point();
            TPSell = Bid - TakeProfit*Point();
         }
         if(bool(ObjectGetInteger(chartID, sparam1,OBJPROP_STATE,true)))
           {
            OnChartEvent(CHARTEVENT_OBJECT_CLICK, lparam, dparam,sparam1);
            if(CheckMoneyForTrade(Lots,OP_BUY) && CheckVolumeValue(Lots))
               send = OrderSend(Symbol(),OP_BUY,lotAdjust(Lots),Ask,10,SLBuy,TPBuy,"Order Test",0,0,clrBlue);
           }

         if(bool(ObjectGetInteger(chartID, sparam2,OBJPROP_STATE,true)))
           {
            OnChartEvent(CHARTEVENT_OBJECT_CLICK, lparam, dparam,sparam2);
            if(CheckMoneyForTrade(Lots,OP_SELL) && CheckVolumeValue(Lots))
               send = OrderSend(Symbol(),OP_SELL,lotAdjust(Lots),Bid,10,SLSell,TPSell,"Order Test",0,0,clrRed);
           }

         if(bool(ObjectGetInteger(chartID, sparam3,OBJPROP_STATE,true)))
           {
            OnChartEvent(CHARTEVENT_OBJECT_CLICK, lparam, dparam,sparam3);
            AveragingOrders();
           }
         if(bool(ObjectGetInteger(chartID, sparam4,OBJPROP_STATE,true)))
           {
            OnChartEvent(CHARTEVENT_OBJECT_CLICK, lparam, dparam,sparam4);
            TrailingOrders();
           }
        }
     }
   else
     {
      Alert("Only works on Strategy Tester Mode");
      ExpertRemove();
     }

   ChartRedraw(chartID);

   return;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingOrders()
  {
   double ask = SymbolInfoDouble(Symbol(),SYMBOL_ASK);
   double bid = SymbolInfoDouble(Symbol(),SYMBOL_BID);
   double point = SymbolInfoDouble(Symbol(), SYMBOL_POINT);
   int digits = (int)SymbolInfoInteger(Symbol(),SYMBOL_DIGITS);
   bool fm;

   double TS = TrailingStop*point;
   double TST = TrailingStep*point;

   int b = 0,s = 0;
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==0)
              {
               if(OrderType() == OP_BUY)
                 {
                  b++;
                 }
               if(OrderType() == OP_SELL)
                 {
                  s++;
                 }
              }
        }
     }



   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==0)
              {
               if(b == 1)
                 {
                  if(OrderStopLoss() == 0 || (OrderStopLoss() != 0 && OrderStopLoss() < OrderOpenPrice()))
                    {
                     if(bid - OrderOpenPrice() > TS+TST-1*point)
                       {
                        fm = OrderModify(OrderTicket(),OrderOpenPrice(),bid - TS,OrderTakeProfit(),0,clrAqua);
                       }
                    }
                  if(OrderStopLoss() > OrderOpenPrice())
                    {
                     if(bid - OrderStopLoss() > TST+(5*point))
                       {
                        fm = OrderModify(OrderTicket(),OrderOpenPrice(),bid - TST,OrderTakeProfit(),0,clrAqua);
                       }
                    }
                 }
               if(s == 1)
                 {
                  if(OrderStopLoss() == 0 || (OrderStopLoss()!= 0 && OrderStopLoss() > OrderOpenPrice()))
                    {
                     if(OrderOpenPrice() - ask > TS+TST-1*point)
                       {
                        fm = OrderModify(OrderTicket(),OrderOpenPrice(),ask + TS,OrderTakeProfit(),0,clrAqua);
                       }
                    }
                  if(OrderStopLoss() < OrderOpenPrice())
                    {
                     if(OrderStopLoss()-ask > TST+(5*point))
                       {
                        fm = OrderModify(OrderTicket(),OrderOpenPrice(),ask + TST,OrderTakeProfit(),0,clrAqua);
                       }
                    }
                 }
              }
        }
     }

   return;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AveragingOrders()
  {
   double ask = SymbolInfoDouble(Symbol(),SYMBOL_ASK);
   double bid = SymbolInfoDouble(Symbol(),SYMBOL_BID);
   double point = SymbolInfoDouble(Symbol(), SYMBOL_POINT);
   int digits = (int)SymbolInfoInteger(Symbol(),SYMBOL_DIGITS);
   int stopLevel = (int)SymbolInfoInteger(Symbol(),SYMBOL_TRADE_STOPS_LEVEL);
   double tickSize=SymbolInfoDouble(Symbol(),SYMBOL_TRADE_TICK_SIZE);

   double swapB = 0, swapS = 0;

   double
   BuyPriceMax = 0, BuyPriceMin = 0,
   SelPriceMin = 0, SelPriceMax = 0,
   BuyPriceMaxLot = 0, BuyPriceMinLot = 0,
   SelPriceMaxLot = 0, SelPriceMinLot = 0,
   BuyTP = 0, BuySL = 0, BSL = 0, SSL = 0,
   SellTP = 0, SellSL = 0;
   int
   countOpen = 0, buys = 0, sells = 0;
   double nn=0,bb=0, factb = 0, facts = 0;
   double nnn=0,bbb=0;
   for(int i = OrdersTotal() - 1; i >= 0; i--)
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber() == 0)
              {
               countOpen++;

               if(OrderType() == OP_BUY)
                 {
                  buys++;
                  double opB = NormalizeDouble(OrderOpenPrice(), digits);
                  BuyTP = NormalizeDouble(OrderTakeProfit(),digits);
                  BuySL = NormalizeDouble(OrderStopLoss(),digits);
                  double llot=OrderLots();
                  double itog=0;

                  swapB = (OrderSwap() + OrderCommission())/llot*tickSize;

                  itog=(opB - swapB)*llot;

                  bb=bb+itog;
                  nn=nn+llot;

                  factb=bb/nn;

                  if(opB > BuyPriceMax || BuyPriceMax == 0)
                    {
                     BuyPriceMax    = opB;
                     BuyPriceMaxLot = llot;
                    }
                  if(opB < BuyPriceMin || BuyPriceMin == 0)
                    {
                     BuyPriceMin    = opB;
                     BuyPriceMinLot = llot;
                    }
                 }
               if(OrderType() == OP_SELL)
                 {
                  sells++;
                  double opS = NormalizeDouble(OrderOpenPrice(), digits);
                  SellTP = NormalizeDouble(OrderTakeProfit(),digits);
                  SellSL = NormalizeDouble(OrderStopLoss(),digits);
                  double llots=OrderLots();
                  double itogs=0;

                  swapS = (OrderSwap() + OrderCommission())/llots*tickSize;

                  itogs=(opS - swapS)*llots;

                  bbb=bbb+itogs;
                  nnn=nnn+llots;
                  facts=bbb/nnn;

                  if(opS > SelPriceMax || SelPriceMax == 0)
                    {
                     SelPriceMax    = opS;
                     SelPriceMaxLot = llots;
                    }
                  if(opS < SelPriceMin || SelPriceMin == 0)
                    {
                     SelPriceMin    = opS;
                     SelPriceMinLot = llots;
                    }
                 }
              }
        }
   int send = -1;
   double BuyStep = 0, SellStep = 0;
   BuyStep = StepPips * point;
   SellStep = StepPips * point;

   double buyLot = 0, selLot = 0;

   buyLot = lotAdjust(BuyPriceMinLot * MathPow(nextLot,buys));
   selLot = lotAdjust(SelPriceMaxLot * MathPow(nextLot,sells));

   if(buys > 0)
     {
      if(BuyPriceMin - ask >= BuyStep)
        {
         if(CheckMoneyForTrade(buyLot,OP_BUY) && CheckVolumeValue(buyLot))
            send = OrderSend(Symbol(),OP_BUY,buyLot,ask,10,0,0,"Test Order",0,0,clrBlue);
         Print("Buy Average Success");
        }
     }
   if(sells > 0)
     {
      if(bid - SelPriceMax >= SellStep)
        {
         if(CheckMoneyForTrade(selLot,OP_SELL) && CheckVolumeValue(selLot))
            send = OrderSend(Symbol(),OP_SELL,selLot,bid,10,0,0,"Test Order",0,0,clrRed);
         Print("Sell Average Success");
        }
     }

   double TPAverage = TPPlus;
   double CORR = 0;
   CORR = NormalizeDouble((TPAverage + stopLevel) * point,digits);

   for(int j=OrdersTotal()-1; j>=0; j--)
     {
      if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES))
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber() == 0)
              {
               if(buys >=2 && OrderType()==OP_BUY)
                 {
                  if(!compareDoubles(factb+CORR,OrderTakeProfit()))
                  modifyAllSLTP(OP_BUY,BuySL,factb+CORR);
                 }
               if(sells >= 2 && OrderType()==OP_SELL)
                 {
                  if(!compareDoubles(facts-CORR,OrderTakeProfit()))
                  modifyAllSLTP(OP_SELL,SellSL,facts-CORR);
                 }
              }
     }
   return;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double lotAdjust(double lots)
  {
   double value = 0;
   double lotStep = SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   double minLot  = SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   double maxLot  = SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   value          = NormalizeDouble(lots/lotStep,0) * lotStep;

   value = MathMax(MathMin(maxLot, value), minLot);

   return(value);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool modifyAllSLTP(ENUM_ORDER_TYPE type, double SL, double TP)
  {
   int digits = (int)SymbolInfoInteger(Symbol(),SYMBOL_DIGITS);
   for(int j=OrdersTotal()-1; j>=0; j--)
     {
      if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES))
         if(OrderSymbol() == Symbol())
            if(OrderMagicNumber() == 0)
               if(OrderType() == type)
                 {
                  if(SL != 0)
                     if(!compareDoubles(NormalizeDouble(SL,digits),NormalizeDouble(OrderStopLoss(),digits)))
                       {
                        if(!OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(SL,digits),OrderTakeProfit(),0,clrGreenYellow))
                           Print("Error Modify Stoploss "+IntegerToString(GetLastError()));
                        else
                           return(true);
                       }
                  if(TP != 0)
                     if(!compareDoubles(NormalizeDouble(TP,digits),NormalizeDouble(OrderTakeProfit(),digits)))
                       {
                        if(!OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(TP,digits),0,clrGreenYellow))
                           Print("Error Modify Takeprofit "+IntegerToString(GetLastError()));
                        else
                           return(true);
                       }
                 }
     }
   return(false);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool compareDoubles(double val1, double val2)
  {
   int digits = (int)SymbolInfoInteger(Symbol(),SYMBOL_DIGITS);
   if(NormalizeDouble(val1 - val2,digits-1)==0)
      return (true);

   return(false);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(double lots, int type)
  {
   double free_margin = AccountFreeMarginCheck(Symbol(),type,lots);
//-- if there is not enough money
   if(free_margin<0)
     {
      string oper=(type==OP_BUY)? "Buy":"Sell";
      Print("Not enough money for ", oper," ",lots, " ", Symbol(), " Error code=",GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      Print("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      Print("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      Print("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
            volume_step,ratio*volume_step);
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
//---
   if(id == CHARTEVENT_OBJECT_CLICK)
     {
      if(sparam == prefix+"Buy")
         if(bool(ObjectGetInteger(chartID, sparam,OBJPROP_STATE,true)))
           {
            ObjectSetInteger(chartID,prefix+"Buy",OBJPROP_STATE,false);
           }
      if(sparam == prefix+"Sell")
         if(bool(ObjectGetInteger(chartID, sparam,OBJPROP_STATE,true)))
           {
            ObjectSetInteger(chartID,prefix+"Sell",OBJPROP_STATE,false);
           }
      if(sparam == prefix+"Martingale")
        {
         if(bool(ObjectGetInteger(chartID, sparam,OBJPROP_STATE,false)))
           {
            ObjectSetInteger(chartID,prefix+"Martingale",OBJPROP_STATE,true);
           }
        }
      if(sparam == prefix+"Trail")
        {
         if(bool(ObjectGetInteger(chartID, sparam,OBJPROP_STATE,false)))
           {
            ObjectSetInteger(chartID,prefix+"Trail",OBJPROP_STATE,true);
           }
        }
     }

   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool createBackground(string name)
  {
   ObjectCreate(chartID, name, OBJ_RECTANGLE_LABEL, 0, 0, 0);
   ObjectSetInteger(chartID,name, OBJPROP_CORNER, CORNER_LEFT_UPPER);
   ObjectSetInteger(chartID, name, OBJPROP_XDISTANCE, 20);
   ObjectSetInteger(chartID, name, OBJPROP_YDISTANCE, 40);
   ObjectSetInteger(chartID, name, OBJPROP_XSIZE, 140);
   ObjectSetInteger(chartID, name, OBJPROP_YSIZE, 30);
   ObjectSetInteger(chartID, name, OBJPROP_BGCOLOR, clrGray);
   ObjectSetInteger(chartID, name, OBJPROP_BORDER_COLOR, clrBlack);
   ObjectSetInteger(chartID, name, OBJPROP_BORDER_TYPE, BORDER_RAISED);
   ObjectSetInteger(chartID, name, OBJPROP_WIDTH, 0);
   ObjectSetInteger(chartID, name, OBJPROP_BACK, false);
   ObjectSetInteger(chartID, name, OBJPROP_SELECTABLE, false);
   ObjectSetInteger(chartID, name, OBJPROP_HIDDEN, true);
   return (true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool createBackground2(string name)
  {
   ObjectCreate(chartID, name, OBJ_RECTANGLE_LABEL, 0, 0, 0);
   ObjectSetInteger(chartID,name, OBJPROP_CORNER, CORNER_LEFT_UPPER);
   ObjectSetInteger(chartID, name, OBJPROP_XDISTANCE, 20);
   ObjectSetInteger(chartID, name, OBJPROP_YDISTANCE, 80);
   ObjectSetInteger(chartID, name, OBJPROP_XSIZE, 140);
   ObjectSetInteger(chartID, name, OBJPROP_YSIZE, 30);
   ObjectSetInteger(chartID, name, OBJPROP_BGCOLOR, clrDarkGreen);
   ObjectSetInteger(chartID, name, OBJPROP_BORDER_COLOR, clrBlack);
   ObjectSetInteger(chartID, name, OBJPROP_BORDER_TYPE, BORDER_RAISED);
   ObjectSetInteger(chartID, name, OBJPROP_WIDTH, 0);
   ObjectSetInteger(chartID, name, OBJPROP_BACK, false);
   ObjectSetInteger(chartID, name, OBJPROP_SELECTABLE, false);
   ObjectSetInteger(chartID, name, OBJPROP_HIDDEN, true);
   return (true);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool createBackground3(string name)
  {
   ObjectCreate(chartID, name, OBJ_RECTANGLE_LABEL, 0, 0, 0);
   ObjectSetInteger(chartID,name, OBJPROP_CORNER, CORNER_LEFT_UPPER);
   ObjectSetInteger(chartID, name, OBJPROP_XDISTANCE, 20);
   ObjectSetInteger(chartID, name, OBJPROP_YDISTANCE, 120);
   ObjectSetInteger(chartID, name, OBJPROP_XSIZE, 140);
   ObjectSetInteger(chartID, name, OBJPROP_YSIZE, 30);
   ObjectSetInteger(chartID, name, OBJPROP_BGCOLOR, clrDarkGoldenrod);
   ObjectSetInteger(chartID, name, OBJPROP_BORDER_COLOR, clrBlack);
   ObjectSetInteger(chartID, name, OBJPROP_BORDER_TYPE, BORDER_RAISED);
   ObjectSetInteger(chartID, name, OBJPROP_WIDTH, 0);
   ObjectSetInteger(chartID, name, OBJPROP_BACK, false);
   ObjectSetInteger(chartID, name, OBJPROP_SELECTABLE, false);
   ObjectSetInteger(chartID, name, OBJPROP_HIDDEN, true);
   return (true);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool createButton(string name,string text,int xSize,int ySize,int x,int y,int size, int clr, int bgColor, bool state)
  {
   ObjectCreate(chartID,name, OBJ_BUTTON, 0, 0, 0);
   ObjectSetInteger(chartID,name, OBJPROP_CORNER, CORNER_LEFT_UPPER);
   ObjectSetInteger(chartID, name, OBJPROP_XSIZE, xSize);
   ObjectSetInteger(chartID, name, OBJPROP_YSIZE, ySize);
   ObjectSetInteger(chartID,name, OBJPROP_XDISTANCE, x);
   ObjectSetInteger(chartID,name, OBJPROP_YDISTANCE, 30+y);
   ObjectSetString(chartID,name,OBJPROP_TEXT, text);
   ObjectSetInteger(chartID, name, OBJPROP_BACK, false);
   ObjectSetInteger(chartID,name,OBJPROP_FONTSIZE,size);
   ObjectSetInteger(chartID,name,OBJPROP_COLOR,clr);
   ObjectSetInteger(chartID,name,OBJPROP_STATE,state);
   ObjectSetInteger(chartID,name,OBJPROP_ZORDER,100);
   ObjectSetInteger(chartID, name, OBJPROP_BGCOLOR, bgColor);

   return (true);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void testerPanel()
  {
   createBackground(prefix+"Background");
   createButton(prefix+"Buy","Buy",50,20,30,15,8,clrWhite,clrBlue,false);
   createButton(prefix+"Sell","Sell",50,20,100,15,8,clrWhite,clrRed,false);
   createBackground2(prefix+"Background2");
   createButton(prefix+"Martingale","Enable Martingale",120,20,30,55,8,clrBlack,clrLightGreen,true);
   createBackground3(prefix+"Background3");
   createButton(prefix+"Trail","Enable Trail",120,20,30,95,8,clrBlack,clrGold,true);
   return;
  }
//+------------------------------------------------------------------+
