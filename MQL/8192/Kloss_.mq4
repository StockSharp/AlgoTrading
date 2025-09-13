//+------------------------------------------------------------------+
//|                                                       Kloss_.mq4 |
//|                                       Copyright © 2007, Tinytjan |
//|                                                 tinytjan@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, Tinytjan"
#property link      "tinytjan@mail.ru"

#define STUPID 0x60BE57

extern string     Lots_Desc         =  "if 0 dynamic lot is applied";
extern double     Lots              =  0.1;

extern string     RiskPercentage_Desc =  "actual only of Lots == 0, percentage of the risk for opening order";
extern int        RiskPercentage    =  10;

extern int        Slippage          =  1;

extern string     Target_Desc       =  "Take Profit -- none if 0";
extern int        Target            =  0;

extern string     Loss_Desc         =  "Stop Loss -- none if 0";
extern int        Loss              =  0;

extern int        MA                =  5;

extern int        CCI               =  10;

extern int        StochasticK       =  5;

extern int        StochasticD       =  3;

extern int        StochasticS       =  3;

extern string     CCIDiffer_Desc    =  "CCIDiffer and -CCIDiffer are signal levels";
extern int        CCIDiffer         =  120;

extern string     StochDiffer_Desc  =  "50 + StochDiffer and 50 - StochDiffer are signal levels";
extern int        StochDiffer       =  25;

extern string     MaxOrders_Desc    =  "no limit if 0";
extern int        MaxOrders         =  3;

double LotsToBid;
string symbol;

void CloseBuys(int MagicNumber, int Slippage)
{
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == OP_BUY)
      {
         if(OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, Blue))
         {
            i--;
         }
         RefreshRates();
      }
   }
}

void CloseSells(int MagicNumber, int Slippage)
{
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == OP_SELL)
      {
         if (OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, Red))
         {
            i--;
         }
         RefreshRates();
      }
   }
}

int GetOrdersCount(int MagicNumber, int Type)
{
   int count = 0;
   
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == Type)
      {
         count++;
      }
   }
   
   return (count);
}

double GetLotsToBid(int RiskPercentage)
{
   double margin = MarketInfo(Symbol(), MODE_MARGINREQUIRED);
   double minLot = MarketInfo(Symbol(), MODE_MINLOT);
   double maxLot = MarketInfo(Symbol(), MODE_MAXLOT);
   double account = AccountFreeMargin();
   
   double percentage = account*RiskPercentage/100;
   
   double lots = MathRound(10*percentage/margin)/10;
   
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

void OpenBuy()
{
   double TP = 0;
   if (Target > 0)
   {
      TP = Ask + Target*Point;
   }

   double SL = 0;
   if (Loss > 0)
   {
      SL = Ask - Loss*Point;
   }
   
   if (Lots == 0) LotsToBid = GetLotsToBid(RiskPercentage);
   
   OrderSend(Symbol(), OP_BUY, LotsToBid, Ask, Slippage, SL, TP, NULL, STUPID, 0, Blue);
}

void OpenSell()
{
   double TP = 0;
   if (Target > 0)
   {
      TP = Bid - Target*Point;
   }

   double SL = 0;
   if (Loss > 0)
   {
      SL = Bid + Loss*Point;
   }
   
   if (Lots == 0) LotsToBid = GetLotsToBid(RiskPercentage);
   
   OrderSend(Symbol(), OP_SELL, LotsToBid, Bid, Slippage, SL, TP, NULL, STUPID, 0, Red);
}

void Check()
{
   double ma = iMA(symbol, 0, MA, 0, MODE_EMA, PRICE_WEIGHTED, 1);
   double cci = iCCI(symbol, 0, CCI, PRICE_WEIGHTED, 1);
   double stoch = iStochastic(symbol, 0, StochasticK, StochasticD, StochasticS, MODE_EMA, 0, MODE_MAIN, 1);
   
   if (cci < -CCIDiffer && stoch < 50 - StochDiffer && Close[1] > ma)
   {
      CloseSells(STUPID, Slippage);
      if (GetOrdersCount(STUPID, OP_BUY) < MaxOrders || MaxOrders == 0) OpenBuy();
   }

   if (cci > CCIDiffer && stoch > 50 + StochDiffer && Close[1] < ma)
   {
      CloseBuys(STUPID, Slippage);
      if (GetOrdersCount(STUPID, OP_SELL) < MaxOrders || MaxOrders == 0) OpenSell();
   }
}

int init()
{
   LotsToBid = Lots;
   symbol = Symbol();
}

int start()
{

   // Trading only when tick is opening
   if (Volume[0] == 1)
   {
      // Check for open new orders and close current ones
      Check();
   }

   return(0);
}oBid = Lots;
   symbol = Symbol();
}

datetime LastTime = 0;

int start()
{
   if (LastTime == 0)
   {
      LastTime = TimeCurrent();
      return (0);
   }
   
   // Trading only when tick is opening
   //if (TimeCurrent() - LastTime > 20)
   {
      // Check for open new orders and close current ones
      Check();
      LastTime = TimeCurrent();
   }

   return(0);
}