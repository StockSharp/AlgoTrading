//+------------------------------------------------------------------+
//|                                                           RRS EA |
//|                   Copyright 2024, RRS Classic & Virtual Trailing |
//|                                             rajeeevrrs@gmail.com |
//+------------------------------------------------------------------+
#property copyright "RRS Classic & Virtual Trailing"
#property link      "https://t.me/rajeevrrs"
#property strict

//+------------------------------------------------------------------+
//| EA Inputs                                                        |
//+------------------------------------------------------------------+


extern string __TrailingManagement__ = "***Trailing Settings***";
enum TrailingType_enum {Virtual_Trailing, Classic_Trailing};
extern TrailingType_enum Trailing_Type = Virtual_Trailing;
extern int Trailing_Start = 30;
extern int Trailing_Gap = 30;


//+------------------------------------------------------------------+
//| Pre-Defined Value Auto                                           |
//+------------------------------------------------------------------+
double gPips = Point;
int gStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL);
int gFreezeLevel = MarketInfo(Symbol(), MODE_FREEZELEVEL);
string DemoRealCheck = IsDemo() ? "Demo" : "Real";

int gSpread;
double TrailingStopLoss_entryPrice;
string cTrailingType;

//+------------------------------------------------------------------+
//| OnInit                                                           |
//+------------------------------------------------------------------+
int OnInit()
  {
//Predefined Value
   gPips = Point;
   gStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL);
   gFreezeLevel = MarketInfo(Symbol(), MODE_FREEZELEVEL);
   DemoRealCheck = IsDemo() ? "Demo" : "Real";
   cTrailingType = Trailing_Type == Classic_Trailing ? "Classic" : "Virtual";

   if(Trailing_Type == Classic_Trailing && Trailing_Gap < gStopLevel)
      Trailing_Gap = gStopLevel + 1;

   return(INIT_SUCCEEDED);
  }


//+------------------------------------------------------------------+
//| On Deinit                                                        |
//+------------------------------------------------------------------+
int deinit()
  {
   ObjectsDeleteAll(0,"B",-1,-1);
   ObjectsDeleteAll(0,"S",-1,-1);
   return (0);
  }

//+------------------------------------------------------------------+
//| OnTick                                                           |
//+------------------------------------------------------------------+
void OnTick()
  {
   gSpread = MarketInfo(Symbol(), MODE_SPREAD);
//Trailing TP
   if(Trailing_Type == Classic_Trailing)
     {
      TrailingStopLoss();
      TrailingStopLoss();
     }
   else
     {
      VirtualTrailingStopLoss();
      VirtualTrailingStopLoss();
     }

   ChartComment(); //Chart Comment to show details
// --------- OnTick End ------------ //
  }

// Chart Comment Status
void ChartComment()
  {

   Comment("                                               ---------------------------------------------"
           "\n                                             :: ===>RRS Trailing<==="
           "\n                                             :: Info                              : (Spread : " + gSpread + ") |:| (Stop Level : " + gStopLevel + ") |:| (Freeze Level : " + gFreezeLevel + ")" +
           "\n                                             :: Leverage                       : 1 : " + AccountLeverage() + " ("+DemoRealCheck+" Account)" +
           "\n                                             ------------------------------------------------"
           "\n                                             :: Trailing                          : (Start : " + Trailing_Start + ") |:| (Gap : " + Trailing_Gap + ") |:| (Type : " + cTrailingType + ")" +
           "\n                                             ------------------------------------------------");
  }


//+--------------------------------------------------------------------+
// Trailing SL                                                         +
//+--------------------------------------------------------------------+
void TrailingStopLoss()
  {
// Loop through all open orders
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         TrailingStopLoss_entryPrice = OrderOpenPrice();
         // Buy Order Trailing
         if(OrderSymbol() == Symbol() && OrderType() == OP_BUY)
           {
            if(Bid - (TrailingStopLoss_entryPrice + Trailing_Start * gPips) > gPips * Trailing_Gap)
              {
               if(OrderStopLoss() < Bid - gPips * Trailing_Gap || OrderStopLoss() == 0)
                 {
                  ResetLastError();
                  RefreshRates();
                  if(!OrderModify(OrderTicket(), OrderOpenPrice(), Bid - gPips * Trailing_Gap, OrderTakeProfit(), 0, clrNONE))
                     Print(__FUNCTION__ + " => Buy Trail Error Code : " + GetLastError());
                 }
              }
           }

         // Sell Order Trailing
         if(OrderSymbol() == Symbol() && OrderType() == OP_SELL)
           {
            if((TrailingStopLoss_entryPrice - Trailing_Start * gPips) - Ask > gPips * Trailing_Gap)
              {
               if(OrderStopLoss() > Ask + gPips * Trailing_Gap || OrderStopLoss() == 0)
                 {
                  ResetLastError();
                  RefreshRates();
                  if(!OrderModify(OrderTicket(), OrderOpenPrice(), Ask + gPips * Trailing_Gap, OrderTakeProfit(), 0, clrNONE))
                     Print(__FUNCTION__ + " => Sell Trail Error Code : " + GetLastError());
                 }
              }
           }
        }
     }
  }


//+--------------------------------------------------------------------+
// Virtual Trailing                                                    +
//+--------------------------------------------------------------------+
void VirtualTrailingStopLoss()
  {
// Loop through all open orders
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         TrailingStopLoss_entryPrice = OrderOpenPrice();
         // Buy Order Trailing
         if(OrderSymbol() == Symbol() && OrderType() == OP_BUY)
           {
            double LastVirtualBuySL = GetHorizontalLinePrice("B"+OrderTicket());
            if(Bid <= LastVirtualBuySL && LastVirtualBuySL != 0)
              {
               ObjectDelete("B"+OrderTicket());
               ResetLastError();
               if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 3, clrNONE))
                  Print(__FUNCTION__ + " => Buy Order failed to close : " + GetLastError());
              }

            if(Bid - (TrailingStopLoss_entryPrice + Trailing_Start * gPips) > gPips * Trailing_Gap)
              {
               double VirtualBuySL = Bid - (gPips * Trailing_Gap);
               if(LastVirtualBuySL < VirtualBuySL || LastVirtualBuySL == 0.00)
                  DrawHline("B"+OrderTicket(),VirtualBuySL,clrBlue,1);
              }
           }
        }

      // Sell Order Trailing
      if(OrderSymbol() == Symbol() && OrderType() == OP_SELL)
        {
         double LastVirtualSellSL = GetHorizontalLinePrice("S"+OrderTicket());
         if(Ask >= LastVirtualSellSL && LastVirtualSellSL != 0)
           {
            ObjectDelete("S"+OrderTicket());
            ResetLastError();
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 3, clrNONE))
               Print(__FUNCTION__ + " => Sell Order failed to close : " + GetLastError());
           }
         if((TrailingStopLoss_entryPrice - Trailing_Start * gPips) - Ask > gPips * Trailing_Gap)
           {
            double VirtualSellSL = Ask + (gPips * Trailing_Gap);
            if(LastVirtualSellSL > VirtualSellSL || LastVirtualSellSL == 0.00)
               DrawHline("S"+OrderTicket(),VirtualSellSL,clrOrange,1);
           }
        }
     }
  }

//+------------------------------------------------------------------+
//|  Virtual Trailing Line                                           |
//+------------------------------------------------------------------+
void DrawHline(string name,double P,color clr,int WIDTH)
  {
   if(ObjectFind(name)!=-1)
      ObjectDelete(name);
   ObjectCreate(name,OBJ_HLINE,0,0,P,0,0,0,0);
   ObjectSet(name,OBJPROP_COLOR,clr);
   ObjectSet(name,OBJPROP_STYLE,2);
   ObjectSet(name,OBJPROP_WIDTH,WIDTH);
  }

//+------------------------------------------------------------------+
//| Virtual Trailing Line Price                                      |
//+------------------------------------------------------------------+
double GetHorizontalLinePrice(string objectName)
  {
// Loop through all objects on the chart
   for(int i = ObjectsTotal()-1; i >= 0; i--)
     {
      // Check if the object is a horizontal line and its name matches the specified objectName
      if(ObjectName(i) == objectName)
        {
         // Return the price value of the horizontal line
         return ObjectGetDouble(0, objectName, OBJPROP_PRICE1);
        }
     }
// If the object with the specified name is not found, return a default value (e.g., 0.00)
   return 0.00;
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
