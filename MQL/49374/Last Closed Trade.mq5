// variables
string DayStart = "00:00"; // Day Start Time
double LastClosed_Profit; // Last Closed trade profit
string TradeSymbol, TradeType;



// Expert Initializing --------------------
int OnInit()
  {
   return(INIT_SUCCEEDED);
  }

// Expert DeInitializing -------------------
void OnDeinit(const int reason)
  {

  }

// Expert OnTick --------------------------
void OnTick()
  {
// check for last closed trade.
   CheckLastClosed();

  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckLastClosed()
  {
   datetime HistoryTime = StringToTime(DayStart);

// history from "Day begining to current time
   if(HistorySelect(HistoryTime,TimeCurrent()))
     {
      int Total = HistoryDealsTotal();

      // Get the last deal ticket number and select it to furthur work.
      ulong Ticket = HistoryDealGetTicket(Total -1);

      // Get what you need to get.
      LastClosed_Profit = NormalizeDouble(HistoryDealGetDouble(Ticket,DEAL_PROFIT),2);
      TradeSymbol      = HistoryOrderGetString(Ticket,ORDER_SYMBOL);

      // Identify a sell trade.
      if(HistoryDealGetInteger(Ticket,DEAL_TYPE) == DEAL_TYPE_BUY)
        {
         TradeType = "Sell Trade";
        }

      // Identify a buy trade
      if(HistoryDealGetInteger(Ticket,DEAL_TYPE) == DEAL_TYPE_SELL)
        {
         TradeType = "Buy Trade";
        }

      // chart out put.
      Comment("\n","Deals Total - :  ", Total,
              "\n","Last Deal Ticket - :  ", Ticket,
              "\n", "Last Closed Profit -:  ", LastClosed_Profit,
              "\n", "Last Trade was -:  ", TradeType);

     }
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
