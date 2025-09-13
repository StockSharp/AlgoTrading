//+------------------------------------------------------------------+
//|                                                Expert Master.mq4 |
//|                                                          Gabito. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Gabito."
#property link      "http://www.metaquotes.net"
//-----------------------------------+
#define MagicNumber 23478423 
//-----------------------------------+
extern int       Trailing=25;
extern double    Lots=10;
double lot;
extern double    Risk=0.01;
extern double DF     =0;
extern double SF     =0;
int Ticket;
int i;

//---------- indicators ------------+
double mac1, mac2, mac3, mac4, mac5, mac6, mac7, mac8;

//-----------------------------------+
void OpBuySell()
{
mac1 = iMACD(NULL,0,5,15,3,PRICE_CLOSE,MODE_MAIN,0);
mac2 = iMACD(NULL,0,5,15,3,PRICE_CLOSE,MODE_MAIN,1);
mac3 = iMACD(NULL,0,5,15,3,PRICE_CLOSE,MODE_MAIN,2);
mac4 = iMACD(NULL,0,5,15,3,PRICE_CLOSE,MODE_MAIN,3);
mac5 = iMACD(NULL,0,5,15,3,PRICE_CLOSE,MODE_SIGNAL,0);
mac6 = iMACD(NULL,0,5,15,3,PRICE_CLOSE,MODE_SIGNAL,1);
mac7 = iMACD(NULL,0,5,15,3,PRICE_CLOSE,MODE_SIGNAL,2);
mac8 = iMACD(NULL,0,5,15,3,PRICE_CLOSE,MODE_SIGNAL,3);

       // check for short position (SELL) possibility
if(mac8>mac7 && mac7>mac6 && mac6<mac5 && mac4>mac3 && mac3<mac2 && mac2<mac1 && mac2<-0.00020 && mac4<0 && mac1>0.00020)
        {
         Ticket=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Bid,1,0,0,"expert master",MagicNumber,0,Green);
         return(0); 
        }
      // check for short position (SELL) possibility
if(mac8<mac7 && mac7<mac6 && mac6>mac5 && mac4<mac3 && mac3>mac2 && mac2>mac1 && mac2>0.00020 && mac4>0 && mac1<-0.00035)
        {
         Ticket=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Ask,1,0,0,"expert master",MagicNumber,0,Red);
         return(0); 
        }
      
     }

//-----------------------------------+
void CloseOpBuySell()
{
if(OrderType()==OP_BUY)   // long position is opened
{
if(mac1<mac2)
{
OrderClose(OrderTicket(),OrderLots(),Bid,1,White);
return(0);
}
if(Trailing>0)
 {
  if(Bid-OrderOpenPrice()>Point*Trailing)
  {
   if(OrderStopLoss()<Bid-Point*Trailing)
   {
    OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*Trailing, OrderTakeProfit(),0,Blue);
    return(0);
   }
  }
 }
 
 }
 
if(OrderType()==OP_SELL)   // short position is opened
{           
if(mac1>mac2)
{
OrderClose(OrderTicket(),OrderLots(),Ask,1,Black);
return(0);
}
if(Trailing>0)
 {
  if((OrderOpenPrice()-Ask)>(Point*Trailing))
                 {
                  if((OrderStopLoss()>(Ask+Point*Trailing)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*Trailing,OrderTakeProfit(),0,Red);
                     return(0);
   }
  }
 }
 
 }
 
}
//-----------------------------------+
//-----------------------------------+
double LotsOptimized()
  {
   lot=Lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   lot=NormalizeDouble(AccountFreeMargin()*Risk/100,1);
//---- calculate number of losses orders without a break
   if(2>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses--;
        }
      if(losses>1) lot=NormalizeDouble(lot+lot*losses,1);
     }
//---- return lot size
    return(lot);
  }


//-----------------------------------+
int start()
{
int total=OrdersTotal();
total=OrdersTotal();
   if(total<1) 
     {
     
     OpBuySell();
     return(0);
     }
        for(int cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()==Symbol())  // check for symbol
        {
               
            CloseOpBuySell();
        }
      }

}
//-----------------------------------+

