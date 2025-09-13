//+------------------------------------------------------------------+
//| AutoCloseOnProfitLoss_EA.mq5                                     |
//| Copyright 2025, Your Name                                        |
//| Description: EA to auto-close all positions on profit/loss       |
//+------------------------------------------------------------------+
#property copyright "Your Name, 2025"
#property link      "https://www.mql5.com"
#property version   "1.02"
#property strict

#include <Trade\Trade.mqh>

//--- Input parameters
input double TargetProfit = 100.0;    // Target profit (in account currency)
input double MaxLoss = -50.0;         // Max loss (in account currency, negative value)
input bool EnableProfitClose = true;  // Enable closing on target profit
input bool EnableLossClose = true;    // Enable closing on max loss
input bool ShowAlerts = true;         // Show alerts when closing positions

//--- Global variables
CTrade trade;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   // Check connection to server
   
   
   // Validate inputs
   if(TargetProfit <= 0 && EnableProfitClose)
   {
      Alert("Error: TargetProfit must be greater than 0!");
      Print("Error: TargetProfit must be greater than 0!");
      return(INIT_PARAMETERS_INCORRECT);
   }
   if(MaxLoss >= 0 && EnableLossClose)
   {
      Alert("Error: MaxLoss must be a negative value!");
      Print("Error: MaxLoss must be a negative value!");
      return(INIT_PARAMETERS_INCORRECT);
   }
   
   // Check if trading is allowed
   if(!AccountInfoInteger(ACCOUNT_TRADE_ALLOWED))
   {
      Alert("Error: Trading is not allowed for this account!");
      Print("Error: Trading is not allowed for this account!");
      return(INIT_FAILED);
   }
   
   Print("EA initialized successfully at ", TimeToString(TimeCurrent(), TIME_DATE|TIME_MINUTES|TIME_SECONDS));
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Print("EA deinitialized. Reason: ", reason, " | Time: ", TimeToString(TimeCurrent(), TIME_DATE|TIME_MINUTES|TIME_SECONDS));
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   // Calculate total profit/loss
   double totalProfit = CalculateTotalProfit();
   Print("Total Profit/Loss on tick: ", DoubleToString(totalProfit, 2), " | Time: ", TimeToString(TimeCurrent(), TIME_DATE|TIME_MINUTES|TIME_SECONDS));
   
   // Check if profit/loss conditions are met
   bool shouldClose = false;
   string closeReason = "";
   
   if(EnableProfitClose && totalProfit >= TargetProfit)
   {
      shouldClose = true;
      closeReason = "Target profit reached: " + DoubleToString(totalProfit, 2);
   }
   else if(EnableLossClose && totalProfit <= MaxLoss)
   {
      shouldClose = true;
      closeReason = "Max loss reached: " + DoubleToString(totalProfit, 2);
   }
   
   // Close all positions if conditions are met
   if(shouldClose)
   {
      if(CloseAllPositions())
      {
         if(ShowAlerts)
         {
            Alert("All positions closed! Reason: ", closeReason);
         }
         Print("All positions closed at: ", TimeToString(TimeCurrent(), TIME_DATE|TIME_MINUTES|TIME_SECONDS), 
               " | Reason: ", closeReason);
      }
      else
      {
         Print("Failed to close all positions! Check logs for details.");
      }
   }
}

//+------------------------------------------------------------------+
//| Calculate total profit/loss of all open positions                |
//+------------------------------------------------------------------+
double CalculateTotalProfit()
{
   double totalProfit = 0.0;
   
   // Loop through all open positions
   int totalPositions = PositionsTotal();
   for(int i = totalPositions - 1; i >= 0; i--)
   {
      ulong ticket = PositionGetTicket(i);
      if(PositionSelectByTicket(ticket))
      {
         totalProfit += PositionGetDouble(POSITION_PROFIT);
      }
      else
      {
         Print("Warning: Could not select position with ticket #", ticket);
      }
   }
   
   return totalProfit;
}

//+------------------------------------------------------------------+
//| Close all open positions                                         |
//+------------------------------------------------------------------+
bool CloseAllPositions()
{
   bool allClosed = true;
   
   // Loop through all open positions
   int totalPositions = PositionsTotal();
   for(int i = totalPositions - 1; i >= 0; i--)
   {
      ulong ticket = PositionGetTicket(i);
      if(PositionSelectByTicket(ticket))
      {
         string symbol = PositionGetString(POSITION_SYMBOL);
         trade.SetDeviationInPoints(10); // Slippage tolerance
         
         // Close the position
         if(!trade.PositionClose(ticket))
         {
            Print("Failed to close position #", ticket, " | Symbol: ", symbol, 
                  " | Error: ", trade.ResultRetcode());
            allClosed = false;
         }
         else
         {
            Print("Closed position #", ticket, " | Symbol: ", symbol, " | Result: ", trade.ResultRetcode());
         }
      }
      else
      {
         Print("Error: Could not select position with ticket #", ticket);
         allClosed = false;
      }
   }
   
   return allClosed;
}
//+------------------------------------------------------------------+