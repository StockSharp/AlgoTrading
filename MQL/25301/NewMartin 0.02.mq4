#property strict

extern double Dev = 400;

void OnTick()
{
   double Lots = (AccountBalance()/100)/100;
   double avg0 = iMA(_Symbol,_Period,200,0,MODE_SMA,PRICE_CLOSE,0);
   double avg1 = iMA(_Symbol,_Period,50,0,MODE_SMA,PRICE_CLOSE,0);
   double avg2 = iMA(_Symbol,_Period,5,0,MODE_SMA,PRICE_CLOSE,0);
      
   Comment(" Orders: ", OrdersTotal(),"    ", " Bid: ", Bid, "    ", " Equity: ", AccountEquity(), "    ", " R: ", avg2 - avg0 );
   
   if(OrdersTotal() == 0 && avg0 - Bid > Dev*1 *_Point){int b0 = OrderSend(_Symbol,OP_BUY,Lots*1.0,Ask,0,0,avg0,0,0,0,clrIndigo);}
   if(OrdersTotal() == 1 && avg0 - Bid > Dev*2 *_Point){int b1 = OrderSend(_Symbol,OP_BUY,Lots*1.4,Ask,0,0,avg0,0,0,0,clrIndigo);}
   if(OrdersTotal() == 2 && avg0 - Bid > Dev*3 *_Point){int b2 = OrderSend(_Symbol,OP_BUY,Lots*2.8,Ask,0,0,avg0,0,0,0,clrIndigo);}   
   if(OrdersTotal() == 3 && avg0 - Bid > Dev*4 *_Point){int b3 = OrderSend(_Symbol,OP_BUY,Lots*4.2,Ask,0,0,avg0,0,0,0,clrIndigo);}

   if(OrdersTotal() == 0 && Bid - avg0 > Dev*1 *_Point){int s0 = OrderSend(_Symbol,OP_SELL,Lots*1.0,Bid,0,0,avg0,0,0,0,clrIndigo);}
   if(OrdersTotal() == 1 && Bid - avg0 > Dev*2 *_Point){int s1 = OrderSend(_Symbol,OP_SELL,Lots*1.4,Bid,0,0,avg0,0,0,0,clrIndigo);}
   if(OrdersTotal() == 2 && Bid - avg0 > Dev*3 *_Point){int s2 = OrderSend(_Symbol,OP_SELL,Lots*2.8,Bid,0,0,avg0,0,0,0,clrIndigo);}   
   if(OrdersTotal() == 3 && Bid - avg0 > Dev*4 *_Point){int s3 = OrderSend(_Symbol,OP_SELL,Lots*4.2,Bid,0,0,avg0,0,0,0,clrIndigo);}

           
   if(AccountEquity() > AccountBalance() * 1.7){CloseTrades();}   
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
