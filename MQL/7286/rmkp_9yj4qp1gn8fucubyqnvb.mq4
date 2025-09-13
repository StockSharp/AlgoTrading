//+------------------------------------------------------------------+
//|                                                по стохастику.mq4 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
extern double Lots=0.1;
extern double TakeProfit=50;
extern double TrailingStop=20;
extern double MaxLots=7;
extern double pips=7;
extern double per_K=5;
extern double per_D=3;
extern double slow=3;
extern double zoneBUY=30;
extern double zoneSELL=70;


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
  double total,Cena,cnt,lot;
  double cenaoppos,l,sl;
  total=OrdersTotal();
    if(total<1)
    {  
    if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,0,1)>iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)
      && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)<zoneBUY)
      {
        sl=MaxLots*TrailingStop*Point+20*Point;
         OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-sl,Ask+TakeProfit*Point,0,Green);
         }
     if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,0,1)<iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)
      && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)>zoneSELL)
      {
         sl=MaxLots*TrailingStop*Point+20*Point;
         OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+sl,Bid-TakeProfit*Point,0,Red);
         }      
      }
    if(total>0 && total<MaxLots)      
      {
      for(cnt=0;cnt<total;cnt++)
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      cenaoppos=OrderOpenPrice();
      lot=OrderLots()*2;
      if(OrderType()<=OP_SELL &&   
         OrderSymbol()==Symbol())  
        {
         if(OrderType()==OP_BUY)   
           {
           Cena=Ask;
            if((cenaoppos-pips*Point)>Cena)
                {
                OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,Ask+TakeProfit*Point,0,Green);
                 return(0); 
                }
           }
         else 
           {
           Cena=Bid;
            if((cenaoppos+pips*Point)<Cena)
              {
              OrderSend(Symbol(),OP_SELL,lot,Bid,3,0,Bid-TakeProfit*Point,"macd sample",16384,0,Red);             
               return(0); 
              }
           }
        }
      }
      for(cnt=0;cnt<total;cnt++)
      {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()==OP_BUY)
        {  
         if(TrailingStop>0)  
           {                 
            if(Bid-OrderOpenPrice()>Point*TrailingStop)
              {
               if(OrderStopLoss()<Bid-Point*TrailingStop)
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,Ask+TakeProfit*Point,0,Green);
                  return(0);
                 }
              }
           }
         }
         
       else
            {
            if(TrailingStop>0)  
              {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,Bid-TakeProfit*Point,0,Red);
                     return(0);
                    }
                 }
              }
           }
           
//----------------------------------
}}