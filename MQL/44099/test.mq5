// Include necessary files
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>

// Input parameters
input double RiskPercentage = 1.0; // Risk percentage per trade
input double ProfitPercentage = 2.0; // Profit percentage per trade
input double TrailingStopPoints = 50; // Trailing stop points

// Declare global variables
CTrade trade;
CSymbolInfo symbolInfo;

// OnInit function
int OnInit()
{
   if (!symbolInfo.Name(Symbol()))
   {
      Print("Error: SymbolInfo not found");
      return INIT_FAILED;
   }
   return INIT_SUCCEEDED;
}

// OnDeinit function
void OnDeinit(const int reason)
{
}

// OnTick function
void OnTick()
{
   CheckTrades();
}

void CheckTrades()
{
   double accountBalance = AccountInfoDouble(ACCOUNT_BALANCE);
   double riskAmount = accountBalance * RiskPercentage / 100.0;
   double profitAmount = accountBalance * ProfitPercentage / 100.0;
  
   for (int i = PositionsTotal() - 1; i >= 0; i--)
   {
      ulong ticket = PositionGetTicket(i);
      if (ticket > 0)
      {
         string orderSymbol = PositionGetString(POSITION_SYMBOL);
         if (Symbol() == orderSymbol)
         {
            double orderOpenPrice = PositionGetDouble(POSITION_PRICE_OPEN);
            double orderStopLoss = PositionGetDouble(POSITION_SL);
            double orderTakeProfit = PositionGetDouble(POSITION_TP);
            ENUM_POSITION_TYPE orderType = ENUM_POSITION_TYPE(PositionGetInteger(POSITION_TYPE));
            
            if (orderType == POSITION_TYPE_BUY)
            {
               double currentProfit = (symbolInfo.Ask() - orderOpenPrice) * symbolInfo.Point();
               if (currentProfit >= profitAmount || currentProfit <= -riskAmount)
               {
                  trade.PositionClose(ticket);
               }
               else
               {
                  // Update stop loss with a trailing stop
                  double newStopLoss = symbolInfo.Bid() - TrailingStopPoints * symbolInfo.Point();
                  if (newStopLoss > orderStopLoss && newStopLoss < orderOpenPrice)
                  {
                     trade.PositionModify(ticket, newStopLoss, orderTakeProfit);
                  }
               }
            }
            else if (orderType == POSITION_TYPE_SELL)
            {
               double currentProfit = (orderOpenPrice - symbolInfo.Bid()) * symbolInfo.Point();
               if (currentProfit >= profitAmount || currentProfit <= -riskAmount)
               {
                  trade.PositionClose(ticket);
               }
               else
               {
                  // Update stop loss with a trailing stop
                  double newStopLoss = symbolInfo.Ask() + TrailingStopPoints * symbolInfo.Point();
                  if (newStopLoss < orderStopLoss && newStopLoss > orderOpenPrice)
                  {
                     trade.PositionModify(ticket, newStopLoss, orderTakeProfit);
                  }
               }
            }
         }
      }
   }
}
