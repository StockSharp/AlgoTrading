//+------------------------------------------------------------------+
//|                                                         e-CA.mq4 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+

extern double  TakeProfit  = 60;
extern int     StopLoss    = 40;
extern double  Lots        = 0.1;
extern int     Trailing    = 0;
extern int     Step        = 0;
extern int MA.Period = 35;
extern int MA.method = 0;//MODE_SMA
extern int sigma_b=5;
extern int sigma_s=5;
int br=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int cnt, ticket, total;
   
   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);  
     }
//   if(TakeProfit<10)
//     {
//      Print("TakeProfit less than 10");
//      return(0);  // check TakeProfit
//     }
     
//double zz=iCustom(NULL,NextTF,"HiLo_Act_Next_Profi2",Range,0,1);
double zz1=iCustom(NULL,0,"i-CA",MA.Period,MA.method,0,0);
     
   total=OrdersTotal();
   if(total<1) 
     {
      // no opened orders identified
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);  
        }
      // check for long position (BUY) possibility
      if((Ask>=(zz1+sigma_b*Point))&&(Close[1]<zz1)&& br<Bars)//||(Close[3]<zz1))
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"0",16384,0,Green);
         br=Bars;
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 
        }
      // check for short position (SELL) possibility
      if((Bid<=(zz1-sigma_s*Point))&&(Close[1]>zz1)&& br<Bars)//||(Close[3]>zz1))
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+StopLoss*Point,Bid-TakeProfit*Point,"0",16384,0,Red);
         br=Bars;
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
           }
         else Print("Error opening SELL order : ",GetLastError()); 
         return(0); 
        }
      return(0);
     }
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL &&   // check for opened position 
         OrderSymbol()==Symbol())  // check for symbol
        {
         if(OrderType()==OP_BUY)   // long position is opened
           {
         if(Trailing>0)  
              {                 
               if(Bid-OrderOpenPrice()>Point*Trailing)
                 {
                  if((OrderStopLoss()<Bid-Point*Trailing && (Bid-Point*Trailing)-OrderStopLoss()>Step*Point) || OrderStopLoss()==0)
                    {
                     if(Bid-Point*Trailing!=OrderStopLoss()) OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*Trailing,OrderTakeProfit(),0);
                     return(0);
                    }
                 }
              }
           }
         else
           {
            if(Trailing>0)  
              {                 
               if((OrderOpenPrice()-Ask)>(Point*Trailing))
                 {
                  if((OrderStopLoss()>(Ask+Point*Trailing) && (OrderStopLoss()-(Ask+Point*Trailing)>Step*Point)) || OrderStopLoss()==0)
                    {
                     if(Ask+Point*Trailing!=OrderStopLoss()) OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*Trailing,OrderTakeProfit(),0);
                     return(0);
                    }
                 }
              }
           }
        }
     }
}


