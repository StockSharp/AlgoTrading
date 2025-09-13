//+------------------------------------------------------------------+
//|                                                           RRS EA |
//|                        Copyright 2024, RRS NonDirectional Trader |
//|                                             rajeeevrrs@gmail.com |
//+------------------------------------------------------------------+
#property copyright "RRS NonDirectional Trading EA"
#property link      "https://t.me/rajeevrrs"
#property strict


//+------------------------------------------------------------------+
//| EA Inputs                                                        |
//+------------------------------------------------------------------+

extern string __EA_Strategy__ = "***Trading Strategy***";
enum TradingStrategy_enum {Hedge_Style, BuySell_Random, Buy_Sell, Auto_Swap, Buy_Order, Sell_Order};
extern TradingStrategy_enum Trading_Strategy = Hedge_Style;
extern bool New_Trade = TRUE;

extern string __OrderSettings__ = "***Order Settings***";
extern double Lot_Size = 0.01;

extern string __StopLoss__ = "***Stop Loss***";
enum StopLossType_enum {Virtual_SL, Classic_SL};
extern StopLossType_enum StopLoss_Type = Virtual_SL;
extern int StopLoss = 200;

extern string __TakeProfit__ = "***Take Profit***";
enum TakeProfitType_enum {Virtual_TP, Classic_TP};
extern TakeProfitType_enum TakeProfit_Type = Virtual_TP;
extern int TakeProfit = 100;

extern string __TrailingManagement__ = "***Trailing Settings***";
enum TrailingType_enum {Virtual_Trailing, Classic_Trailing};
extern TrailingType_enum Trailing_Type = Virtual_Trailing;
extern int Trailing_Start = 30;
extern int Trailing_Gap = 30;

extern string __Risk_Management__ = "***Risk Management***";
enum RiskInMoneyMode_enum {FixedMoney, BalancePercentage};
extern RiskInMoneyMode_enum Risk_In_Money_Type = BalancePercentage;
extern double Money_In_Risk = 5.0;

extern string __RestrictionMode__ = "***Restriction Mode***";
extern int Max_Spread = 50;
extern int Slippage = 3;

extern string __ExpertAdvisor__ = "***EA Settings***";
extern string Trade_Comment = "RRS";
extern int Magic = 1000;
extern string EA_Notes = "Note For Your Reference";

//+------------------------------------------------------------------+
//| Pre-Defined Value Auto                                           |
//+------------------------------------------------------------------+
double gPips = Point;
double gMinLotChecking = MarketInfo(Symbol(), MODE_MINLOT);
double gMaxLotChecking = MarketInfo(Symbol(), MODE_MAXLOT);
int gStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL);
int gFreezeLevel = MarketInfo(Symbol(), MODE_FREEZELEVEL);
string gDepositCurrency=AccountInfoString(ACCOUNT_CURRENCY);
double gTickValue = MarketInfo(Symbol(), MODE_TICKVALUE);
string DemoRealCheck = IsDemo() ? "Demo" : "Real";

//Timezone
int localHours = (TimeLocal() - TimeGMT()) / 3600;
int localMinutes = ((TimeLocal() - TimeGMT()) % 3600) / 60;
int brokerHours = (TimeCurrent() - TimeGMT()) / 3600;
int brokerMinutes = ((TimeCurrent() - TimeGMT()) % 3600) / 60;


//+------------------------------------------------------------------+
//| RRS Defined                                                      |
//+------------------------------------------------------------------+
//int
int gBuyMagic, gSellMagic;
int gSpread;
int gLastOrderCheckType;
int BuySellRandomMath = -1;
int OrderCount_BuyMagicOPBUY, OrderCount_SellMagicOPSELL;

//String
string Trade_Mode_Status; //Status for Initial Trade
string stylecommenttrade;
string BuyStopTradeComment, SellStopTradeComment;
string cTradingStyle, cTrailingType, cTakeProfitType, cStopLossType;

//Double
double gSymbolEA_FloatingPL, gBuyFloatingPL, gSellFloatingPL;
double gTargeted_Revenue, gRisk_Money;
double gAccountBalance;
double TrailingStopLoss_entryPrice;

//+------------------------------------------------------------------+
//| OnInit                                                           |
//+------------------------------------------------------------------+
int OnInit()
  {
//Predefined Value
   gPips = Point;
   gMinLotChecking = MarketInfo(Symbol(), MODE_MINLOT);
   gMaxLotChecking = MarketInfo(Symbol(), MODE_MAXLOT);
   gStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL);
   gFreezeLevel = MarketInfo(Symbol(), MODE_FREEZELEVEL);
   gDepositCurrency=AccountInfoString(ACCOUNT_CURRENCY);
   gTickValue = MarketInfo(Symbol(), MODE_TICKVALUE);
   DemoRealCheck = IsDemo() ? "Demo" : "Real";

//Automatic Value Handling
   if(Lot_Size < gMinLotChecking)
      Lot_Size = gMinLotChecking;
   if(Lot_Size > gMaxLotChecking)
      Lot_Size = gMaxLotChecking;
   if(Trailing_Gap < gStopLevel)
      Trailing_Gap = gStopLevel + 2;
   if(Trailing_Start < gStopLevel)
      Trailing_Start = gStopLevel + 2;
   if(TakeProfit_Type == Classic_TP && TakeProfit < gStopLevel)
      TakeProfit = gStopLevel + 2;
   if(StopLoss_Type == Classic_SL && StopLoss < gStopLevel)
      StopLoss = gStopLevel + 2;

//Auto Swap
   if(Trading_Strategy == Auto_Swap)
     {
      if(MarketInfo(Symbol(), MODE_SWAPLONG) > 0)
         Trading_Strategy = Buy_Order;
      else
         if(MarketInfo(Symbol(), MODE_SWAPSHORT) > 0)
            Trading_Strategy = Sell_Order;
         else
            Trading_Strategy = Hedge_Style;
     }
//Magic Numbers and Trade-Order Comments
   if(Trading_Strategy == Hedge_Style)
     {
      stylecommenttrade = "HedgeStyle";
      gBuyMagic    = Magic + 1;
      gSellMagic   = Magic + 11;
     }
   if(Trading_Strategy == Buy_Order)
     {
      stylecommenttrade = "BUY";
      gBuyMagic    = Magic + 2;
      gSellMagic   = Magic + 22;
     }
   if(Trading_Strategy == Sell_Order)
     {
      stylecommenttrade = "SELL";
      gBuyMagic    = Magic + 3;
      gSellMagic   = Magic + 33;
     }
   if(Trading_Strategy == BuySell_Random)
     {
      stylecommenttrade = "RANDOM";
      gBuyMagic    = Magic + 4;
      gSellMagic   = Magic + 44;
     }
   if(Trading_Strategy == Buy_Sell)
     {
      stylecommenttrade = "BUYSELL";
      gBuyMagic    = Magic + 5;
      gSellMagic   = Magic + 55;
     }

//Trade Comments
   BuyStopTradeComment = Trade_Comment + "+" + stylecommenttrade + "+RRS";
   SellStopTradeComment = Trade_Comment + "+" + stylecommenttrade + "+RRS";

//Comment status
   cTradingStyle = (Trading_Strategy == Hedge_Style) ? "Hedge Style" : (Trading_Strategy == Buy_Order) ? "Buy Orders" : (Trading_Strategy == BuySell_Random) ? "Buy Sell Random" : (Trading_Strategy == Buy_Sell) ? "Buy Sell Based on Last Trade" : "Sell Orders";
   cTrailingType = Trailing_Type == Classic_Trailing ? "Classic Trailing Stop Loss" : "Virtual Trailing";
   cTakeProfitType = TakeProfit_Type == Classic_TP ? "Class Take Profit" : "Virtual Take Profit";
   cStopLossType = StopLoss_Type == Classic_SL ? "Classic Stop Loss" : "Virtual Stop Loss";

   return(INIT_SUCCEEDED);
  }


//+------------------------------------------------------------------+
//| On Deinit                                                        |
//+------------------------------------------------------------------+
int deinit()
  {
   ObjectsDeleteAll(0,"#",-1,-1);
   if(_UninitReason == REASON_REMOVE)
     {
      ObjectsDeleteAll(0,"B",-1,-1);
      ObjectsDeleteAll(0,"S",-1,-1);
     }
   return (0);
  }

//+------------------------------------------------------------------+
//| OnTick                                                           |
//+------------------------------------------------------------------+
void OnTick()
  {
//Pre-defined OnTick Value
   MathSrand(GetTickCount());
   BuySellRandomMath = MathRand() % 2;
   gSpread = MarketInfo(Symbol(), MODE_SPREAD);
   OrderCount_BuyMagicOPBUY = trade_count_ordertype(OP_BUY, gBuyMagic);
   OrderCount_SellMagicOPSELL = trade_count_ordertype(OP_SELL, gSellMagic);
   gLastOrderCheckType = CheckLastClosedOrder();

//Trailing TP
   if(Trailing_Type == Classic_Trailing)
     {
      if(OrderCount_BuyMagicOPBUY >= 1)
         TrailingStopLoss(gBuyMagic);
      if(OrderCount_SellMagicOPSELL >= 1)
         TrailingStopLoss(gSellMagic);
     }
   else
     {
      if(OrderCount_BuyMagicOPBUY >= 1)
         VirtualTrailingStopLoss(gBuyMagic);
      if(OrderCount_SellMagicOPSELL >= 1)
         VirtualTrailingStopLoss(gSellMagic);
     }

//Order Placement
   if(New_Trade && gSpread <= Max_Spread)
     {
      Trade_Mode_Status = "Active";
      Order_BuySell();
     }
   else
     {
      if(!New_Trade)
         Trade_Mode_Status = "Paused => New Trade is disabled in EA Settings.";
      else
         if(gSpread > Max_Spread)
            Trade_Mode_Status = "Paused => Spread is too high to take the trade.";
     }

//Virtual StopLoss
   if(StopLoss_Type == Virtual_SL)
     {
      VirtualStopLoss(gBuyMagic);
      VirtualStopLoss(gSellMagic);
     }
//Virtual Take Profit
   if(TakeProfit_Type == Virtual_TP)
     {
      VirtualTakeProfit(gBuyMagic);
      VirtualTakeProfit(gSellMagic);
     }
//Financial Value
   gAccountBalance = AccountBalance();
   gBuyFloatingPL = CalculateTradeFloating(gBuyMagic);
   gSellFloatingPL = CalculateTradeFloating(gSellMagic);
   gSymbolEA_FloatingPL = gBuyFloatingPL + gSellFloatingPL;

//Risk In Money
   if(Risk_In_Money_Type == BalancePercentage)
      gRisk_Money =(-1.0 * AccountBalance() * (Money_In_Risk * 0.01));
   else
      gRisk_Money = (-1.0 * Money_In_Risk);

   if(gSymbolEA_FloatingPL <= gRisk_Money)
     {
      CloseOpenAndPendingTrades(gBuyMagic);
      CloseOpenAndPendingTrades(gSellMagic);
      Print("Risk Management => Successfully Closed");
     }

   ChartComment(); //Chart Comment to show details
// --------- OnTick End ------------ //
  }

//+------------------------------------------------------------------+
//| Buy And Sell Order                                               |
//+------------------------------------------------------------------+
void Order_BuySell()
  {
   double OrderSend_StopLoss, OrderSend_TakeProfit;
   if(OrderCount_BuyMagicOPBUY == 0 && CheckMoneyForTrade(_Symbol, Lot_Size, OP_BUY) == true && (Trading_Strategy == Buy_Order || Trading_Strategy == Hedge_Style || (Trading_Strategy == Buy_Sell && gLastOrderCheckType == 6) || (Trading_Strategy == BuySell_Random && OrderCount_SellMagicOPSELL == 0 && BuySellRandomMath == 0)))
     {
      ResetLastError();
      if(!OrderSend(Symbol(), OP_BUY, Lot_Size, Ask, Slippage, OrderSend_StopLoss = (StopLoss_Type == Classic_SL && StopLoss > 0) ? Ask - StopLoss * gPips : 0, OrderSend_TakeProfit = (TakeProfit_Type == Classic_TP && TakeProfit > 0) ? Ask + TakeProfit * gPips : 0, BuyStopTradeComment, gBuyMagic, 0, clrNONE))
         Print("Buy Order => Error Code : " + GetLastError());
     }

   if(OrderCount_SellMagicOPSELL == 0 && CheckMoneyForTrade(_Symbol, Lot_Size, OP_SELL) == true && ((Trading_Strategy == Sell_Order || Trading_Strategy == Hedge_Style) || (Trading_Strategy == Buy_Sell && gLastOrderCheckType == 5) || (Trading_Strategy == BuySell_Random && OrderCount_BuyMagicOPBUY == 0 && BuySellRandomMath == 1)))
     {
      ResetLastError();
      if(!OrderSend(Symbol(), OP_SELL, Lot_Size, Bid, Slippage, OrderSend_StopLoss = (StopLoss_Type == Classic_SL && StopLoss > 0) ? Bid + StopLoss * gPips : 0, OrderSend_TakeProfit = (TakeProfit_Type == Classic_TP && TakeProfit > 0) ? Bid - TakeProfit * gPips : 0, SellStopTradeComment, gSellMagic, 0, clrNONE))
         Print("Sell Order => Error Code : " + GetLastError());
     }
  }



// Calculate Trade profits and Loss
double CalculateTradeFloating(int CalculateTradeFloating_Magic)
  {
   double CalculateTradeFloating_Value = 0;
   for(int CalculateTradeFloating_i = 0; CalculateTradeFloating_i < OrdersTotal(); CalculateTradeFloating_i++)
     {
      OrderSelect(CalculateTradeFloating_i, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() != Symbol() || (OrderType() != OP_BUY && OrderType() != OP_SELL))
         continue;
      if(CalculateTradeFloating_Magic == OrderMagicNumber())
         CalculateTradeFloating_Value += OrderProfit() + OrderSwap() + OrderCommission();
     }
   return CalculateTradeFloating_Value;
  }

// Trade closing based on Magic number
void CloseOpenAndPendingTrades(int trade_close_magic)
  {
   for(int pos_0 = OrdersTotal() - 1; pos_0 >= 0; pos_0--)
     {
      OrderSelect(pos_0, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() != Symbol() || OrderMagicNumber() != trade_close_magic)
         continue;

      if(OrderType() != OP_BUY && OrderType() != OP_SELL)
        {
         ResetLastError();
         if(!OrderDelete(OrderTicket()))
            Print(__FUNCTION__ " => Pending Order failed to close, error code:", GetLastError());
        }
      else
        {
         ResetLastError();
         if(OrderType() == OP_BUY)
           {
            ObjectDelete("B"+OrderTicket());
            if(!OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, clrNONE))
               Print(__FUNCTION__ " => Buy Order failed to close, error code:", GetLastError());
           }
         if(OrderType() == OP_SELL)
           {
            ObjectDelete("S"+OrderTicket());
            if(!OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, clrNONE))
               Print(__FUNCTION__ " => Sell Order failed to close, error code:", GetLastError());
           }
        }
     }
  }

//+------------------------------------------------------------------+
//|  Virtual Stop Loss                                               |
//+------------------------------------------------------------------+
void VirtualStopLoss(int trade_close_magic)
  {
   for(int pos_0 = OrdersTotal() - 1; pos_0 >= 0; pos_0--)
     {
      OrderSelect(pos_0, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() != Symbol() || OrderMagicNumber() != trade_close_magic)
         continue;
      ResetLastError();
      if(OrderType() == OP_BUY)
        {
         if(StopLoss != 0 && Bid <= OrderOpenPrice() - StopLoss * Point)
           {
            ObjectDelete("B"+OrderTicket());
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
               Print(__FUNCTION__ " => Buy Order failed to close, error code:", GetLastError());
           }
        }
      if(OrderType() == OP_SELL)
        {
         if(StopLoss != 0 && Ask >= OrderOpenPrice() + StopLoss * Point)
           {
            ObjectDelete("S"+OrderTicket());
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
               Print(__FUNCTION__ " => Sell Order failed to close, error code:", GetLastError());
           }
        }
     }
  }

//+------------------------------------------------------------------+
//|  Virtual Take Profits                                            |
//+------------------------------------------------------------------+
void VirtualTakeProfit(int trade_close_magic)
  {
   for(int pos_0 = OrdersTotal() - 1; pos_0 >= 0; pos_0--)
     {
      OrderSelect(pos_0, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() != Symbol() || OrderMagicNumber() != trade_close_magic)
         continue;
      ResetLastError();
      if(OrderType() == OP_BUY)
        {
         if(TakeProfit != 0 && Bid >= OrderOpenPrice() + TakeProfit * Point)
           {
            ObjectDelete("B"+OrderTicket());
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
               Print(__FUNCTION__ " => Buy Order failed to close, error code:", GetLastError());
           }
        }
      if(OrderType() == OP_SELL)
        {
         if(TakeProfit != 0 && Ask <= OrderOpenPrice() - TakeProfit * Point)
           {
            ObjectDelete("S"+OrderTicket());
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
               Print(__FUNCTION__ " => Sell Order failed to close, error code:", GetLastError());
           }
        }
     }
  }


// Trade Counting based on Order Type and Magic Number
int trade_count_ordertype(int trade_count_ordertype_value, int trade_count_ordertype_magic)
  {
   int count_4 = 0;
   for(int pos_8 = 0; pos_8 < OrdersTotal(); pos_8++)
     {
      OrderSelect(pos_8, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() != Symbol() || OrderMagicNumber() != trade_count_ordertype_magic)
         continue;
      if(trade_count_ordertype_value == OrderType())
         count_4++;
     }
   return count_4;
  }

// Chart Comment Status
void ChartComment()
  {
   string c_Risk_Type, c_risk_t;

   if(Risk_In_Money_Type == BalancePercentage)
     {
      c_risk_t = "Balance Percentage";
      c_Risk_Type = "(Percentage : " + Money_In_Risk + ") => (Money In Risk : " + -1 * gRisk_Money + ")";
     }
   else
     {
      c_risk_t = "Fixed Money";
      c_Risk_Type = "(Money In Risk : " + -1 * gRisk_Money + ")";
     }

   Comment("                                               ---------------------------------------------"
           "\n                                             :: ===>RRS Non Directional EA<==="
           "\n                                             :: Info                              : (Spread : " + gSpread + ") |:| (Stop Level : " + gStopLevel + ") |:| (Freeze Level : " + gFreezeLevel + ")" +
           "\n                                             :: Leverage                       : 1 : " + AccountLeverage() + " ("+DemoRealCheck+" Account)" +
           "\n                                             :: Point Value                    : (" + Lot_Size + " Lot : " + (gTickValue * Lot_Size) + " " + gDepositCurrency + ") |:| (1 Lot : " + gTickValue + " " + gDepositCurrency + ") |:| (0.1 Lot : " + (gTickValue * 0.1) + " " + gDepositCurrency + ") |:| (0.01 Lot : " + (gTickValue * 0.01) + " " + gDepositCurrency + ")" +
           "\n                                             ------------------------------------------------"
           "\n                                             :: Trade mode                   : " + Trade_Mode_Status +
           "\n                                             ------------------------------------------------" +
           "\n                                             :: Trading Strategy             : " + cTradingStyle +
           "\n                                             :: Lot Size                         : " + Lot_Size +
           "\n                                             :: Take Profit                      : " + TakeProfit + " ("  + cTakeProfitType + ")" +
           "\n                                             :: Stop Loss                      : " + StopLoss + " ("  + cStopLossType + ")" +
           "\n                                             :: Trailing                          : (Start : " + Trailing_Start + ") |:| (Gap : " + Trailing_Gap + ") |:| (Type : " + cTrailingType + ")" +
           "\n                                             :: Maxium Spread              : " + Max_Spread +
           "\n                                             :: Risk Management          : (Risk Type : " + c_risk_t + ") |:| "  + c_Risk_Type  +
           "\n                                             ------------------------------------------------" +
           "\n                                             :: Magic Number               : " + Magic + " => (Buy Magic : " + gBuyMagic + ") |:| (Sell Magic : " + gSellMagic + ")" +
           "\n                                             :: Timezone                      : (Local PC : " + localHours + ":" + localMinutes + ")" + " |:| (Broker Timezone : " + brokerHours + ":" + brokerMinutes + ")" +
           "\n                                             :: Order Comment             : " + Trade_Comment +
           "\n                                             :: Notes                           : " + EA_Notes +
           "\n                                             ------------------------------------------------" +
           "\n                                             :: Email                           : rajeeevrrs@gmail.com " +
           "\n                                             :: Telegram                     : @rajeevrrs " +
           "\n                                             :: Skype                         : rajeev-rrs " +
           "\n                                             ------------------------------------------------");
  }

//+--------------------------------------------------------------------+
// Trailing SL                                                         +
//+--------------------------------------------------------------------+
void TrailingStopLoss(int TrailingStopLoss_magic)
  {
// Loop through all open orders
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         TrailingStopLoss_entryPrice = OrderOpenPrice();
         // Check if the order is a buy order with magic number 1
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == TrailingStopLoss_magic && OrderType() == OP_BUY)
           {
            if(Bid - (TrailingStopLoss_entryPrice + Trailing_Start * gPips) > gPips * Trailing_Gap)
              {
               if(OrderStopLoss() < Bid - gPips * Trailing_Gap || OrderStopLoss() == 0)
                 {
                  ResetLastError();
                  RefreshRates();
                  if(!OrderModify(OrderTicket(), OrderOpenPrice(), Bid - gPips * Trailing_Gap, OrderTakeProfit(), 0, clrNONE))
                     Print(__FUNCTION__ + " => Buy Order Error Code : " + GetLastError());
                 }
              }
           }

         // Check if the order is a sell order with magic number 2
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == TrailingStopLoss_magic && OrderType() == OP_SELL)
           {
            if((TrailingStopLoss_entryPrice - Trailing_Start * gPips) - Ask > gPips * Trailing_Gap)
              {
               if(OrderStopLoss() > Ask + gPips * Trailing_Gap || OrderStopLoss() == 0)
                 {
                  ResetLastError();
                  RefreshRates();
                  if(!OrderModify(OrderTicket(), OrderOpenPrice(), Ask + gPips * Trailing_Gap, OrderTakeProfit(), 0, clrNONE))
                     Print(__FUNCTION__ + " => Sell Order Error Code : " + GetLastError());
                 }
              }
           }
        }
     }
  }


//+--------------------------------------------------------------------+
// Virtual Trailing                                                    +
//+--------------------------------------------------------------------+
void VirtualTrailingStopLoss(int TrailingStopLoss_magic)
  {
// Loop through all open orders
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         TrailingStopLoss_entryPrice = OrderOpenPrice();
         // Check if the order is a buy order with magic number 1
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == TrailingStopLoss_magic && OrderType() == OP_BUY)
           {
            double LastVirtualBuySL = GetHorizontalLinePrice("B"+OrderTicket());
            if(Bid <= LastVirtualBuySL && LastVirtualBuySL != 0)
              {
               ObjectDelete("B"+OrderTicket());
               ResetLastError();
               if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
                  Print(__FUNCTION__ + " => Buy Order failed to close : " + GetLastError());
              }

            if(Bid - (TrailingStopLoss_entryPrice + Trailing_Start * gPips) > gPips * Trailing_Gap)
              {
               double VirtualBuySL = Bid - (gPips * Trailing_Gap);
               if(LastVirtualBuySL < VirtualBuySL || LastVirtualBuySL == 0.00)
                  DrawHline("B"+OrderTicket(),VirtualBuySL,clrGreen,1);
              }
           }
        }

      // Check if the order is a sell order with magic number 2
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == TrailingStopLoss_magic && OrderType() == OP_SELL)
        {
         double LastVirtualSellSL = GetHorizontalLinePrice("S"+OrderTicket());
         if(Ask >= LastVirtualSellSL && LastVirtualSellSL != 0)
           {
            ObjectDelete("S"+OrderTicket());
            ResetLastError();
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
               Print(__FUNCTION__ + " => Sell Order failed to close : " + GetLastError());
           }
         if((TrailingStopLoss_entryPrice - Trailing_Start * gPips) - Ask > gPips * Trailing_Gap)
           {
            double VirtualSellSL = Ask + (gPips * Trailing_Gap);
            if(LastVirtualSellSL > VirtualSellSL || LastVirtualSellSL == 0.00)
               DrawHline("S"+OrderTicket(),VirtualSellSL,clrRed,1);
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
         //countobj++;
         // Return the price value of the horizontal line
         return ObjectGetDouble(0, objectName, OBJPROP_PRICE1);
        }
     }
// If the object with the specified name is not found, return a default value (e.g., -1)
   return 0.00;
  }

//+------------------------------------------------------------------+
//|   Last Closed Order                                              |
//+------------------------------------------------------------------+
int CheckLastClosedOrder()
  {
   int CheckLastClosedOrder_totalOrders = OrdersHistoryTotal();
   for(int i = CheckLastClosedOrder_totalOrders - 1; i >= 0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
        {
         if(OrderSymbol() == Symbol()) // Ensures it's for the current chart symbol
           {
            if(OrderType() == OP_BUY && OrderMagicNumber() == gBuyMagic)
               return 5;
            else
               if(OrderType() == OP_SELL && OrderMagicNumber() == gSellMagic)
                  return 6;
            break; // Exit loop after finding the most recent closed order
           }
        }
     }
   return 5; // Default return if no valid order found
  }
//+------------------------------------------------------------------+
//|    Check Balance and Margin                                      |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb, double lots,int type)
  {
   double free_margin=AccountFreeMarginCheck(symb,type, lots);
//-- if there is not enough money
   if(free_margin<0)
     {
      Trade_Mode_Status = "Paused => Not enough money. Add the fund to your trading account.";
      return(false);
     }
//--- checking successful
   return(true);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
