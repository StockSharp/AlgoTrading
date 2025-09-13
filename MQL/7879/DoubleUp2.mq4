//+------------------------------------------------------------------+
//|                                                     DoubleUp2.mq4 |
//|                                                  The # one Lotfy |
//|                                             hmmlotfy@hotmail.com |
//+------------------------------------------------------------------+
#property copyright "The # one Lotfy"
#property link      "hmmlotfy@hotmail.com"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
double ma;
double ma2;
int buyTotal = 0;
int sellTotal = 0;
double lot = 0.1;
int pos = 0;
extern int period= 8;
double buyLevel = 230;
extern int fast = 13;
extern int slow = 33;
extern double wait = 0;
extern double preWait=2 ;
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
   lot = NormalizeDouble(AccountBalance()/50001.0,2);
   if(lot < 0.1)
      lot = 0.1;
   ma = iCCI( Symbol(), 0,period, PRICE_CLOSE, 0);
   ma2 = iMACD(NULL,0,fast,slow,2,PRICE_CLOSE,MODE_MAIN,0)*1000000;
   buyTotal = 0;
   sellTotal = 0;
   for(int cnt=OrdersTotal()-1;cnt>=0;cnt--)// close all orders
   {
      if (!OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)) continue;
      if (OrderSymbol() != Symbol()) continue;
      if (OrderMagicNumber() != 343) continue;
      if (OrderType() == OP_BUY)
         buyTotal++;
      else
         sellTotal++;
   }
   if(ma>buyLevel && ma2>buyLevel)
      sell();
   else if(ma<-buyLevel && ma2<-buyLevel)
      buy();
   close();
//----
   return(0);
  }

void buy()
{
   closeAll(OP_SELL);
   if(buyTotal == 0)
      OrderSend(Symbol(),OP_BUY, lot*MathPow(2,pos),
          Ask,2, 0/*Ask-(SL2)*Point*/,0/*Ask+SL3*Point*/, NULL, 343, 0, Blue);
}
void sell()
{
   closeAll(OP_BUY);
   if(sellTotal == 0)
      OrderSend(Symbol(),OP_SELL, lot*MathPow(2,pos),
         Bid,2,0/*Bid+(SL2)*Point*/,0/*Bid-SL3*Point*/, NULL, 343, 0, Red );
}
void closeAll(int op)
{
   for(int cnt=OrdersTotal()-1;cnt>=0;cnt--)// close all orders
   {
      if (!OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)) continue;
      if (OrderSymbol() != Symbol()) continue;
      if (OrderMagicNumber() != 343) continue;
      if (OrderType() == op )
      {
         if(OrderProfit()<0)
             pos++;
         else if(OrderProfit()>0)
             pos = 0;
         OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Green);// or OrderClosePrice()
      }
   }
}
void close()
{
   for(int cnt=OrdersTotal()-1;cnt>=0;cnt--)// close all orders
   {
      if (!OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)) continue;
      if (OrderSymbol() != Symbol()) continue;
      if (OrderMagicNumber() != 343) continue;
         if(OrderProfit()>0 && MathAbs(OrderOpenPrice()-OrderClosePrice())>120*Point)
         {
            pos+=2;
            OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Green);// or OrderClosePrice()
         }
   }
}

