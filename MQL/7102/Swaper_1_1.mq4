//+------------------------------------------------------------------+
//|                                                   Swaper_1.1.mq4 |
//|                               Copyright © 2007, Yury V. Reshetov |
//|                                          http://reshetov.xnet.uz |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, Yury V. Reshetov"
#property link      "http://reshetov.xnet.uz"
//---- input parameters
extern double experts = 1;
extern double beginPrice = 1.8014;
extern int    magicnumber = 777;
static int    prevtime = 0;
static double bl = 1000;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   if(Time[0] == prevtime) 
       return(0);
   prevtime = Time[0];
   if(!IsTradeAllowed()) 
     {
       prevtime = Time[1];
       return(0);
     }
//----
   int total = OrdersHistoryTotal();
   double money = bl * beginPrice;
   int i = 0;
   for (i = 0; i < total; i++) 
     {
       OrderSelect(i, SELECT_BY_POS, MODE_HISTORY);
       if(OrderSymbol() == Symbol() && OrderMagicNumber() == magicnumber) 
         {
           if(OrderType() == OP_BUY) 
             {
               money = money + (OrderClosePrice() - 
                       OrderOpenPrice()) * 10 * OrderLots();
             } 
           else 
             {
               money = money - (OrderClosePrice() - 
                       OrderOpenPrice()) * 10 * OrderLots();
             }
         }
     }
   total = OrdersTotal();   
   double com =  bl;
   int tickbuy = -1;
   double buyvolume = 0;
   int ticksell = -1;
   double sellvolume = 0;
   for(i = 0; i < total; i++) 
     {
       OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() == magicnumber) 
         {
           if(OrderType() == OP_BUY) 
             {
               if(OrderSymbol() == Symbol()) 
                 {
                   money = money - 10 * buyvolume * OrderOpenPrice();
                   if ((MarketInfo(Symbol(), MODE_SWAPLONG) < 0) || (AccountFreeMargin() < 0 && OrderProfit() < 0)) {
                     buyvolume = OrderLots();
                     tickbuy = OrderTicket();
                   } 
                 }
               com = com + 10 * OrderLots(); 
             } 
           else 
             {
               if(OrderSymbol() == Symbol()) 
                 {
                   money = money + 10 * sellvolume * OrderOpenPrice();
                   if ((MarketInfo(Symbol(), MODE_SWAPSHORT) < 0) || (AccountFreeMargin() < 0 && OrderProfit() < 0)) {
                     sellvolume = OrderLots();
                     ticksell = OrderTicket();
                   }
                 }
               com = com - 10 * OrderLots(); 
             }
         }
     }
      
   double fv = money / com;
   Comment("Fair value ", fv);
   
   if(! IsTradeAllowed()) 
     {
       prevtime = Time[1];
       return(0);
     }
   closeby(ticksell, tickbuy);
   if(!IsTradeAllowed()) 
     {
       prevtime = Time[1];
       return(0);
     }
   double lots = 0;
   double price = 0;
   double dt = (money / (MathMax(High[1], High[0]) + MarketInfo(Symbol(), MODE_SPREAD) * Point) - com) * experts / (experts + 1);
   if(dt < 0) 
     {
       dt = (com - money / MathMin(Low[1], Low[0])) * experts / (experts + 1);
       if(dt < 1) 
         {
           closeby(tickbuy, ticksell);
           return(0);
         }
       lots = MathFloor(dt) / 10;
       if(tickbuy >= 0) 
         {
           if(buyvolume > lots) 
             {
               OrderClose(tickbuy, lots, Bid, 3, Blue);
               Sleep(30000);
             } 
           else 
             {
               OrderClose(tickbuy, buyvolume, Bid, 3, Blue);
               tickbuy = -1;
               Sleep(30000);
             }
         } 
       else 
         {
           lots = getLots(lots);
           if(lots > 0) 
             {
               ticksell = OrderSend(Symbol(), OP_SELL, lots, Bid, 3, 
                          0, 0, "Swaper", magicnumber, 0, Red);
               Sleep(30000);
             }
         }
     } 
   else 
     {
       if(dt < 1) 
         {
           closeby(ticksell, tickbuy);
           return(0);
         }
       lots = MathFloor(dt) / 10;
       if(ticksell >= 0) 
         {
           if(sellvolume > lots) 
             {
               OrderClose(ticksell, lots, Ask, 3, Red);
               Sleep(30000);
             } 
           else 
             {
               OrderClose(ticksell, sellvolume, Ask, 3, Red);
               ticksell = -1;
               Sleep(30000);
             }
         } 
       else 
         {
           lots = getLots(lots);
           if(lots > 0) 
             {
               tickbuy = OrderSend(Symbol(), OP_BUY, lots, Ask, 3, 0, 
                         0, "Swaper",  magicnumber, 0, Blue);
               Sleep(30000);
             }
         }
     }
//----
   if(!IsTradeAllowed()) 
     {
       prevtime = Time[1];
       return(0);
     }
   closeby(tickbuy, ticksell);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeby(int sell, int buy) 
  {
   if(sell >= 0 && buy >= 0) 
     {
       OrderCloseBy(buy, sell, Green);
       Sleep(30000);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double getLots(double lt) 
  {
   double marginrequired = MarketInfo(Symbol(), MODE_MARGINREQUIRED);
   double freemargin = AccountFreeMargin();
   if(freemargin > (marginrequired * lt)) 
     {
       return(lt);
     } 
   double result = freemargin / marginrequired;
   result = MathFloor(result * 10) / 10;
   return(result);
  }
//+------------------------------------------------------------------+

