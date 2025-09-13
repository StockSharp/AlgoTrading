//+------------------------------------------------------------------+
//|                                                      Stairs.mq4 |
//|                                       Copyright © 2008, Tinytjan |
//|                                                 tinytjan@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Tinytjan"
#property link      "tinytjan@mail.ru"

#include "libraries/trade.mq4"

extern int        Channel           =  1000;

extern int        Profit            =  1500;
extern int        CommonProfit      =  1000;

extern int        AddLots           =  1;

string symbol;
double Lots;
datetime LastTime;

string CloseAll = "StairsCloseAll";
string StartLots = "StartLots";

bool IsStart()
{
   double buyStopPrice = 0;
   double sellStopPrice = 0;

   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != Magic) continue;
      
      if(OrderType() == OP_SELLSTOP)
      {
         sellStopPrice = OrderOpenPrice();
      }

      if(OrderType() == OP_BUYSTOP)
      {
         buyStopPrice = OrderOpenPrice();
      }
   }
   
   if (buyStopPrice == 0 || sellStopPrice == 0)
   {
      return (false);
   }
   
   double distance = (buyStopPrice - sellStopPrice)/Point;
   return (distance < 1.5*Channel && distance > 0.5*Channel);
}

int GetLastOpenedTicket(int MN)
{
   datetime lastTime = 0;
   int lastTicket = -1;
   
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MN) continue;
      
      if (OrderType() != OP_SELL && OrderType() != OP_BUY) continue;
      
      if (OrderOpenTime() > lastTime)
      {
         lastTime = OrderOpenTime();
         lastTicket = OrderTicket();
      }
   }
   
   return (lastTicket);
}

double GetLastLot(int MN)
{
   int ticket = GetLastOpenedTicket(MN);
   if (OrderSelect(ticket, SELECT_BY_TICKET))
   {
      return (OrderLots());
   }
   
   double lots = TradeLots;
   if (TradeLots == 0)
   {
      lots = GetLotsToBid(RiskPercentage);
   }
   
   return (lots);
}

double GetLastPrice(int MN)
{
   int ticket = GetLastOpenedTicket(MN);
   if (OrderSelect(ticket, SELECT_BY_TICKET))
   {
      return (OrderOpenPrice());
   }
   
   return(0);
}

void Check()
{
   string name = Symbol() + StartLots;

   if (!GlobalVariableCheck(CloseAll))
   {
      GlobalVariableSet(CloseAll, EMPTY_VALUE);
   }
   else
   {
      double value = GlobalVariableGet(CloseAll);
      
      if (value == 1)
      {
         if (GetOrdersCount(Magic) > 0)
         {
            CloseSells(Magic, Slippage);
            CloseBuys(Magic, Slippage);
            DeletePending(Magic);
         }
         RefreshRates();
         
         if (GetOrdersCount(Magic, -1, "all") == 0)
         {
            GlobalVariableSet(CloseAll, EMPTY_VALUE);
         }
         else
         {
            return;
         }
      }
   }
   
   int SL = Channel + MathRound((Ask - Bid)/Point);
   
   if (GetOrdersCount(Magic) == 0)
   {
      double lot = DoubleIf(TradeLots == 0, GetLotsToBid(RiskPercentage), TradeLots);
      
      GlobalVariableSet(name, lot);
      
      int distanse = Channel/2;
      
      OpenBuyStop(Magic, Ask + distanse*Point, 0, SL, lot);
      OpenSellStop(Magic, Ask - distanse*Point, 0, SL, lot);
   }
   
   if (!GetActiveOrders(Magic))
   {
      if (GetOrdersCount(Magic) > 0)
      {
         if (!IsStart())
         {
            // остались отложки
            // это состояние конечное
            // удаляем 
            DeletePending(Magic);
         }
      }
      return;
   }
   
   if (GetOrdersCount(Magic, OP_BUYSTOP) +  GetOrdersCount(Magic, OP_SELLSTOP) < 2 && GetActiveOrders(Magic))
   {
      // отложка сработала, надо выставить заново
      // или постначальное состояние, надо расставить сеть.
      
      DeletePending(Magic);

      double lastOpenPrice = GetLastPrice(Magic);
      double lastOpenLots = GetLastLot(Magic);
      double addLot = GlobalVariableGet(name);

      if (AddLots == 1)
      {
         lastOpenLots += addLot;
      }
      
      if (MathAbs(lastOpenPrice - Bid)/Point < Channel/2)
      {
         OpenBuyStop(Magic, lastOpenPrice + Channel*Point, 0, SL, lastOpenLots);
         OpenSellStop(Magic, lastOpenPrice - Channel*Point, 0, SL, lastOpenLots);
      }
   }
   
   double profit = 0;
   
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if (OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if (OrderSymbol() != symbol) continue;
      // order was opened in another way
      if (OrderMagicNumber() != Magic) continue;
      
      int type = OrderType();
      
      if (type != OP_SELL && type != OP_BUY) continue;
      
      double add = DoubleIf(type == OP_BUY, Bid - OrderOpenPrice(), OrderOpenPrice() - Ask);
      
      profit += add/Point;
   }

   if (profit > Profit)
   {
      CloseSells(Magic, Slippage);
      CloseBuys(Magic, Slippage);
   }

   profit = 0;
   
   for(i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if (OrderSelect(i, SELECT_BY_POS) == false) continue;
      // order was opened in another way
      if (OrderMagicNumber() != Magic) continue;
      
      type = OrderType();
      
      if (type != OP_SELL && type != OP_BUY) continue;
      
      add = OrderOpenPrice() - MarketInfo(OrderSymbol(), MODE_ASK);
      if (type == OP_BUY) add = MarketInfo(OrderSymbol(), MODE_BID) - OrderOpenPrice();
      
      profit += add/MarketInfo(OrderSymbol(), MODE_POINT);
   }

   if (profit > CommonProfit)
   {
      GlobalVariableSet(CloseAll, 1);
   }
}

int init()
{
   Lots = TradeLots;
   symbol = Symbol();
   LastTime = 0;
}

int start()
{
   Check();

   return(0);
}