#property strict
extern double D = 800;
extern double D2 = 2200;
extern double D3 = 1600;

extern bool BuyIsActive = False;
extern bool SellIsActive = False;

void OnTick()
{  
   double E = OrderOpenPrice();
   double Lots = (AccountBalance()/100)/100;
   double avg0 = iMA(_Symbol,_Period,200,0,MODE_SMA,PRICE_CLOSE,0);
   double avg1 = iMA(_Symbol,_Period,50,0,MODE_SMA,PRICE_CLOSE,0);
   double avg2 = iMA(_Symbol,_Period,5,0,MODE_SMA,PRICE_CLOSE,0);
      
   Comment(" Orders: ", OrdersTotal(),"    ", " Bid: ", Bid, "    ", " Equity: ", AccountEquity(), "    ", " R: ", avg2-avg0 , " R2: ",avg1-avg0);
   
   if(OrdersTotal() == 0 && avg0 - Bid > D3 *_Point){int b0 = OrderSend(_Symbol,OP_BUY,Lots,Ask,0,0,0,0,0,0,0);BuyIsActive = True;}
   if(OrdersTotal() == 1 && avg0 - Bid > D3*_Point && BuyIsActive == True){int b1 = OrderSend(_Symbol,OP_BUY,Lots*1.4,Ask,0,0,0,0,0,0,0);}
//   if(OrdersTotal() == 2 && Bid < P - D3*_Point && BuyIsActive == True){int b2 = OrderSend(_Symbol,OP_BUY,Lots*2.8,Ask,0,0,0,0,0,0,0);}   
//   if(OrdersTotal() == 3 && Bid < P - D3*_Point && BuyIsActive == True){int b3 = OrderSend(_Symbol,OP_BUY,Lots*4.2,Ask,0,0,0,0,0,0,0);}

   if(OrdersTotal() == 0 && Bid - avg0 > D *_Point){int s0 = OrderSend(_Symbol,OP_SELL,Lots/5,Bid,0,0,0,0,0,0,0);SellIsActive = True;}
   if(OrdersTotal() == 1 && Bid > (avg2-avg0)+D2*_Point && SellIsActive == True){int s1 = OrderSend(_Symbol,OP_SELL,Lots/5,Bid,0,0,0,0,0,0,0);}
   if(OrdersTotal() == 2 && avg2 - avg0 >= 0.0056 && avg1-avg0 >= 0.0033 && SellIsActive == True){int s2 = OrderSend(_Symbol,OP_SELL,Lots*3,Bid,0,0,0,0,0,0,0);}   
   if(OrdersTotal() == 0 && Bid - Low[0] > 800*_Point){int s3 = OrderSend(_Symbol,OP_SELL,Lots*4,Bid,0,0,0,0,0,0,0);}

           
   if(AccountEquity() > AccountBalance() * 1.3){CloseTrades();BuyIsActive = False;SellIsActive = False;}   
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
