//+------------------------------------------------------------------+
//|                                                STRADDLE NEWS.mq4 |
//|                                          Dany Andrés Benjumea G. |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Dany Andrés Benjumea G."

// EXPERT ADVISOR DESIGNED FOR PLATFORMS WITH 5 Digits

extern double     StopLoss          = 100;   // 10 pips
extern double     TakeProfit        = 300;   // 30 pips
extern double     TrailingStop      = 50;    //  5 pips
extern double     PipsAway          = 50;    //  Send pending orders 5 pips away from the current Bid & Ask
extern double     BalanceUsed       = 0.01;  //  Higher Balance Used --> Higher Risk
extern double     SpreadOperation   = 25;    //  Spread allowed 2,5 pips. If spread is higher than allowed, it doesn't send any pending order 
extern double     Slippage          = 30;    //  Slippage allowed 3,0 pips.
extern int        Leverage          = 400;   //  Broker leverage
double            Lots;                      //  Calculates the number of lots according to the balance used and leverage
int               ticket1, ticket2;
int               t = 0;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
int Spread = MarketInfo(Symbol(),MODE_SPREAD);

if(t < 1)
{
if(Spread < SpreadOperation && OrdersTotal() < 1)
   {
   Lots =(AccountBalance()*Leverage*BalanceUsed)/(100000*Ask);
   ticket1 = OrderSend(Symbol(),OP_BUYSTOP,Lots,Ask + PipsAway * Point, Slippage, Ask - StopLoss * Point,Ask + TakeProfit * Point,"DANCA",800424,0,White);  
   t=t+1;
   ticket2 = OrderSend(Symbol(),OP_SELLSTOP,Lots,Bid - PipsAway * Point, Slippage, Bid + StopLoss * Point,Bid - TakeProfit * Point,"DANCA",240480,0,White);    
   t=t+1;
   }
} 
   

for (int i = 0; i < OrdersTotal(); i++) 
   {
   OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
   
      if (OrderType() == OP_BUY) 
         {
         OrderDelete(ticket2);
         if(Bid-OrderOpenPrice()>Point*TrailingStop)
            {
            if(OrderStopLoss()<Bid-Point*TrailingStop)
               {
               OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
               return(0);
               }
            }
          }

          if (OrderType() == OP_SELL) 
            {
               OrderDelete(ticket1);
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
               {
                  if(OrderStopLoss()>(Ask+Point*TrailingStop))
                  {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                  }
               }
            }
     }
      
   return(0);
   
  }


