//+------------------------------------------------------------------+
//|                                   Martingale Trade Simulator.mq5 |
//|                               Copyright 2023, drdz9876@gmail.com |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, drdz9876@gmail.com"
#property version   "1.0"
#property strict
#include <Trade\Trade.mqh> CTrade trade;

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
   trade.SetExpertMagicNumber(0);
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
   double ask = SymbolInfoDouble(Symbol(),SYMBOL_ASK);
   double bid = SymbolInfoDouble(Symbol(),SYMBOL_BID);
   double point = SymbolInfoDouble(Symbol(),SYMBOL_POINT);
   int digits =(int)SymbolInfoInteger(Symbol(),SYMBOL_DIGITS);
//---
   if(MQLInfoInteger(MQL_TESTER))
     {
      if(MQLInfoInteger(MQL_VISUAL_MODE))
        {

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
            SLBuy = ask - StopLoss*Point();
            SLSell = bid + StopLoss*Point();
           }
         if(TakeProfit > 0)
           {
            TPBuy = ask + TakeProfit*Point();
            TPSell = bid - TakeProfit*Point();
           }
         if(bool(ObjectGetInteger(chartID, sparam1,OBJPROP_STATE,true)))
           {
            OnChartEvent(CHARTEVENT_OBJECT_CLICK, lparam, dparam,sparam1);
            if(CheckMoneyForTrade(Lots,ORDER_TYPE_BUY) && CheckVolumeValue(Lots))
               if(!trade.Buy(lotAdjust(Lots),Symbol(),SymbolInfoDouble(Symbol(),SYMBOL_ASK),SLBuy,TPBuy,"Order Test"))
                  Print("Error to Place Buy Orders "+IntegerToString(GetLastError()));
           }

         if(bool(ObjectGetInteger(chartID, sparam2,OBJPROP_STATE,true)))
           {
            OnChartEvent(CHARTEVENT_OBJECT_CLICK, lparam, dparam,sparam2);
            if(CheckMoneyForTrade(Lots,ORDER_TYPE_SELL) && CheckVolumeValue(Lots))
               if(!trade.Sell(lotAdjust(Lots),Symbol(),SymbolInfoDouble(Symbol(),SYMBOL_BID),SLSell,TPSell,"Order Test"))
                  Print("Error to Place Sell Orders "+IntegerToString(GetLastError()));
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

   double TS = TrailingStop*point;
   double TST = TrailingStep*point;

   int b = 0,s = 0;
   for(int i=0; i<PositionsTotal(); i++)
     {
      if(PositionGetTicket(i) > 0)
        {
         if(PositionGetString(POSITION_SYMBOL) == Symbol())
            if(PositionGetInteger(POSITION_MAGIC)==0)
              {
               if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY)
                 {
                  b++;
                 }
               if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL)
                 {
                  s++;
                 }
              }
        }
     }



   for(int i=0; i<PositionsTotal(); i++)
     {
      if(PositionGetTicket(i) > 0)
        {
         if(PositionGetString(POSITION_SYMBOL) == Symbol())
            if(PositionGetInteger(POSITION_MAGIC)==0)
              {
               if(b == 1)
                 {
                  if(PositionGetDouble(POSITION_SL) == 0 || (PositionGetDouble(POSITION_SL) != 0 && PositionGetDouble(POSITION_SL) < PositionGetDouble(POSITION_PRICE_OPEN)))
                    {
                     if(bid - PositionGetDouble(POSITION_PRICE_OPEN) > TS+TST-1*point)
                       {
                        if(!trade.PositionModify(PositionGetInteger(POSITION_TICKET),bid-TS,PositionGetDouble(POSITION_TP)))
                           Print("Error to Trail "+IntegerToString(GetLastError()));
                       }
                    }
                  if(PositionGetDouble(POSITION_SL) > PositionGetDouble(POSITION_PRICE_OPEN))
                    {
                     if(bid - PositionGetDouble(POSITION_SL) > TST+(5*point))
                       {
                        if(!trade.PositionModify(PositionGetInteger(POSITION_TICKET),bid-TST,PositionGetDouble(POSITION_TP)))
                           Print("Error to Trail "+IntegerToString(GetLastError()));
                       }
                    }
                 }
               if(s == 1)
                 {
                  if(PositionGetDouble(POSITION_SL) == 0 || (PositionGetDouble(POSITION_SL)!= 0 && PositionGetDouble(POSITION_SL) > PositionGetDouble(POSITION_PRICE_OPEN)))
                    {
                     if(PositionGetDouble(POSITION_PRICE_OPEN) - ask > TS+TST-1*point)
                       {
                        if(!trade.PositionModify(PositionGetInteger(POSITION_TICKET),ask+TS,PositionGetDouble(POSITION_TP)))
                           Print("Error to Trail "+IntegerToString(GetLastError()));
                       }
                    }
                  if(PositionGetDouble(POSITION_SL) < PositionGetDouble(POSITION_PRICE_OPEN))
                    {
                     if(PositionGetDouble(POSITION_SL)-ask > TST+(5*point))
                       {
                        if(!trade.PositionModify(PositionGetInteger(POSITION_TICKET),ask+TST,PositionGetDouble(POSITION_TP)))
                           Print("Error to Trail "+IntegerToString(GetLastError()));
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
   for(int i = PositionsTotal() - 1; i >= 0; i--)
      if(PositionGetTicket(i) > 0)
        {
         if(PositionGetString(POSITION_SYMBOL) == Symbol())
            if(PositionGetInteger(POSITION_MAGIC)==0)
              {
               countOpen++;

               if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY)
                 {
                  buys++;
                  double opB = NormalizeDouble(PositionGetDouble(POSITION_PRICE_OPEN), digits);
                  BuyTP = NormalizeDouble(PositionGetDouble(POSITION_TP),digits);
                  BuySL = NormalizeDouble(PositionGetDouble(POSITION_SL),digits);
                  double llot=PositionGetDouble(POSITION_VOLUME);
                  double itog=0;

                  swapB = (PositionGetDouble(POSITION_SWAP) + PositionGetDouble(POSITION_COMMISSION))/llot*tickSize;

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
               if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL)
                 {
                  sells++;
                  double opS = NormalizeDouble(PositionGetDouble(POSITION_PRICE_OPEN), digits);
                  SellTP = NormalizeDouble(PositionGetDouble(POSITION_TP),digits);
                  SellSL = NormalizeDouble(PositionGetDouble(POSITION_SL),digits);
                  double llots=PositionGetDouble(POSITION_VOLUME);
                  double itogs=0;

                  swapS = (PositionGetDouble(POSITION_SWAP) + PositionGetDouble(POSITION_COMMISSION))/llots*tickSize;

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
         if(CheckMoneyForTrade(buyLot,ORDER_TYPE_BUY) && CheckVolumeValue(buyLot))
            if(!trade.Buy(lotAdjust(buyLot),Symbol(),SymbolInfoDouble(Symbol(),SYMBOL_ASK),0,0,"Order Test"))
               Print("Buy Average Failed "+IntegerToString(GetLastError()));
        }
     }
   if(sells > 0)
     {
      if(bid - SelPriceMax >= SellStep)
        {
         if(CheckMoneyForTrade(selLot,ORDER_TYPE_SELL) && CheckVolumeValue(selLot))
            if(!trade.Sell(lotAdjust(selLot),Symbol(),SymbolInfoDouble(Symbol(),SYMBOL_BID),0,0,"Order Test"))
               Print("Sell Average Failed "+IntegerToString(GetLastError()));
        }
     }

   double TPAverage = TPPlus;
   double CORR = 0;
   CORR = NormalizeDouble((TPAverage + stopLevel) * point,digits);

   for(int j=PositionsTotal()-1; j>=0; j--)
     {
      if(PositionGetTicket(j) > 0)
         if(PositionGetString(POSITION_SYMBOL) == Symbol())
            if(PositionGetInteger(POSITION_MAGIC)==0)
              {
               if(buys >=2 && PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
                 {
                  if(!compareDoubles(factb+CORR,PositionGetDouble(POSITION_TP)))
                     modifyAllSLTP(POSITION_TYPE_BUY,BuySL,factb+CORR);
                 }
               if(sells >= 2 && PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
                 {
                  if(!compareDoubles(facts-CORR,PositionGetDouble(POSITION_TP)))
                     modifyAllSLTP(POSITION_TYPE_SELL,SellSL,facts-CORR);
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
bool modifyAllSLTP(ENUM_POSITION_TYPE type, double SL, double TP)
  {
   int digits = (int)SymbolInfoInteger(Symbol(),SYMBOL_DIGITS);
   for(int j=PositionsTotal()-1; j>=0; j--)
     {
      if(PositionGetTicket(j) > 0)
         if(PositionGetString(POSITION_SYMBOL) == Symbol())
            if(PositionGetInteger(POSITION_MAGIC)==0)
               if(PositionGetInteger(POSITION_TYPE) == type)
                 {
                  if(SL != 0)
                     if(!compareDoubles(NormalizeDouble(SL,digits),NormalizeDouble(PositionGetDouble(POSITION_SL),digits)))
                       {
                        if(!trade.PositionModify(PositionGetInteger(POSITION_TICKET),NormalizeDouble(SL,digits),PositionGetDouble(POSITION_TP)))
                           Print("Error Modify Stoploss "+IntegerToString(GetLastError()));
                        else
                           return(true);
                       }
                  if(TP != 0)
                     if(!compareDoubles(NormalizeDouble(TP,digits),NormalizeDouble(PositionGetDouble(POSITION_TP),digits)))
                       {
                        if(!trade.PositionModify(PositionGetInteger(POSITION_TICKET),PositionGetDouble(POSITION_SL),NormalizeDouble(TP,digits)))
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
bool CheckMoneyForTrade(double lots,ENUM_ORDER_TYPE type)
  {
//--- Getting the opening price
   MqlTick mqltick;
   SymbolInfoTick(Symbol(),mqltick);
   double price=mqltick.ask;
   if(type==ORDER_TYPE_SELL)
      price=mqltick.bid;
//--- values of the required and free margin
   double margin,free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
//--- call of the checking function
   if(!OrderCalcMargin(type,Symbol(),lots,price,margin))
     {
      //--- something went wrong, report and return false
      return(false);
     }
//--- if there are insufficient funds to perform the operation
   if(margin>free_margin)
     {
      //--- report the error and return false
      return(false);
     }
//--- checking successful
   return(true);
  }
//************************************************************************************************/
bool CheckVolumeValue(double volume)
  {

   double min_volume = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN);
   if(volume < min_volume)
      return(false);

   double max_volume = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX);
   if(volume > max_volume)
      return(false);

   double volume_step = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_STEP);

   int ratio = (int)MathRound(volume / volume_step);
   if(MathAbs(ratio * volume_step - volume) > 0.0000001)
      return(false);

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
