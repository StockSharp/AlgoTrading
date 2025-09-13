//+------------------------------------------------------------------+
//|                                                           RRS EA |
//|                                   Copyright 2025, RRS Tangled EA |
//|                                             rajeeevrrs@gmail.com |
//+------------------------------------------------------------------+
#property copyright "RRS Tangled EA"
#property link      "https://t.me/rajeevrrs"
#property strict

//+------------------------------------------------------------------+
//| EA Inputs                                                        |
//+------------------------------------------------------------------+


extern string __LotSettings__ = "***Lot Settings***";
extern double minLot_Size = 0.01;
extern double maxLot_Size = 0.50;

extern string __OrderSettings__ = "***Order Settings***";
extern int TakeProfit = 100;
extern int StopLoss = 200;

extern string __TrailingSettings__ = "***Trailing Settings***";
extern int Trailing_Start = 50;
extern int Trailing_Gap = 50;

extern string __RestricationSettings__ = "***Restrication Settings***";
extern int maxSpread = 100;
extern int Slippage = 3;
extern int MaxOpenTrade = 10;

extern string __Risk_Management__ = "***Risk Management***";
enum RiskInMoneyMode_enum {FixedMoney, BalancePercentage};
extern RiskInMoneyMode_enum Risk_In_Money_Type = BalancePercentage;
extern double Money_In_Risk = 5.0;

extern string __ExpertAdvisor__ = "***EA Settings***";
extern string Trade_Comment = "RRS";
extern int Magic = 1000;
extern string EA_Notes = "Note For Your Reference";

//Timezone
int localHours = (TimeLocal() - TimeGMT()) / 3600;
int localMinutes = ((TimeLocal() - TimeGMT()) % 3600) / 60;
int brokerHours = (TimeCurrent() - TimeGMT()) / 3600;
int brokerMinutes = ((TimeCurrent() - TimeGMT()) % 3600) / 60;


//+------------------------------------------------------------------+
//| RRS Defined                                                      |
//+------------------------------------------------------------------+
//Int
int gBuyMagic, gSellMagic;
int OrderCount_BuyMagicOPBUY, OrderCount_SellMagicOPSELL;
int BuySellRandomMath;
int gStopLevel;
int gSymbolIndex = 0;

//String
string BuyStopTradeComment, SellStopTradeComment;
string gRandomSymbol;
string buyOpenTrade_Symbol, sellOpenTrade_Symbol;

//Double
double gSymbolEA_FloatingPL, gBuyFloatingPL, gSellFloatingPL;
double gTargeted_Revenue, gRisk_Money;
double OrderSend_StopLoss, OrderSend_TakeProfit;
double gRandomLotSize;

//+------------------------------------------------------------------+
//| OnInit                                                           |
//+------------------------------------------------------------------+
int OnInit()
  {
   gBuyMagic    = Magic + 1;
   gSellMagic   = Magic + 11;

//Trade Comments
   BuyStopTradeComment = Trade_Comment + "+RRS";
   SellStopTradeComment = Trade_Comment + "+RRS";

   return(INIT_SUCCEEDED);
  }


//+------------------------------------------------------------------+
//| On Deinit                                                        |
//+------------------------------------------------------------------+
int deinit()
  {
   ObjectsDeleteAll(0,"#",-1,-1);
   return (0);
  }

//+------------------------------------------------------------------+
//| OnTick                                                           |
//+------------------------------------------------------------------+
void OnTick()
  {
//Pre-defined OnTick Value
   MathSrand(GetTickCount());
   BuySellRandomMath = MathRand() % 4;
   OrderCount_BuyMagicOPBUY = trade_count_ordertype(OP_BUY, gBuyMagic);
   OrderCount_SellMagicOPSELL = trade_count_ordertype(OP_SELL, gSellMagic);
   gRandomSymbol = randomsymbol();
   buyOpenTrade_Symbol = GetAllOpenTradeSymbols(gBuyMagic, OP_BUY);
   sellOpenTrade_Symbol = GetAllOpenTradeSymbols(gSellMagic, OP_SELL);
   gRandomLotSize = RandomLotSize();
   gStopLevel = MarketInfo(gRandomSymbol, MODE_STOPLEVEL) + 2;

//Trailing TP
   if(Trailing_Gap > 0 && Trailing_Start > 0)
     {
      if(OrderCount_BuyMagicOPBUY >= 1)
         TrailingStopLoss(gBuyMagic, buyOpenTrade_Symbol);
      if(OrderCount_SellMagicOPSELL >= 1)
         TrailingStopLoss(gSellMagic, sellOpenTrade_Symbol);
     }

//Order Placement
   if((OrderCount_BuyMagicOPBUY + OrderCount_SellMagicOPSELL) < MaxOpenTrade && MarketInfo(gRandomSymbol, MODE_SPREAD) < maxSpread)
      NewOrderSend();

//Financial Value
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
//| Doube Side Buy And Sell Order                                    |
//+------------------------------------------------------------------+
void NewOrderSend()
  {
   if(CheckMoneyForTrade(gRandomSymbol, gRandomLotSize, OP_BUY) == true && (BuySellRandomMath == 1))
     {
      ResetLastError();
      if(!OrderSend(gRandomSymbol, OP_BUY, gRandomLotSize, MarketInfo(gRandomSymbol, MODE_ASK), Slippage, OrderSend_StopLoss = (StopLoss > 0) ? MarketInfo(gRandomSymbol, MODE_ASK) - MathMax(StopLoss, gStopLevel) * Point : 0, OrderSend_TakeProfit = (TakeProfit > 0) ? MarketInfo(gRandomSymbol, MODE_ASK) + MathMax(TakeProfit, gStopLevel) * Point : 0, BuyStopTradeComment, gBuyMagic, 0, clrNONE))
         Print("Buy Order => Error Code : " + GetLastError());
     }

   if(CheckMoneyForTrade(gRandomSymbol, gRandomLotSize, OP_SELL) == true && (BuySellRandomMath == 2))
     {
      ResetLastError();
      if(!OrderSend(gRandomSymbol, OP_SELL, gRandomLotSize, MarketInfo(gRandomSymbol, MODE_BID), Slippage, OrderSend_StopLoss = (StopLoss > 0) ? MarketInfo(gRandomSymbol, MODE_BID) + MathMax(StopLoss, gStopLevel) * Point : 0, OrderSend_TakeProfit = (TakeProfit > 0) ? MarketInfo(gRandomSymbol, MODE_BID) - MathMax(TakeProfit, gStopLevel) * Point : 0, SellStopTradeComment, gSellMagic, 0, clrNONE))
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
      if(OrderType() != OP_BUY && OrderType() != OP_SELL)
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
      if(OrderMagicNumber() != trade_close_magic)
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
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
               Print(__FUNCTION__ " => Buy Order failed to close, error code:", GetLastError());
           }
         if(OrderType() == OP_SELL)
           {
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
      if(OrderMagicNumber() != trade_count_ordertype_magic)
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
           "\n                                             :: ===>RRS Tangled EA<==="
           "\n                                             ------------------------------------------------" +
           "\n                                             :: Lot Size                         : (Min Lot : " + minLot_Size + ") |:| (Max Lot : " + maxLot_Size + ")" +
           "\n                                             :: Take Profit                      : " + TakeProfit +
           "\n                                             :: Stop Loss                      : " + StopLoss +
           "\n                                             :: Maximum Open Trade             : " + MaxOpenTrade +
           "\n                                             :: Trailing                          : (Start : " + Trailing_Start + ") |:| (Gap : " + Trailing_Gap + ")" +
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
void TrailingStopLoss(int TrailingStopLoss_magic, string TrailingSymbol)
  {
// Loop through all open orders
   double TrailingStopLoss_entryPrice;
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         TrailingStopLoss_entryPrice = OrderOpenPrice();
         if(OrderMagicNumber() == TrailingStopLoss_magic && OrderType() == OP_BUY && OrderSymbol() == TrailingSymbol)
           {
            int BuyTrailingGap = Trailing_Gap < MarketInfo(buyOpenTrade_Symbol, MODE_SPREAD) ? MarketInfo(buyOpenTrade_Symbol, MODE_SPREAD) + MarketInfo(buyOpenTrade_Symbol, MODE_STOPLEVEL) + Trailing_Gap : Trailing_Gap;
            if(MarketInfo(buyOpenTrade_Symbol, MODE_BID) - (TrailingStopLoss_entryPrice + Trailing_Start * Point) > Point * BuyTrailingGap)
              {
               if(OrderStopLoss() < MarketInfo(buyOpenTrade_Symbol, MODE_BID) - Point * BuyTrailingGap || OrderStopLoss() == 0)
                 {
                  ResetLastError();
                  RefreshRates();
                  if(!OrderModify(OrderTicket(), OrderOpenPrice(), MarketInfo(buyOpenTrade_Symbol, MODE_BID) - Point * BuyTrailingGap, OrderTakeProfit(), 0, clrNONE))
                     Print(__FUNCTION__ + " => " + buyOpenTrade_Symbol + " : Buy Order Error Code : " + GetLastError());
                 }
              }
           }

         if(OrderMagicNumber() == TrailingStopLoss_magic && OrderType() == OP_SELL && OrderSymbol() == TrailingSymbol)
           {
            int SellTrailingGap = Trailing_Gap < MarketInfo(sellOpenTrade_Symbol, MODE_SPREAD) ? MarketInfo(sellOpenTrade_Symbol, MODE_SPREAD) + MarketInfo(sellOpenTrade_Symbol, MODE_STOPLEVEL) + Trailing_Gap : Trailing_Gap;
            if((TrailingStopLoss_entryPrice - Trailing_Start * Point) - MarketInfo(sellOpenTrade_Symbol, MODE_ASK) > Point * SellTrailingGap)
              {
               if(OrderStopLoss() > MarketInfo(sellOpenTrade_Symbol, MODE_ASK) + Point * SellTrailingGap || OrderStopLoss() == 0)
                 {
                  ResetLastError();
                  RefreshRates();
                  if(!OrderModify(OrderTicket(), OrderOpenPrice(), MarketInfo(sellOpenTrade_Symbol, MODE_ASK) + Point * SellTrailingGap, OrderTakeProfit(), 0, clrNONE))
                     Print(__FUNCTION__ + " => " + sellOpenTrade_Symbol + " : Sell Order Error Code : " + GetLastError());
                 }
              }
           }
        }
     }
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
      Print("Not enough money to trade");
      return(false);
     }
//--- checking successful
   return(true);
  }
//+------------------------------------------------------------------+
//|  Random Symbol                                                   |
//+------------------------------------------------------------------+
string randomsymbol()
  {
   string randomsymbol_pairs[] = {"USD", "GBP", "AUD", "CAD", "JPY", "XAU", "XAG", "EUR", "CHF", "SDG", "HKD", "NZD", "BTC"};
//string randomsymbol_pairs[] = {"USD", "GBP", "AUD", "CAD", "JPY", "XAU", "XAG", "EUR", "CHF", "SDG", "HKD", "NZD", "BTC", "BCH", "XRP", "ETH", "USD", "LTC", "XLM"};

   int totalSymbols = SymbolsTotal(true);  // Include all Market Watch symbols
   string validSymbols[];

   for(int i = 0; i < totalSymbols; i++)
     {
      string symbolName = SymbolName(i, true);

      // Check for valid currency pair combinations
      for(int j = 0; j < ArraySize(randomsymbol_pairs); j++)
        {
         for(int k = 0; k < ArraySize(randomsymbol_pairs); k++)
           {
            if(j != k)  // Prevent matching same symbols (e.g., USDUSD)
              {
               if(StringFind(symbolName, randomsymbol_pairs[j]) != -1 &&
                  StringFind(symbolName, randomsymbol_pairs[k]) != -1)
                 {
                  ArrayResize(validSymbols, ArraySize(validSymbols) + 1);
                  validSymbols[ArraySize(validSymbols) - 1] = symbolName;
                 }
              }
           }
        }
     }

// Return a random symbol from the valid list
   if(ArraySize(validSymbols) > 0)
     {
      int randomIndex = MathRand() % ArraySize(validSymbols);
      return validSymbols[randomIndex];
     }

   return ""; // Return empty if no valid symbol found
  }

//+------------------------------------------------------------------+
//|   Get Symbol by Magic                                            |
//+------------------------------------------------------------------+
string GetAllOpenTradeSymbols(int OrderMagicNumberr, int OrderTypee)
  {
   string GetAllOpenTradeSymbols_symbols = "";
   string GetAllOpenTradeSymbols_symbolsArray[];
   for(int i = 0; i < OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))  // Select order
        {
         if(OrderMagicNumber() == OrderMagicNumberr && OrderType() == OrderTypee)
           {
            string GetAllOpenTradeSymbols_symbol = OrderSymbol();
            if(StringFind(GetAllOpenTradeSymbols_symbols, GetAllOpenTradeSymbols_symbol) == -1)  // Check if symbol is already added
              {
               ArrayResize(GetAllOpenTradeSymbols_symbolsArray, ArraySize(GetAllOpenTradeSymbols_symbolsArray) + 1);
               GetAllOpenTradeSymbols_symbolsArray[ArraySize(GetAllOpenTradeSymbols_symbolsArray) - 1] = OrderSymbol();
              }
           }
        }
     }
// Return a random symbol from the valid list
   if(ArraySize(GetAllOpenTradeSymbols_symbolsArray) > 0)
     {
      gSymbolIndex = gSymbolIndex < ArraySize(GetAllOpenTradeSymbols_symbolsArray) ? gSymbolIndex : 0;
      string SelectedOpenSymbol = GetAllOpenTradeSymbols_symbolsArray[gSymbolIndex];
      gSymbolIndex++;
      return SelectedOpenSymbol;
     }
   return "";
  }

//+------------------------------------------------------------------+
//|        Random Lot size                                           |
//+------------------------------------------------------------------+
double RandomLotSize()
  {
// Retrieve lot constraints
   double minLot  = MarketInfo(gRandomSymbol, MODE_MINLOT);
   double maxLot  = MarketInfo(gRandomSymbol, MODE_MAXLOT);
   double lotStep = MarketInfo(gRandomSymbol, MODE_LOTSTEP);

// Ensure lotStep is valid
   if(lotStep <= 0)
      lotStep = 0.01; // Default to a small valid step

// Generate a random value within the specified range
   double LotrandomValue = minLot_Size + (maxLot_Size - minLot_Size) * MathRand() / 32767.0;

// Adjust to the nearest lot step
   LotrandomValue = minLot + lotStep * MathRound((LotrandomValue - minLot) / lotStep);

// Final check to ensure it remains within bounds
   LotrandomValue = MathMax(minLot, MathMin(LotrandomValue, maxLot));

// Normalize to 2 decimal places
   return NormalizeDouble(LotrandomValue, 2);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
