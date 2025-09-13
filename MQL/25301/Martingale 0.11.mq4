#property strict  /// --- Please remove this line and press Compile. If this line is removed it should compile without errors or warnings.

extern double LL = 7000;
extern double HH = 1300;
static bool BuyIsActive = False;
static bool SellIsActive = False;

void OnTick()
{
   double Lot = (AccountBalance()*0.002) / 100;
   double avg0 = iMA(_Symbol,_Period,200,0,MODE_SMA,PRICE_CLOSE,0);
   double avg1 = iMA(_Symbol,_Period,50,0,MODE_SMA,PRICE_CLOSE,0);
   double avg2 = iMA(_Symbol,_Period,5,0,MODE_SMA,PRICE_CLOSE,0);
   double rsi = iRSI(_Symbol,_Period,9,PRICE_CLOSE,0);
      
   Comment(" Orders: ", OrdersTotal() ,"    ", " Balance: ", AccountBalance(), "    ", " Equity: ", AccountEquity(), "    ", " R: ", avg0 - Bid );
   
   
   if (OrdersTotal() < 10 && avg0 - Bid > LL*_Point && rsi < 30)
   { 
      int b0 = OrderSend(_Symbol,OP_BUY,Lot,Ask,0,0,0,0,0,0,clrIndigo);
      BuyIsActive = True;
   }
   
   if (OrdersTotal() < 10 && Bid - avg0 > HH*_Point && rsi > 70)
   { 
      int s0 = OrderSend(_Symbol,OP_SELL,Lot,Bid,0,0,0,0,0,0,clrIndigo);
      SellIsActive = True;
   }
   
   if(rsi > 80 && OrdersTotal() < 20 && BuyIsActive == True && Bid - avg0 > 1300*_Point){CloseTrades();BuyIsActive = False;};   
   if(rsi < 20 && OrdersTotal() < 20 && SellIsActive == True && avg0 - Bid > 1300*_Point){CloseTrades();SellIsActive = False;}; 
  
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








//   if (OrdersTotal() == 0 && AccountBalance() == AccountEquity() && Bid - avg0 > 1400*_Point)
//   { 
//      int s0 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,0,0,0,clrIndigo);
//   }
//   
//   if (OrdersTotal() == 1 && AccountEquity() < AccountBalance()-(AccountBalance() * 0.02))
//   { 
//      int s1 = OrderSend(_Symbol,OP_SELL,Lots*3,Bid,0,0,0,0,0,0,clrIndigo);
//   }
//      
//   if (OrdersTotal() == 2 && AccountEquity() < AccountBalance()-(AccountBalance() * 0.03))
//   { 
//      int s2 = OrderSend(_Symbol,OP_SELL,Lots*6,Bid,0,0,0,0,0,0,clrIndigo);
//   }
//         
//   if (OrdersTotal() == 3 && AccountEquity() < AccountBalance()-(AccountBalance() * 0.04))
//   { 
//      int s3 = OrderSend(_Symbol,OP_SELL,Lots*10,Bid,0,0,0,0,0,0,clrIndigo);
//   }
//         
//   if (OrdersTotal() == 0 && Bid - Low[0] > 800 *_Point)
//   { 
//      int s4 = OrderSend(_Symbol,OP_SELL,Lots*20,Bid,0,0,0,0,0,0,clrIndigo);
//   }
//   
//   if(AccountEquity() > AccountBalance() * 1.7)
//   {
//      CloseTrades();
//   } 