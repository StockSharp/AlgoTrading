//+------------------------------------------------------------------+
//|                                                      Starter.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
#define MAGICMA  20050610

extern double Lots           = 1.2;
extern double MaximumRisk    = 0.036;
extern double DecreaseFactor = 2;
extern double Stop = 10;
extern double MAPeriod = 5;
double spread = 1.5;
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
   double Laguerre;
   double Alpha;
   double MA, MAprevious;
   int cnt, ticket, total;
   Laguerre = iCustom(NULL, 0, "Laguerre", 0, 0);
   Alpha = iCCI(NULL, 0, 14, PRICE_CLOSE, 0);
   MA = iMA(NULL,0,MAPeriod, 0, MODE_EMA, PRICE_MEDIAN, 0);
   MAprevious = iMA(NULL, 0, MAPeriod, 0, MODE_EMA, PRICE_MEDIAN, 1);
//----
   total = OrdersTotal();
   if(total < 1) 
     {
       // no opened orders identified
       if(AccountFreeMargin() < (1000*Lots))
         {
           Print("We have no money. Free Margin = ", AccountFreeMargin());
           return(0);  
         }
       // check for long position (BUY) possibility
       if((Laguerre == 0) && (MA > MAprevious) && (Alpha < -5)) //+-- && Juice>JuiceLevel)
         {
           ticket = OrderSend(Symbol(), OP_BUY, LotsOptimized(), Ask, 3, 0, 
                              0, "starter", 16384, 0, Green);
         }
       // check for short position (SELL) possibility
       if((Laguerre == 1) && (MA < MAprevious) && (Alpha > 5)) //+-- && Juice>JuiceLevel)
         {
           ticket = OrderSend(Symbol(), OP_SELL, LotsOptimized(), Bid, 3, 0, 
                              0, "starter", 16384, 0, Red);
         } 
     }
// it is important to enter the market correctly, 
// but it is more important to exit it correctly...   
   for(cnt = 0; cnt < total; cnt++)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderType() <= OP_SELL &&   // check for opened position 
          OrderSymbol() == Symbol())  // check for symbol
         {
           if(OrderType() == OP_BUY)   // long position is opened
             {
              // should it be closed?
              if(Laguerre > 0.9)
                {
                  OrderClose(OrderTicket(), OrderLots(), Bid, 3, Violet); // close position
                  return(0); // exit
                }
              // check for stop
              if(Stop > 0)  
                {                 
                  if(Bid - OrderOpenPrice() > Point*Stop)
                    {
                      OrderClose(OrderTicket(), OrderLots(), Bid, 3, Violet); // close position
                      return(0);
                    }
                }
             }
           else // go to short position
             {
               // should it be closed?
               if(Laguerre < 0.1)
                 {
                   OrderClose(OrderTicket(), OrderLots(), Ask, 3, Violet); // close position
                   return(0); // exit
                 }
               // check for stop
               if(Stop > 0)  
                 {                 
                   if(OrderOpenPrice() - Ask > Point*Stop)
                     {
                       OrderClose(OrderTicket(), OrderLots(), Ask, 3, Violet); // close position
                       return(0);
                     }
                 }
             }
         }
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
    double lot = Lots;
    int    orders = HistoryTotal();     // history orders total
    int    losses = 0;                  // number of losses orders without a break
//---- select lot size
    lot = NormalizeDouble(AccountFreeMargin()*MaximumRisk / 500, 1);
//---- calcuulate number of losses orders without a break
    if(DecreaseFactor > 0)
      {
        for(int i = orders - 1; i >= 0; i--)
          {
            if(OrderSelect(i, SELECT_BY_POS, MODE_HISTORY) == false) 
              { 
                Print("Error in history!"); 
                break; 
              }
            //----
            if(OrderSymbol() != Symbol() || OrderType() > OP_SELL) 
                continue;
            //----
            if(OrderProfit() > 0) 
                break;
            //----
            if(OrderProfit() < 0) 
                losses++;
          }
        if(losses > 1) 
            lot = NormalizeDouble(lot - lot*losses / DecreaseFactor,1);
      }
//---- return lot size
    if(lot < 0.1) 
        lot = 0.1;
    return(lot);
  }    
//+------------------------------------------------------------------+
// the end.

