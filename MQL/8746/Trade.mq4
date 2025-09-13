//+------------------------------------------------------------------+
//|                                                        Trade.mq4 |
//|                                       Copyright © 2009, Tinytjan |
//|                                                 tinytjan@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, Tinytjan"
#property link      "tinytjan@mail.ru"

// magic number needed for filtering orders opened by certain EA
extern int        Magic             =  12345;

// lots size of opening orders. If 0 it is counted automatically
extern double     TradeLots         =  0.1;
// risk for automatic lots size counting
extern double     RiskPercentage    =  0;

// maximum acceptable slippage for the price
extern int        Slippage          =  20;

// maximum loop count for orders opening if failed due to requoting
extern int        TimesToRepeat     =  3;

#include <Logical.mqh>

void WaitForContext()
{
   while (IsTradeContextBusy())
   {
      Sleep(100);
   }
}

void OpenBuy(int MN, int Target, int Loss, double Lot)
{
   int count = 0;
   while (count < TimesToRepeat)
   {
      WaitForContext();
      RefreshRates();
   
      double TP = DoubleIf(Target > 0, Ask + Target*Point, 0);
      double SL = DoubleIf(Loss > 0, Ask - Loss*Point, 0);
   
      double LotsToBid = DoubleIf(Lot == 0, GetLotsToBid(RiskPercentage), Lot);
   
      int res = OrderSend(Symbol(), OP_BUY, LotsToBid, Ask, Slippage, SL, TP, NULL, MN, 0, Blue);
      
      if (res > 0) return;
   }
}

void OpenSell(int MN, int Target, int Loss, double Lot)
{
   int count = 0;
   while (count < TimesToRepeat)
   {
      WaitForContext();
      RefreshRates();
   
      double TP = DoubleIf(Target > 0, Bid - Target*Point, 0);
      double SL = DoubleIf(Loss > 0, Bid + Loss*Point, 0);
   
      double LotsToBid = DoubleIf(Lot == 0, GetLotsToBid(RiskPercentage), Lot);
   
      int res = OrderSend(Symbol(), OP_SELL, LotsToBid, Bid, Slippage, SL, TP, NULL, MN, 0, Red);
      
      if (res > 0) return;
   }
}

void OpenBuyStop(int MN, double Price, int Target, int Loss, double Lot)
{
   double TP = DoubleIf(Target > 0, Price + Target*Point, 0);
   double SL = DoubleIf(Loss > 0, Price - Loss*Point, 0);
   
   double LotsToBid = DoubleIf(Lot == 0, GetLotsToBid(RiskPercentage), Lot);
   
   WaitForContext();
   
   OrderSend(Symbol(), OP_BUYSTOP, LotsToBid, Price, Slippage, SL, TP, NULL, MN, 0, Blue);
}

void OpenSellStop(int MN, double Price, int Target, int Loss, double Lot)
{
   Price = Price + Bid - Ask;
   
   double TP = DoubleIf(Target > 0, Price - Target*Point, 0);
   double SL = DoubleIf(Loss > 0, Price + Loss*Point, 0);
   
   double LotsToBid = DoubleIf(Lot == 0, GetLotsToBid(RiskPercentage), Lot);
   
   WaitForContext();
   
   OrderSend(Symbol(), OP_SELLSTOP, LotsToBid, Price, Slippage, SL, TP, NULL, MN, 0, Red);
}

void CloseSells(int MagicNumber, int Slippage)
{
   for(int i = OrdersTotal() - 1; i >= 0; i--)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == OP_SELL)
      {
         int count = 0;
         while (count < TimesToRepeat)
         {
            WaitForContext();
            
            RefreshRates();
            if (OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, Red))
            {
               break;
            }
            count++;
         }
      }
   }
}

void CloseBuys(int MagicNumber, int Slippage)
{
   for(int i = OrdersTotal(); i >= 0; i--)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == OP_BUY)
      {
         int count = 0;
         while (count < TimesToRepeat)
         {
            WaitForContext();
            
            RefreshRates();
            if (OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, Blue))
            {
               break;
            }
            count++;
         }
      }
   }
}

void DeletePending(int MagicNumber = -1)
{
   for(int i = OrdersTotal() - 1; i >= 0; i--)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber && MagicNumber != -1) continue;
      
      if(OrderType() != OP_SELL && OrderType() != OP_BUY)
      {
         WaitForContext();
         
         OrderDelete(OrderTicket());
      }
   }
}

bool GetActiveOrders(int MagicNumber = -1)
{
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber && MagicNumber != -1) continue;
      
      if(OrderType() == OP_SELL || OrderType() == OP_BUY)
      {
         return (true);
      }
   }
   
   return (false);
}

int GetOrdersCount(int MagicNumber = -1, int Type = -1, string symb = "")
{
   int count = 0;
   
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol() && symb == "") continue;
      // not specified symbol
      if(OrderSymbol() != symb && symb != "" && symb != "all") continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber && MagicNumber != -1) continue;
      
      if(OrderType() == Type || Type == -1)
      {
         count++;
      }
   }
   
   return (count);
}

double GetLotsToBid(double RiskPercentage)
{
   double margin  = MarketInfo(Symbol(), MODE_MARGINREQUIRED);
   double minLot  = MarketInfo(Symbol(), MODE_MINLOT);
   double maxLot  = MarketInfo(Symbol(), MODE_MAXLOT);
   double step    = MarketInfo(Symbol(), MODE_LOTSTEP);
   double account = AccountFreeMargin();
   
   double percentage = account*RiskPercentage/100;
   
   double lots = MathRound(percentage/margin/step)*step;
   
   if(lots < minLot)
   {
      lots = minLot;
   }
   
   if(lots > maxLot)
   {
      lots = maxLot;
   }

   return (lots);
}

bool NeedToRetry(int errorCode)
{
   switch (errorCode)
   {
      case 1 : return (true);
      case 129 : return (true);
      case 135 : return (true);
      case 136 : return (true);
      case 138: return (true);
      
      default: return (false);
   }
}

int GetStopLevel()
{
   return (MathMax(MathRound(MarketInfo(Symbol(), MODE_STOPLEVEL)), 3));
}