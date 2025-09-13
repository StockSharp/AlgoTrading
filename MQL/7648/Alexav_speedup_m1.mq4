//+------------------------------------------------------------------+
//|                                            Alexav_SpeedUp_M1.mq4 |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Alex Saveliev"
#property link      "asavelievca@yahoo.com"
//----
extern double Lots=1;
extern double TakeProfit=26;
extern double InitialStop=23;
extern double trailingStop=23;
extern double OpenCloseDiff=0.001;
extern double slippage    =5;
extern int magicEA        =119;
//----
int k=0,MMin=70;
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
   int totalOrders=OrdersTotal();
//----
   Comment("Lots = ",Lots);
   for(int cnt=0; cnt<totalOrders; cnt++) // scan all orders and positions...
     {
      OrderSelect(cnt, SELECT_BY_POS);         // the next line will check for ONLY market trades, not entry orders
      if(OrderSymbol()==Symbol() && OrderType()<=OP_SELL && OrderMagicNumber()==magicEA)
        {   // only look for this symbol,OP_BUY or OP_SELL and only orders from this EA      
         if(trailingStop > 0) // Check trailing stop
           {
            if(Bid-OrderOpenPrice() > trailingStop*Point)
              {
               if(OrderStopLoss() < Bid - trailingStop*Point)
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),Bid-trailingStop*Point,OrderTakeProfit(),0);
                 }
              }
            if(OrderOpenPrice() - Ask > trailingStop*Point)
              {
               if(OrderStopLoss() > Ask + trailingStop*Point)
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),Ask+trailingStop*Point,OrderTakeProfit(),0);
                 }
              }
           }
        }
     }
   if (Minute()==MMin+1) k=0;
   if (k<1)
     {
      if (iClose(Symbol(),0,1)-iOpen(Symbol(),0,1)>OpenCloseDiff)
        {
         OrderSend(Symbol(),OP_BUY,Lots,Ask,slippage,Ask-InitialStop*Point,Ask+TakeProfit*Point,NULL,magicEA,0);
         MMin=Minute();
         k=k+1;
        }
      if (iOpen(Symbol(),0,1)-iClose(Symbol(),0,1)>OpenCloseDiff)
        {
         OrderSend(Symbol(),OP_SELL,Lots,Bid,slippage,Bid+InitialStop*Point,Bid-TakeProfit*Point,NULL,magicEA,0);
         MMin=Minute();
         k=k+1;
        }
     }
  }
//+------------------------------------------------------------------+