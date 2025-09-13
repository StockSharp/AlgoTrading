//+------------------------------------------------------------------+
//| Order_Close_Old.mq4                                              |
//|   Immediate close of the oldest order.                           |
//+------------------------------------------------------------------+
#property strict
//--- description
#property description "Immediate close of the oldest order."

// Global names for this iteration (currency pair)
string   GMaxSlip    = "GMaxSlip"+Symbol();
string   GStatus     = "GStatus"+Symbol();

double   MaxSlip     = 3;
int      iMaxSlip;


//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
// Can we create an order?
   if (!IsTradeAllowed()) return; 

   
// Load GLOBAL variables
   MaxSlip = GlobalVariableGet(GMaxSlip);

//--- Create the Close order
   iMaxSlip = int(NormalizeDouble(MaxSlip,0));
   
   int total  = OrdersTotal();

   if (total < 1)
   {
      Print("No orders to delete for ",Symbol());
      return;
   }
   for (int pos = 0; pos < total; pos++)                            // Start with oldest
   {
      if (!OrderSelect(pos,SELECT_BY_POS,MODE_TRADES)) continue;    // May not still be valid
      if (OrderSymbol() == Symbol())                                // Is this our trade?
      {
         while (IsTradeContextBusy()) Sleep(100);
         if (OrderType()==OP_BUY)                  // Close One or All
            if (OrderClose(OrderTicket(),OrderLots(),Bid,iMaxSlip,clrViolet)) 
               break;
            else
               Print("Error closing OP_BUY order : ",GetLastError());
         else
         if (OrderType()==OP_SELL)                 // Close One or All
            if (OrderClose(OrderTicket(),OrderLots(),Ask,iMaxSlip,clrViolet)) 
               break;
            else
               Print("Error closing OP_SELL order : ",GetLastError());
      }
   }
   // Tell Order_EA that we tried to delete One order
   datetime Temp = GlobalVariableSet(GStatus, 2.0);

//--- Exit
}
