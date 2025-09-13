//+------------------------------------------------------------------+
//|                                                     DoubleUp.mq4 |
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
double lot = 0.01;
int pos = 0;
bool buy = false;
bool sell = false;
extern int period= 8;
double buyLevel = 230;
extern int fast = 13;
extern int slow = 33;
extern double wait = 0;
extern double preWait=2 ;
extern int back = 0;
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
   ma = iCCI( Symbol(), 0,period, PRICE_CLOSE, back);
   ma2 = iMACD(NULL,0,fast,slow,2,PRICE_CLOSE,MODE_MAIN,back)*1000000;
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
   {
      sell = true;
      buy = false;
   }
   else if(ma<-buyLevel && ma2<-buyLevel)
   {
         buy = true;
         sell = false;
   }
   if(buy && ma< buyLevel)
   {
      buy();
      buy = false;  
   }
   if(sell && ma<-buyLevel)
   {
      sell();
      sell = false;
   }
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
       
        if(op == OP_BUY)
        {
               if(OrderProfit()<0)
               {
                  pos++;
                  if(pos >= preWait)
                  {
                  wait += pos;
                  pos = 0;
                  }
               }
               else if(OrderProfit()>0)
               {
                  pos = wait;///2;
                  wait = 0;//wait/2;
               
               }
            OrderClose(OrderTicket(),OrderLots(),Bid,3,Green); // or OrderClosePrice()
        }
        else
        {
            if(OrderProfit()<0)
               {
                  pos++;
                  if(pos >= preWait)
                  {
                  wait += pos;
                  pos = 0;
                  }
               }
               else if(OrderProfit()>0)
               {
                  pos = wait;///2;
                  wait = 0;//wait/2;
               
               }
            OrderClose(OrderTicket(),OrderLots(),Ask,3,Green);// or OrderClosePrice()
        }
      }
   }
}