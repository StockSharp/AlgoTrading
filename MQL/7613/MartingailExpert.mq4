//+------------------------------------------------------------------+
//|                                                          aaa.mq4 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
extern double step=25;
extern double proffactor=9;
extern double mult=1.6;
extern double lots=0.3;  
extern double per_K=20;
extern double per_D=6;
extern double slow=6;
extern double zoneBUY=50;
extern double zoneSELL=50;
double openprice,ask,n,lots2,tp,total,cnt,sm,rtpbay,rtpsell,free,balance;
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
if (Ask>=openprice+tp*Point)
for(cnt = OrdersTotal(); cnt >= 0; cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderSymbol() == Symbol())
         {
           if(OrderType() == OP_BUY)
             {
              OrderClose(OrderTicket(), OrderLots(), Ask, 3, Yellow);lots2=lots;
             }
         }
     }
if (Bid<=openprice-tp*Point)
for(cnt = OrdersTotal(); cnt >= 0; cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderSymbol() == Symbol())
         {
           if(OrderType() == OP_SELL)
             {
              OrderClose(OrderTicket(), OrderLots(), Bid, 3, Yellow);lots2=lots;
             }
         }
     }
free=AccountFreeMargin();balance=AccountBalance();
if (AccountFreeMargin()<=AccountBalance()/2)return(0);  
total=OrdersTotal();
    if(total<1)
    {  
    if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,0,1)>iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)
      && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)>zoneBUY)             
        n=OrderSend(Symbol(),OP_BUY,lots,Ask,3,0,0,"",0,0,Green);       
    if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,0,1)<iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)
      && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)<zoneSELL)      
        n=OrderSend(Symbol(),OP_SELL,lots,Bid,3,0,0,"",0,0,Red);
    }
total=OrdersTotal();
OrderSelect(n,SELECT_BY_TICKET, MODE_TRADES);
openprice=OrderOpenPrice();
  if(total>0)
  {
   if(OrderType() == OP_BUY)
     {
      if (Ask>=openprice+tp*Point)lots2=lots;
      if (Ask>=openprice+tp*Point)n=OrderSend(Symbol(),OP_BUY,lots2,Ask,3,0,0,"",0,0,Blue);
      if (Ask<=openprice-step*Point)lots2=lots2*mult;
      if (Ask<=openprice-step*Point)n=OrderSend(Symbol(),OP_BUY,NormalizeDouble(lots2,1),Ask,3,0,0,"",0,0,Blue);
     }
   if(OrderType() == OP_SELL)
     {
      if (Bid<=openprice-tp*Point)lots2=lots;
      if (Bid<=openprice-tp*Point)n=OrderSend(Symbol(),OP_SELL,lots2,Bid,3,0,0,"",0,0,Red);
      if (Bid>=openprice+step*Point)lots2=lots2*mult;
      if (Bid>=openprice+step*Point)n=OrderSend(Symbol(),OP_SELL,NormalizeDouble(lots2,1),Bid,3,0,0,"",0,0,Red);
     }
  }
total=OrdersTotal();
OrderSelect(n,SELECT_BY_TICKET, MODE_TRADES);
openprice=OrderOpenPrice();
if (total>0) tp=total*proffactor;
rtpbay=openprice+tp*Point;rtpsell=openprice-tp*Point;
{
   double sm;
   total = OrdersTotal();
   for(cnt = 0; cnt < total; cnt++)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);     
         {                
          sm = sm + OrderLots();  
         } 
     }   
   Comment("Total = ",total,"  Lot = ",sm,"  TakeProfitSell = ",rtpsell,"  TakeProfitBay = ",rtpbay,
   "  FreeMargin = ",free,"  Balance = ",balance);
}

//----
   return(0);
  }
//+------------------------------------------------------------------+