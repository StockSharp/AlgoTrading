#property strict

void OnTick()
{
   double Lots = (AccountBalance()* 0.01) / 100;
   double avg0 = iMA(_Symbol,_Period,200,0,MODE_SMA,PRICE_CLOSE,0);
   double avg1 = iMA(_Symbol,_Period,50,0,MODE_SMA,PRICE_CLOSE,0);
   double avg2 = iMA(_Symbol,_Period,5,0,MODE_SMA,PRICE_CLOSE,0);
      
   Comment(" Orders: ", OrdersTotal() ,"    ", " Balance: ", AccountBalance(), "    ", " Equity: ", AccountEquity(), "    ", " R: ", avg2 - avg0 );
   

   if (OrdersTotal() == 0 && AccountBalance() == AccountEquity() && Bid - avg0 > 1400*_Point)
   { 
      int s0 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,0,0,0,clrIndigo);
   }
   
   if (OrdersTotal() == 1 && AccountEquity() < AccountBalance()-(AccountBalance() * 0.02))
   { 
      int s1 = OrderSend(_Symbol,OP_SELL,Lots*3,Bid,0,0,0,0,0,0,clrIndigo);
   }
      
   if (OrdersTotal() == 2 && AccountEquity() < AccountBalance()-(AccountBalance() * 0.03))
   { 
      int s2 = OrderSend(_Symbol,OP_SELL,Lots*4,Bid,0,0,0,0,0,0,clrIndigo);
   }
         
   if (OrdersTotal() == 3 && AccountEquity() < AccountBalance()-(AccountBalance() * 0.04))
   { 
      int s3 = OrderSend(_Symbol,OP_SELL,Lots*5,Bid,0,0,0,0,0,0,clrIndigo);
   }
         
   if (OrdersTotal() == 0 && Bid - Low[0] > 800 *_Point)
   { 
      int s4 = OrderSend(_Symbol,OP_SELL,Lots*20,Bid,0,0,0,0,0,0,clrIndigo);
   }     


   if(OrdersTotal() == 1 && AccountEquity() > AccountBalance() * 1.04)
   {
      CloseTrades();
   }
   
   if(OrdersTotal() == 2 && AccountEquity() > AccountBalance() * 1.1)
   {
      CloseTrades();
   }
   
   if(OrdersTotal() == 3 && AccountEquity() > AccountBalance() * 1.3)
   {
      CloseTrades();
   }
   
   if(OrdersTotal() == 4 && AccountEquity() > AccountBalance() * 1.6)
   {
      CloseTrades();
   }
      
   if(OrdersTotal() == 5 && AccountEquity() > AccountBalance() * 1.8)
   {
      CloseTrades();
   }   
}  

void CloseTrades()
{
   for(int i = OrdersTotal()-1; i >= 0 ; i--)
   {
      if(OrderSelect(i,SELECT_BY_POS)==True)
      {
         bool result = OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),0,clrRed);
      }
   }
}




//   if(Trend == 2 && AccountEquity() > AccountBalance()){Trend = 0;}      
      
      
      //for(int i = 0; i < OrdersTotal(); i++)
      //{
      //   if(OrderSelect(i, SELECT_BY_POS)==True)
      //   {
      //      bool result = OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),10,clrAquamarine);
      //   }
      //}


   //if (OrdersTotal() < MaxTradePositions && avg0 > Low[0] && High[0] - Low[0] > 6000*_Point && Trend == 1)
   //{
   //   int b0 = OrderSend(_Symbol,OP_BUY,TradeSize,Ask,Slippage,0,avg0,0,0,0,clrIndigo);
   //}
   
   //if (OrdersTotal() < MaxTradePositions && Close[1] < Open[1] && Open[1]-Close[1] < 20 *_Point && Close[1] < avg0 && Trend == 0)
   //{
   //   int s0 = OrderSend(_Symbol,OP_SELL,TradeSize,Bid,Slippage,0,avg0-10000*_Point,0,0,0,clrIndigo);
   //}
   
   
   
   
//   
//   if (OrdersTotal() == 0 && Bid < Open[0] && Trend == 0)
//   {
//      int s0 = OrderSend(_Symbol,OP_SELL,TradeSize,Bid,Slippage,0,0,0,0,0,clrIndigo);
//   }
//   
//   if (OrdersTotal() == 0 && Trend == 1)
//   {
//      int b0 = OrderSend(_Symbol,OP_BUY,TradeSize,Ask,Slippage,0,0,0,0,0,clrIndigo);
//   }   
//   
//   
//   if(avg0 - Bid > 10000*_Point && Trend == 0)
//   {
//      Trend = 1;
//      CloseAllTrades();
//   }
//   
//   if(Ask > avg0 && Trend == 1)
//   {
//      CloseAllTrades();
//      Trend = 0;
//   }       
//   
   


//   if(High[0] < High[1] && High[1] < High[2] && High[2] < High[3] && High[3] < High[4] && High[4] < High[5] && Bid < Low[1]){Trend = 1; TP = 400;}
//   if(High[1] - Low[30] > 900 *_Point){Trend = 0;}
//   if(Low[1] - Low[30] > 3000 *_Point){Trend = 1; TP = 3000;}   
//   
//   if(AccountEquity() > AccountBalance() - (AccountBalance() * 0.30)){ Trend = 0;}
//   if(AccountEquity() < AccountBalance() - (AccountBalance() * 0.20)){ Trend = 2;}
      
   //if(avg2 > avg1 && avg1 > avg0 && avg2 - avg0 > Range *_Point){Trend = 1;}
