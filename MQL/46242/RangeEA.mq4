//+------------------------------------------------------------------+
//|                                                    RangeEA.mq4   |
//|                        Copyright 2023, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "2023, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.01"
#property strict

#define VER "1.01"

input int MAGICMA = 3937; // Magic Number; a unique number to assign to the orders
input int startTrade = 0; // Trade Start Time; Do not trade before this hour
input int endTrade = 24; // Trade End Time; Do not trade after this hour
input bool closeAllAtEndTrade = true; // Close all trades and orders after Trade End Time despite profit
input int maxOpenOrders = 5; // Adjust this to your broker's maximum orders limit
input int numberOfOrders = 10; // Number of pending orders to open
input double lotSize = 0.01;   // Lot size for each order
input bool resetOrdersDaily = true; // Input parameter to reset pending orders every day
input double stopLossPoints = 60; // Input parameter for the minimum stop loss in points
input double takeProfitPoints = 60; // Input parameter for the minimum take profit in points
input double stopLossMultiplier = 3.0; // Input parameter for stop loss multiplier
input double takeProfitMultiplier = 1.0; // Input parameter for take profit multiplier
input double targetPercentage = 8.0; // Percentage of gain to trigger closing

// Global array to store order tickets
int orderTickets[];
int orderTicketsCount = 0;

// Global array to store executed order prices
double executedOrderPrices[];
int executedOrdersCount = 0;
double StopLevel;

int openOrderCount = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   StopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL);
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
// Deinitialize logic here
   ResetPendingOrders();
  }

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
datetime lastBarTime = 0; // Store the opening time of the last bar
void OnTick()
  {

   datetime currentBarTime = Time[0]; // Get the opening time of the current bar

   if(Hour() >= startTrade && Hour() < endTrade)
     {

      // Check if a new bar has opened
      if(currentBarTime != lastBarTime)
        {
         lastBarTime = currentBarTime; // Update the last bar's opening time

         // Calculate and place new pending orders
         double tradingRange, rangeHigh, rangeLow;
         CalculateTradingRange(tradingRange, rangeHigh, rangeLow);  // Get the trading range

         double distance = tradingRange / numberOfOrders;
         double currentPrice = Bid;

         if(tradingRange > 0)
           {
            if(IsNewDay() || orderTicketsCount == 0)
              {
               if(resetOrdersDaily)
                 {
                  ResetPendingOrders();
                 }
               if(orderTicketsCount == 0)
                 {
                  for(int i = 0; i < numberOfOrders; i++)
                    {
                     double price = NormalizeDouble(rangeLow + distance * i, Digits);

                     if(price > currentPrice)
                        PlaceSellLimitOrder(price);
                     else
                        PlaceBuyLimitOrder(price);
                    }
                 }
              }
           }
         // Check if any orders have been executed
         CheckExecutedOrders();

         // Close orders if gain exceeds target percentage
         CloseOrdersOnGain();

         // Check whether the number of executed orders is close to numberOfOrders
         if(executedOrdersCount > 1 && orderTicketsCount == numberOfOrders - 2)
           {
            double penultimateExecutedPrice = executedOrderPrices[executedOrdersCount - 2];
            if(penultimateExecutedPrice > currentPrice)
               PlaceSellLimitOrder(penultimateExecutedPrice);
            else
               PlaceBuyLimitOrder(penultimateExecutedPrice);
           }
         /*if(orderTicketsCount < numberOfOrders - 2)
           {
            ResetPendingOrders();
           }*/
        }
     }
   else
      if(closeAllAtEndTrade)
        {
         CloseAllOrders();
         ResetPendingOrders();
        }

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsNewDay()
  {
   static datetime lastCheckedTime = 0;
   datetime currentTime = TimeCurrent();

   if(TimeDay(currentTime) != TimeDay(lastCheckedTime))
     {
      lastCheckedTime = currentTime;
      return true;
     }

   return false;
  }

//+------------------------------------------------------------------+
//| Function to get the trading range                                |
//+------------------------------------------------------------------+
// Calculate and set the trading range, high, and low based on the current pivot
void CalculateTradingRange(double &outRange, double &outHigh, double &outLow)
  {
// Set the timeframe to weekly
   int weeklyTimeframe = PERIOD_W1;
   int shiftedIndex = 0; // Shift to previous weekly bar

// Calculate pivot, support, and resistance levels using weekly data
   double pivot = (iHigh(NULL, weeklyTimeframe, shiftedIndex) + iLow(NULL, weeklyTimeframe, shiftedIndex) + iClose(NULL, weeklyTimeframe, shiftedIndex)) / 3.0;
   double support = iLow(NULL, weeklyTimeframe, iLowest(NULL, weeklyTimeframe, MODE_LOW, 2, shiftedIndex));
   double resistance = iHigh(NULL, weeklyTimeframe, iHighest(NULL, weeklyTimeframe, MODE_HIGH, 2, shiftedIndex));

// Set trading range, high, and low
   outRange = resistance - support;
   outHigh = resistance;
   outLow = support;
  }

//+------------------------------------------------------------------+
//| Function to place a SELL LIMIT order                            |
//+------------------------------------------------------------------+
void PlaceSellLimitOrder(double price)
  {

   CountOpenMarketOrders(); // Update the open market order count
   CountOpenPendingOrders(); // Update the open pending order count

   int totalOpenOrders = openOrderCount;

   if(totalOpenOrders > maxOpenOrders)
     {
      // Handle the case where the maximum limit is reached
      Print("Maximum open orders reached. Cannot place a new order.");
      return;
     }

   ResetLastError();
   double stopLoss = NormalizeDouble(price + (price - Ask) * stopLossMultiplier, Digits); // Calculate stop loss based on multiplier
   double takeProfit = NormalizeDouble(price - (price - Ask) * takeProfitMultiplier, Digits); // Calculate take profit based on multiplier

// Set minimum acceptable values for stopLoss and takeProfit
   double minStopLoss = NormalizeDouble(price + (stopLossPoints * Point), Digits);
   double minTakeProfit = NormalizeDouble(price - (takeProfitPoints * Point), Digits);

   if(stopLoss < minStopLoss || takeProfit > minTakeProfit)
     {
      Print("Warning: Stop Loss or Take Profit values too small. Order not placed.", stopLoss, minStopLoss);
      return;
     }

   string description = "";
   if(CheckVolumeValue(CheckLotSize(), description) && CheckMoneyForTrade(Symbol(), CheckLotSize(), OP_SELL))
     {
      int ticket = OrderSend(Symbol(), OP_SELLLIMIT, CheckLotSize(), price, 2, 0, 0, "RangeEA_"+VER, MAGICMA, 0, clrRed);

      if(ticket > 0)
        {
         if(OrderModify(ticket, price, stopLoss, takeProfit, 0, clrNONE))
           {
            ArrayResize(orderTickets, orderTicketsCount + 1);
            orderTickets[orderTicketsCount] = ticket; // Store the order ticket
            orderTicketsCount++;
           }
         else
           {
            int error = GetLastError();
            if(error == 130)
              {
               Print("Error 130 (ERR_INVALID_STOPS) while modifying SELL LIMIT order. Details:");
               Print("Order Type: OP_SELLLIMIT");
               Print("Open Price: ", price);
               Print("Stop Loss: ", stopLoss);
               Print("Take Profit: ", takeProfit);
              }
            else
              {
               Print("Error modifying SELL LIMIT order. Error code: ", error);
              }
           }

         if(GetLastError())
           {
            if(!OrderDelete(ticket, clrNONE))
              {
               Print("WARNING: Could not delete the wrong order ticket: ", ticket);
              }
           }
        }
      else
        {
         int error = GetLastError();
         if(error == 130)
           {
            Print("Error 130 (ERR_INVALID_STOPS) while placing SELL LIMIT order. Details:");
            Print("Order Type: OP_SELLLIMIT");
            Print("Open Price: ", price);
            Print("Stop Loss: ", stopLoss);
            Print("Take Profit: ", takeProfit);
           }
         else
           {
            Print("Error placing SELL LIMIT order. Error code: ", error);
           }
        }
     }
  }

//+------------------------------------------------------------------+
//| Function to place a BUY LIMIT order                             |
//+------------------------------------------------------------------+
void PlaceBuyLimitOrder(double price)
  {

   CountOpenMarketOrders(); // Update the open market order count
   CountOpenPendingOrders(); // Update the open pending order count

   int totalOpenOrders = openOrderCount;

   if(totalOpenOrders > maxOpenOrders)
     {
      // Handle the case where the maximum limit is reached
      Print("Maximum open orders reached. Cannot place a new order.");
      return;
     }

   ResetLastError();
   double stopLoss = NormalizeDouble(price - (Bid - price) * stopLossMultiplier, Digits); // Calculate stop loss based on multiplier
   double takeProfit = NormalizeDouble(price + (Bid - price) * takeProfitMultiplier, Digits); // Calculate take profit based on multiplier

// Set minimum acceptable values for stopLoss and takeProfit
   double minStopLoss = NormalizeDouble(price - (stopLossPoints * Point), Digits);
   double minTakeProfit = NormalizeDouble(price + (takeProfitPoints * Point), Digits);

   if(stopLoss > minStopLoss || takeProfit < minTakeProfit)
     {
      Print("Warning: Stop Loss or Take Profit values too small. Order not placed.", stopLoss, minStopLoss);
      return;
     }

   string description = "";
   if(CheckVolumeValue(CheckLotSize(), description) && CheckMoneyForTrade(Symbol(), CheckLotSize(), OP_BUY))
     {
      int ticket = OrderSend(Symbol(), OP_BUYLIMIT, CheckLotSize(), price, 2, 0, 0, "RangeEA_"+VER, MAGICMA, 0, clrBlue);

      if(ticket > 0)
        {
         if(OrderModify(ticket, price, stopLoss, takeProfit, 0, clrNONE))
           {
            ArrayResize(orderTickets, orderTicketsCount + 1);
            orderTickets[orderTicketsCount] = ticket; // Store the order ticket
            orderTicketsCount++;
           }
         else
           {
            int error = GetLastError();
            if(error == 130)
              {
               Print("Error 130 (ERR_INVALID_STOPS) while modifying BUY LIMIT order. Details:");
               Print("Order Type: OP_BUYLIMIT");
               Print("Open Price: ", price);
               Print("Stop Loss: ", stopLoss);
               Print("Take Profit: ", takeProfit);
              }
            else
              {
               Print("Error modifying BUY LIMIT order. Error code: ", error);
              }
           }

         if(GetLastError())
           {
            if(!OrderDelete(ticket, clrNONE))
              {
               Print("WARNING: Could not delete the wrong order ticket: ", ticket);
              }
           }
        }
      else
        {
         int error = GetLastError();
         if(error == 130)
           {
            Print("Error 130 (ERR_INVALID_STOPS) while placing BUY LIMIT order. Details:");
            Print("Order Type: OP_BUYLIMIT");
            Print("Open Price: ", price);
            Print("Stop Loss: ", stopLoss);
            Print("Take Profit: ", takeProfit);
           }
         else
           {
            Print("Error placing BUY LIMIT order. Error code: ", error);
           }
        }
     }
  }


//+------------------------------------------------------------------+
//| Function to check and remove executed orders                     |
//+------------------------------------------------------------------+
void CheckExecutedOrders()
  {
   for(int i = orderTicketsCount - 1; i >= 0; i--)
     {
      if(OrderSelect(orderTickets[i], SELECT_BY_TICKET, MODE_TRADES))
        {
         if(OrderSymbol()==Symbol())
           {
            if(OrderMagicNumber()!=MAGICMA)
              {
               continue;
              }
            if(OrderType() == OP_BUY || OrderType() == OP_SELL)
              {
               if(OrderCloseTime() > 0)
                 {
                  double executedPrice = OrderOpenPrice();
                  ArrayRemove(orderTickets, i); // Remove the order ticket from the array
                  ArrayResize(executedOrderPrices, executedOrdersCount + 1);
                  executedOrderPrices[executedOrdersCount] = executedPrice;
                  executedOrdersCount++;
                  orderTicketsCount--;
                 }
              }
           }
        }
     }
  }

//+------------------------------------------------------------------+
//| Close orders if gain exceeds target percentage                   |
//+------------------------------------------------------------------+
void CloseOrdersOnGain()
  {
   double gainPercentage = (AccountProfit() / AccountBalance()) * 100;

   if(gainPercentage >= targetPercentage)
     {
      // Close all orders
      int totalClosed = 0;
      for(int i = OrdersTotal() - 1; i >= 0; i--)
        {
         if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
           {
            if(OrderSymbol()==Symbol())
              {
               if(OrderMagicNumber()!=MAGICMA)
                 {
                  continue;
                 }
               if(OrderType() <= OP_SELL)
                 {
                  double orderClosePrice = MarketInfo(OrderSymbol(), MODE_BID);
                  // Get current market prices.
                  RefreshRates();
                  // Select the correct price.
                  if(OrderType() == OP_BUY)
                     orderClosePrice = Bid;
                  if(OrderType() == OP_SELL)
                     orderClosePrice = Ask;

                  bool result = false;

                  // Retry order closure with adjusted price if requote occurs
                  for(int retryCount = 0; retryCount < 3; retryCount++)
                    {
                     result = OrderClose(OrderTicket(), OrderLots(), orderClosePrice, 2, clrAntiqueWhite);

                     if(result)
                       {
                        Print("Order closed successfully: ", OrderType(), " ", OrderLots(), " lots at price ", orderClosePrice);
                        totalClosed++;
                        break;
                       }
                     else
                       {
                        if(GetLastError() == 138)  // Requote error
                          {
                           orderClosePrice = MarketInfo(OrderSymbol(), MODE_BID) + MarketInfo(OrderSymbol(), MODE_POINT) * retryCount;
                           Sleep(1000); // Wait for a second before retrying
                          }
                        else
                          {
                           Print("Error closing order: ", GetLastError());
                           break;
                          }
                       }
                    }
                 }
              }
           }
        }

      if(totalClosed > 0)
        {
         ResetPendingOrders();
        }
     }
  }

// Function to remove an element from an array
void ArrayRemove(int& array[], int index)
  {
   if(index >= 0 && index < ArraySize(array))
     {
      for(int i = index; i < ArraySize(array) - 1; i++)
         array[i] = array[i + 1];

      ArrayResize(array, ArraySize(array) - 1);
     }
  }
//---------------------------------------------------------------------------
void CloseAllOrders()
  {
   RefreshRates();

   bool   Result=false;
   int    i,Pos,Error=GetLastError();
   int    Total=OrdersTotal();

   if(Total>0)
     {
      for(i=Total-1; i>=0; i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderSymbol()==Symbol())
              {
               if(OrderMagicNumber()!=MAGICMA)
                 {
                  continue;
                 }
               Pos=OrderType();
               if(Pos==OP_BUY)
                 {
                  Result=OrderClose(OrderTicket(), OrderLots(), Bid, 2, clrBlue);
                 }
               if(Pos==OP_SELL)
                 {
                  Result=OrderClose(OrderTicket(), OrderLots(), Ask, 2, clrRed);
                 }
               if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT))
                 {
                  Result=OrderDelete(OrderTicket(), CLR_NONE);
                 }
               //-----------------------
               if(Result!=true)
                 {
                  Error=GetLastError();
                 }
               else
                  Error=0;
               //-----------------------
              }//if
           }//if
        }//for
     }//if

   Sleep(20);
   return;
  }
//+------------------------------------------------------------------+
void ResetPendingOrders()
  {
   orderTicketsCount = 0;
   executedOrdersCount = 0;
// Remove existing pending orders
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol()==Symbol())
           {
            if(OrderMagicNumber()!=MAGICMA)
              {
               continue;
              }
            if(OrderType() == OP_BUYLIMIT || OrderType() == OP_SELLLIMIT)
              {
               if(OrderDelete(OrderTicket()))
                 {
                  Print("Pending order deleted. Ticket: ", OrderTicket());
                 }
               else
                 {
                  Print("Error deleting pending order. Ticket: ", OrderTicket(), " Error: ", GetLastError());
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume,string &description)
  {

   if(AccountBalance() < 10)
     {
      Comment("minimum balances is $10");
      return(false);
     }

//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                               volume_step,ratio*volume_step);
      return(false);
     }
   description="Correct volume value";
   return(true);
  }
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb, double lots,int type)
  {
   double free_margin=AccountFreeMarginCheck(symb,type, lots);
//-- if there is not enough money
   if(free_margin<0)
     {
      string oper=(type==OP_BUY)? "Buy":"Sell";
      Print("Not enough money for ", oper," ",lots, " ", symb, " Error code=",GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
  }

//+------------------------------------------------------------------+
double CheckLotSize()
  {
   double minLotSize = MarketInfo(Symbol(), MODE_MINLOT);
   double maxLotSize = MarketInfo(Symbol(), MODE_MAXLOT);

   if(lotSize < minLotSize)
     {
      Print("Warning: Lot size is too small. Minimum lot size for ", Symbol(), " is ", minLotSize);
      return minLotSize;
     }
   else
      if(lotSize > maxLotSize)
        {
         Print("Warning: Lot size is too large. Maximum lot size for ", Symbol(), " is ", maxLotSize);
         return maxLotSize;
        }

// Lot size is within the valid range
   return lotSize;
  }
//+------------------------------------------------------------------+
// Function to count open market orders
void CountOpenMarketOrders()
  {
   openOrderCount = 0;
   for(int i = 0; i < OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol()==Symbol())
           {
            if(OrderMagicNumber()!=MAGICMA)
              {
               continue;
              }
            if(OrderType() == OP_BUY || OrderType() == OP_SELL)
              {
               openOrderCount++;
              }
           }
        }
     }
  }
// Function to count open pending orders
void CountOpenPendingOrders()
  {
   for(int i = 0; i < OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
        {
         if(OrderSymbol()==Symbol())
           {
            if(OrderMagicNumber()!=MAGICMA)
              {
               continue;
              }
            if(OrderType() == OP_BUYSTOP || OrderType() == OP_SELLSTOP ||
               OrderType() == OP_BUYLIMIT || OrderType() == OP_SELLLIMIT)
              {
               openOrderCount++;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
