//+------------------------------------------------------------------+
//|                                           TemplateEAbyMarket.mq4 |
//|                                                   JorgeDeveloper |
//|                     https://www.mql5.com/en/users/jorgedeveloper |
//+------------------------------------------------------------------+
#property copyright "JorgeDeveloper"
#property link      "https://www.mql5.com/en/users/jorgedeveloper"
#property version   "1.00"
#property strict

extern int Take_Profit              = 50;          // Take profit / points
extern int Stop_Loss                = 100;         // Stop loss / points
extern double FixedLot              = 0.01;        // Lot
extern int MaxOrders                = 1;           // Max open orders

extern string OtherSettings = "=============================== Other Settings ===============================";
extern int Magic                    = 2122122;     // Magic number
extern int Slippage                 = 15;          // Slippage

string Signal = "empty";

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   Signal();
   OpenOrder();
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Get signal                                                       |
//+------------------------------------------------------------------+
void Signal()
  {

   RefreshRates();

   double macdMain = iMACD(NULL, 0, 12, 26, 9, PRICE_CLOSE, MODE_MAIN, 1);
   double macdSignal = iMACD(NULL, 0, 12, 26, 9, PRICE_CLOSE, MODE_SIGNAL, 1);

   double macdMainPrev = iMACD(NULL, 0, 12, 26, 9, PRICE_CLOSE, MODE_MAIN, 2);
   double macdSignalPrev = iMACD(NULL, 0, 12, 26, 9, PRICE_CLOSE, MODE_SIGNAL, 2);

   Signal = "empty";

   if(
      (macdMain > 0 && macdSignal > 0) &&
      (macdMainPrev < macdSignalPrev) &&
      (macdMain > macdSignal)
   )
     {
      Signal = "buy";
     }

   if(
      (macdMain < 0 && macdSignal < 0) &&
      (macdMainPrev > macdSignalPrev) &&
      (macdMain < macdSignal)

   )
     {
      Signal = "sell";
     }
  }

//+------------------------------------------------------------------+
//| Allow open orders                                                |
//+------------------------------------------------------------------+
void OpenOrder()
  {

   RefreshRates();

   double lot = CheckVolumeValue(FixedLot);

   if(IsNewOrderAllowed() && CountMyOrders("all") < MaxOrders)
     {

      // Buy
      if(Signal == "buy")
        {

         if(! CheckMoneyForTrade(Symbol(), lot, OP_BUY))
           {
            return;
           }

         double price = NormalizeDouble(Ask, Digits);
         double sl = ((Stop_Loss == 0) ? 0: getStopLoss(Stop_Loss, OP_BUY));
         double tp = ((Take_Profit == 0) ? 0: getTakeProfit(Take_Profit, OP_BUY));

         if(OrderSend(Symbol(), OP_BUY, lot, price, Slippage, sl, tp, "Order BUY", Magic, 0, clrBlue) == -1)
           {
            Print("================================================");
            Print("OrderSend - Buy error #",GetLastError());
            Print("StopLoss", sl);
            Print("Price", price);
            Print("TakeProfit", tp);
            Print("=================================================");

            return;
           }
        }

      // Sell
      if(Signal=="sell")
        {

         if(! CheckMoneyForTrade(Symbol(), lot, OP_SELL))
           {
            return;
           }

         double price = NormalizeDouble(Bid, Digits);
         double sl = ((Stop_Loss == 0) ? 0 : getStopLoss(Stop_Loss, OP_SELL));
         double tp = ((Take_Profit == 0) ? 0 : getTakeProfit(Take_Profit, OP_SELL));

         if(OrderSend(Symbol(), OP_SELL, lot, price, Slippage, sl, tp, "Order SELL", Magic, 0, clrRed) == -1)
           {
            Print("================================================");
            Print("OrderSend - Sell error #", GetLastError());
            Print("TakeProfit", tp);
            Print("Price", price);
            Print("StopLoss", sl);
            Print("=================================================");

            return;
           }
        }
     }
  }

//+------------------------------------------------------------------+
//| Min stop level                                                   |
//+------------------------------------------------------------------+
double getMinStopLevel()
  {
   return (MarketInfo(Symbol(), MODE_STOPLEVEL));
  }

//+------------------------------------------------------------------+
//| Get spread                                                       |
//+------------------------------------------------------------------+
long getSpread()
  {
   return SymbolInfoInteger(Symbol(), SYMBOL_SPREAD);
  }

//+------------------------------------------------------------------+
//| Get stop loss                                                    |
//+------------------------------------------------------------------+
double getStopLoss(double sl, int type)
  {

   long spread = getSpread();
   sl = (sl > getMinStopLevel()) ? sl : getMinStopLevel();

   if(type == OP_BUY)
     {
      return NormalizeDouble(Bid - ((sl) * Point), Digits);
     }

   if(type == OP_SELL)
     {
      return NormalizeDouble(Ask + ((sl) * Point), Digits);
     }

   return 0;
  }

//+------------------------------------------------------------------+
//| Get Take Profit                                                  |
//+------------------------------------------------------------------+
double getTakeProfit(double tp, int type)
  {

   tp = (tp >= getMinStopLevel()) ? tp : getMinStopLevel();

   if(type == OP_BUY)
     {
      return NormalizeDouble(Bid + (tp * Point), Digits);
     }

   if(type == OP_SELL)
     {
      return NormalizeDouble(Ask - (tp * Point), Digits);
     }

   return 0;
  }

//+------------------------------------------------------------------+
//|It allows to know if it is allowed to open a new order            |
//+------------------------------------------------------------------+
bool IsNewOrderAllowed()
  {

   int max_allowed_orders = (int)AccountInfoInteger(ACCOUNT_LIMIT_ORDERS);

   if(max_allowed_orders == 0)
      return true;

   int orders = OrdersTotal();

   return orders < max_allowed_orders;
  }

//+------------------------------------------------------------------+
//| Check the order volume                                           |
//+------------------------------------------------------------------+
double CheckVolumeValue(double volume)
  {

   double minVolume = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN);

   if(volume < minVolume)
     {
      return minVolume;
     }

   double maxVolume=SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX);

   if(volume > maxVolume)
     {
      return maxVolume;
     }

   double volumeStep = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_STEP);

   int ratio = (int)MathRound(volume/volumeStep);

   if(MathAbs(ratio*volumeStep-volume)>0.0000001)
     {
      return ratio*volumeStep;
     }

   return volume;
  }

//+------------------------------------------------------------------+
//| Check if there is enough balance in the account                  |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb, double lots, int type)
  {

   double free_margin = AccountFreeMarginCheck(symb, type, lots);

   if(free_margin<0)
     {
      string oper=(type== OP_BUY)? "Buy" : "Sell" ;
      Print("Not enough money for ", oper, " ",lots, " ", symb, " Error code=", GetLastError());
      return false;
     }

   return true;
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Count the number of open orders                                  |
//+------------------------------------------------------------------+
int CountMyOrders(string type)
  {
   int count = 0;

   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {

      RefreshRates();

      if(OrderSelect(i,SELECT_BY_POS))
        {
         if(OrderMagicNumber() == Magic)
           {
            if(type=="all")                          // OP_BUY=0, OP_SELL=1, OP_BUYLIMIT=2, OP_SELLLIMIT=3,OP_BUYSTOP=4,OP_SELLSTOP=5
               count ++;
            if(type=="pending" && OrderType() > 1)   // OP_BUYLIMIT=2, OP_SELLLIMIT=3,OP_BUYSTOP=4,OP_SELLSTOP=5
               count ++;
            if(type=="buy" && OrderType() == 0)      // OP_BUY
               count ++;
            if(type=="sell" && OrderType() == 1)     // OP_SELL
               count ++;
           }
        }
     }

   return count;
  }
//+------------------------------------------------------------------+
