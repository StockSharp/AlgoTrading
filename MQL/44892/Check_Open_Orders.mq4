//+------------------------------------------------------------------+
//|                                            Check_Open_Orders.mq4 |
//|                                                  Francisco Rayol |
//|                      https://www.mql5.com/en/users/rayolf/seller |
//+------------------------------------------------------------------+
#property copyright "Francisco Rayol"
#property link      "https://www.mql5.com/en/users/rayolf/seller"
#property version   "1.00"
#property strict
#property show_inputs

enum CheckOpenOrdersMode
  {
   CheckAllTypes=0,     // Function checks for open market Buy and Sell orders
   CheckOnlyBuy=1,      // Function checks for open market Buy orders only
   CheckOnlySell=2,     // Function checks for open market Sell orders only
  };

input  int MAGICMA = 556600;        // Define EA's MagicNumber
input  double stoploss = 100;       // Stop Loss points defined by the user
input  double takeprofit = 400;     // Take Profit points defined by the user
extern double lot = 0.01;          // Lot size defined by the user
input  int    slippage = 7;         // Slippage allowed defined by the user
input  int    wait_time = 2000;     // Time in milisseconds between the orders opening
input CheckOpenOrdersMode Check_Open_Orders = CheckAllTypes;

string option_chosen,orders,current_status; // String variables to be used on the Comment function

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(Check_Open_Orders==1)
     {
      option_chosen="Checking for buy market open orders only";
      orders = "buy";
     }
   else
      if(Check_Open_Orders==2)
        {
         option_chosen="Checking sell market open orders only";
         orders = "sell";
        }
      else
        {
         option_chosen="Checking all market open orders";
         orders = "buy and sell";
        }

   int ticket;

   string symb=Symbol();

   double sl = stoploss*Point;
   double tp = takeprofit*Point;

//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
//--- minimal allowed volume of trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(lot<min_volume)
     {
      lot=min_volume;
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(lot>max_volume)
     {
      lot=max_volume;
     }
//+--------------------------------------------------------------------------------------------------------------------------------------------------------+
//| At the start of the Expert Advisor's running, I set this EA to open three sample orders to make it visible the "Check Open Orders" function working. |
//| After the two first orders being sent successfully I set the Sleep() function to make the EA wait some seconds before opening a new trade.           |
//+------------------------------------------------------------------------------------------------------------------------------------------------------+
   if(CheckMoneyForTrade(symb,lot,0))
     {

      ticket=OrderSend(symb,0,lot,Ask,slippage,Bid-sl,Ask+tp,"",MAGICMA,0,Blue);
      if(ticket>0)
         Sleep(wait_time);

     }

   if(CheckMoneyForTrade(symb,lot,0))
     {

      ticket=OrderSend(symb,0,lot,Ask,slippage,Bid-sl,Ask+tp,"",MAGICMA,0,Blue);
      if(ticket>0)
         Sleep(wait_time);

     }

   if(CheckMoneyForTrade(symb,lot,1))
     {
      ticket=OrderSend(symb,1,lot,Bid,slippage,Ask+sl,Bid-tp,"",MAGICMA,0,Red);
      if(ticket>0)
         Print("Samples orders sent.");
     }
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   Comment(""); // It clears out the comment from the active chart when the Expert Advisor is removed.
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   Comment("\n Option chosen: "+option_chosen+".\n Are there any current "+orders+" open orders? "+(CheckOpenOrders()==true ? "Yes" : "No"));
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Check Open Orders Function                                       |
//+------------------------------------------------------------------+
bool CheckOpenOrders()
  {


   for(int i = 0 ; i < OrdersTotal() ; i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(Check_Open_Orders==CheckOnlyBuy)
           {
            if(OrderSymbol() == Symbol() && OrderMagicNumber()==MAGICMA && OrderType()==OP_BUY)
               return(true);
           }
         else
            if(Check_Open_Orders==CheckOnlySell)
              {
               if(OrderSymbol() == Symbol() && OrderMagicNumber()==MAGICMA && OrderType()==OP_SELL)
                  return(true);
              }
            else
              {
               if(OrderSymbol() == Symbol() && OrderMagicNumber()==MAGICMA)
                  return(true);
              }
        }
     }

   return(false);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb, double lots,int type)
  {
   double free_margin=AccountFreeMarginCheck(symb,type,lots);
//-- if there is not enough money
   if(free_margin<0)
     {
      return(false);
     }
//--- checking successful
   return(true);
  }
//+------------------------------------------------------------------+
