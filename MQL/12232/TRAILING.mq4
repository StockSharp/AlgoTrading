
#property copyright "Copyright © 2020, Adam Malik Kasang"
#property link      "adamakasang2020@gmail.com"


extern int    TakeProfit       = 100;        
extern int    StopLoss         = 20;
extern int    Trailing     = 3;           


int init()
  {
//----
  
 
//----
   return(0);
  }

int deinit()
  {
//----
   
//----
   return(0);
  }

int start()
  {
  
  
 Comment("     " ,AccountName(), "              ACCOUNT  ",AccountNumber(),"            PROFIT  ",AccountProfit(),  "           FREE MARGIN  ",AccountFreeMargin(),  "             TOTAL  ",OrdersTotal(), "          EQUITY  ",AccountEquity(), "            BALANCE  ",AccountBalance());
  


  
int Magic=0;
  
for(int cnt=0;cnt<OrdersTotal();cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if( OrderSymbol()==Symbol() && OrderMagicNumber()==Magic)
        
        {
         if(OrderType()==OP_BUY)
           {
            if(Bid-OrderOpenPrice()>=TakeProfit*Point|| OrderOpenPrice()-Ask>StopLoss*Point)
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,0,Violet); return(0);
              }
            if(Trailing>0)
              {
               if(Bid-OrderOpenPrice()>Point*Trailing)
                 {
                  if(OrderStopLoss()<Bid-Point*Trailing)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*Trailing,OrderTakeProfit(),0,Green); return(0);
                    }
                 }
              }
           }
         if(OrderType()==OP_SELL)
           {
            if(OrderOpenPrice()-Ask>=TakeProfit*Point|| Bid-OrderOpenPrice()>StopLoss*Point)
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,0,Violet); return(0);
              }
            if(Trailing>0)
              {
               if((OrderOpenPrice()-Ask)>(Point*Trailing))
                 {
                  if((OrderStopLoss()>(Ask+Point*Trailing)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*Trailing,OrderTakeProfit(),0,Red); return(0);
                    }
                 }
              }
           }
         }
       }


  return(0);
}