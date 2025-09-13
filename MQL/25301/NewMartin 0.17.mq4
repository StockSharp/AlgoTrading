#property strict                                         
double Deviation = 100;
static double D = -100000;

void OnTick()
{  

   double Lots = 0.4;
   double avg0 = iMA(_Symbol,_Period,200,0,MODE_SMA,PRICE_CLOSE,0);
   double avg1 = iMA(_Symbol,_Period,50,0,MODE_SMA,PRICE_CLOSE,0);
   double avg2 = iMA(_Symbol,_Period,5,0,MODE_SMA,PRICE_CLOSE,0);
   double rsi = iRSI(_Symbol,_Period,5,PRICE_CLOSE,0);
   
   
   Comment(" Orders: ", OrdersTotal(),"    ", " Bid: ", Bid, "    ", "Balance: " , AccountBalance(), "    " ," Equity: ", AccountEquity(), "    "," Price: " ,D );

   if(Bid-D > Deviation *_Point && OrdersTotal() < 9){int s1 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,NULL,0,0,0);D = Bid;}
   if(avg2 > avg0 && avg2 - avg0 < 30 *_Point){D = Bid+800*_Point;}
   
   if(AccountEquity() > AccountBalance() * 1.4){CloseTrades();}
   
            
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














//   
//   if(avg2 > avg0 && avg2 - avg0 < 30 *_Point && OrdersTotal() == 0){int s1 = OrderSend(_Symbol,OP_SELLLIMIT,Lots,Bid+900*_Point,0,0,0,NULL,0,0,0);D = Bid;}



   //if(OrdersTotal() == 1 && AccountEquity() > AccountBalance() * 1.001){CloseTrades();D = Bid;}
   //if(OrdersTotal() == 2 && AccountEquity() > AccountBalance() * 1.002){CloseTrades();D = Bid;}
   //if(OrdersTotal() == 3 && AccountEquity() > AccountBalance() * 1.003){CloseTrades();D = Bid;}
   //if(OrdersTotal() == 4 && AccountEquity() > AccountBalance() * 1.004){CloseTrades();D = Bid;}
   //if(OrdersTotal() == 5 && AccountEquity() > AccountBalance() * 1.01 ){CloseTrades();D = Bid;}
   //if(AccountEquity() > AccountBalance() * 1.3){CloseTrades();D = Bid;}





//  int s1 = OrderSend(_Symbol,OP_SELL,Lots*2,Bid,0,0,0,NULL,0,0,0);












//   if(OrdersTotal() < 40 && Bid-Low[1] > 700*_Point && Bid < High[1]){int s2 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,NULL,0,0,0);D = Bid;}



   
   //if(OrdersTotal() == 0 && High[1] - Low[1] > 800*_Point && Bid < Close[1]){int s0 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,NULL,0,0,0);SellIsActive = True;}
   //if(OrdersTotal() == 0 && Bid - Low[24] > 1000*_Point){int s1 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,NULL,0,0,0);SellIsActive = True;}
   //if(OrdersTotal() == 0 && High[0] - Bid > 600*_Point){int s2 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,NULL,0,0,0);SellIsActive = True;}
   //if(OrdersTotal() == 0 && Close[1] < Close[2] && High[2]-Low[6] > 500*_Point){int s3 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,NULL,0,0,0);SellIsActive = True;}
   //if(OrdersTotal() == 0 && High[8] - Bid > 1380 *_Point && Bid > Low[0]){int b0 = OrderSend(_Symbol,OP_BUY,Lots,Ask,0,0,0,NULL,0,0,0);BuyIsActive = True;}
   //if(OrdersTotal() == 0 && avg2-avg0 > 300 *_Point && FirstBuy == 0 && _Symbol == "EURUSD"){int b1 = OrderSend(_Symbol,OP_BUY,Lots,Ask,0,0,0,NULL,0,0,0);BuyIsActive = True; FirstBuy += 1;}




   //if(_Symbol == "EURUSD" && AccountEquity() > AccountBalance() * 1.2){CloseTrades();BuyIsActive = False;SellIsActive = False;}
   //if(_Symbol == "NZDUSD" && AccountEquity() > AccountBalance() * (1+Lots)){CloseTrades();BuyIsActive = False;SellIsActive = False;} 
   //if(_Symbol == "EURJPY" && AccountEquity() > AccountBalance() * (1+Lots)){CloseTrades();BuyIsActive = False;SellIsActive = False;Price = Bid;}     

















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
