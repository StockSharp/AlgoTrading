//+------------------------------------------------------------------+
//|                                                           RRS EA |
//|                                   Copyright 2025, RRS Chaotic EA |
//|                                             rajeeevrrs@gmail.com |
//+------------------------------------------------------------------+
#property copyright "RRS Chaotic EA"
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

extern string __RestricationSettings__ = "***Restrication Settings***";
extern int MaxOpenTrade = 10;
extern int maxSpread = 100;
extern int Slippage = 3;

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
//int
int gBuyMagic, gSellMagic;
int OrderCount_BuyMagicOPBUY, OrderCount_SellMagicOPSELL;
int BuySellRandomMath;
int Buy_StopLevel, Sell_StopLevel;

//String
string stylecommenttrade;
string BuyStopTradeComment, SellStopTradeComment;
string buy_random_symbol, sell_random_symbol;

//Double
double gSymbolEA_FloatingPL, gBuyFloatingPL, gSellFloatingPL;
double gTargeted_Revenue, gRisk_Money;
double OrderSend_StopLoss, OrderSend_TakeProfit;
double Buy_Lot_Size, Sell_Lot_Size;

//+------------------------------------------------------------------+
//| OnInit                                                           |
//+------------------------------------------------------------------+
int OnInit()
  {

//Magic Numbers
   gBuyMagic    = Magic + 1;
   gSellMagic   = Magic + 11;
   stylecommenttrade = "Chaotic";

//Trade Comments
   BuyStopTradeComment = Trade_Comment + "+" + stylecommenttrade + "+RRS";
   SellStopTradeComment = Trade_Comment + "+" + stylecommenttrade + "+RRS";

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
   BuySellRandomMath = MathRand() % 11;
   OrderCount_BuyMagicOPBUY = trade_count_ordertype(OP_BUY, gBuyMagic);
   OrderCount_SellMagicOPSELL = trade_count_ordertype(OP_SELL, gSellMagic);
   buy_random_symbol = randomsymbol();
   sell_random_symbol = randomsymbol();
   Buy_StopLevel = MarketInfo(buy_random_symbol, MODE_STOPLEVEL) + 2;
   Sell_StopLevel = MarketInfo(sell_random_symbol, MODE_STOPLEVEL) + 2;
   Buy_Lot_Size = RandomLotSize();
   Sell_Lot_Size = RandomLotSize();

//Order Placement
   if(OrderCount_BuyMagicOPBUY + OrderCount_SellMagicOPSELL < MaxOpenTrade)
      Random_OrderSend();

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
void Random_OrderSend()
  {
   if(CheckVolumeValue(Buy_Lot_Size, buy_random_symbol) == true && CheckMoneyForTrade(buy_random_symbol, Buy_Lot_Size, OP_BUY) == true && (BuySellRandomMath == 6 || BuySellRandomMath == 9) &&  MarketInfo(buy_random_symbol, MODE_SPREAD) < maxSpread)
     {
      ResetLastError();
      if(!OrderSend(buy_random_symbol, OP_BUY, Buy_Lot_Size, MarketInfo(buy_random_symbol, MODE_ASK), Slippage, OrderSend_StopLoss = (StopLoss > 0) ? MarketInfo(buy_random_symbol, MODE_ASK) - MathMax(StopLoss, Buy_StopLevel) * Point : 0, OrderSend_TakeProfit = (TakeProfit > 0) ? MarketInfo(buy_random_symbol, MODE_ASK) + MathMax(TakeProfit, Buy_StopLevel) * Point : 0, BuyStopTradeComment, gBuyMagic, 0, clrNONE))
         Print("Buy Order => Error Code : " + GetLastError());
     }

   if(CheckVolumeValue(Sell_Lot_Size, sell_random_symbol) == true && CheckMoneyForTrade(sell_random_symbol, Sell_Lot_Size, OP_SELL) == true && (BuySellRandomMath == 3 || BuySellRandomMath == 8) && MarketInfo(sell_random_symbol, MODE_SPREAD) < maxSpread)
     {
      ResetLastError();
      if(!OrderSend(sell_random_symbol, OP_SELL, Sell_Lot_Size, MarketInfo(sell_random_symbol, MODE_BID), Slippage, OrderSend_StopLoss = (StopLoss > 0) ? MarketInfo(sell_random_symbol, MODE_BID) + MathMax(StopLoss, Sell_StopLevel) * Point : 0, OrderSend_TakeProfit = (TakeProfit > 0) ? MarketInfo(sell_random_symbol, MODE_BID) - MathMax(TakeProfit, Sell_StopLevel) * Point : 0, SellStopTradeComment, gSellMagic, 0, clrNONE))
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
      if(!OrderSelect(pos_0, SELECT_BY_POS, MODE_TRADES)) // Ensure OrderSelect success
         continue;

      if(OrderMagicNumber() != trade_close_magic)
         continue;

      ResetLastError(); // Always reset before an action to ensure accurate error reporting

      if(OrderType() == OP_BUY || OrderType() == OP_SELL)
      {
         // Close market orders (Buy/Sell)
         if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
            Print(__FUNCTION__, " => Market Order failed to close, error code: ", GetLastError());
      }
      else
      {
         // Close pending orders (BuyLimit, SellLimit, BuyStop, SellStop)
         if(!OrderDelete(OrderTicket()))
            Print(__FUNCTION__, " => Pending Order failed to close, error code: ", GetLastError());
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
           "\n                                             :: ===>RRS Chaotic EA<==="
           "\n                                             ------------------------------------------------" +
           "\n                                             :: Maxium Open Trade             : " + MaxOpenTrade +
           "\n                                             :: Lot Size                         : (Min Lot : " + minLot_Size + ") |:| (Max Lot : " + maxLot_Size + ")" +
           "\n                                             :: Take Profit                      : " + TakeProfit +
           "\n                                             :: Stop Loss                      : " + StopLoss +
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
   string randomsymbol_pairs[] = {"USD", "GBP", "AUD", "CAD", "JPY", "XAU", "XAG",
                                  "EUR", "CHF", "SDG", "HKD", "NZD"
                                 };

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
//|        Random Lot size                                           |
//+------------------------------------------------------------------+
double RandomLotSize()
  {
// Generate a random value within the specified range
   double LotrandomValue = minLot_Size + (maxLot_Size - minLot_Size) * MathRand() / 32767.0;

// Normalize to 2 decimal places
   return NormalizeDouble(LotrandomValue, 2);
  }
//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume, string randomsymbol_checkvolume)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(randomsymbol_checkvolume,SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      PrintFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(randomsymbol_checkvolume,SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      PrintFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(randomsymbol_checkvolume,SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      PrintFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                  volume_step,ratio*volume_step);
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
