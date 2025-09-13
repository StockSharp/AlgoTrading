#property strict

static bool BuyIsActive = False;
static bool SellIsActive = False;
string Signal = "N";

void OnTick()
{

   double Lots = AccountBalance()/1000000;
   static double Price = Bid;
   double rsi = iRSI(_Symbol,_Period,12,PRICE_WEIGHTED,0);
   double ma1 = iMA(_Symbol,_Period,12,0,MODE_EMA,PRICE_CLOSE,0);
        
   Comment(" Orders: ", OrdersTotal() ,"    ", " Balance: ", AccountBalance(), "    ", " Equity: ", AccountEquity(), "    " , " Signal: " ,Signal,"    ");
   
   if(Signal == "S" && SellIsActive == False){int s0 = OrderSend(_Symbol,OP_SELL,Lots*20,Bid,0,0,0,NULL,0,clrIndigo);SellIsActive = True; Price = Bid;}
   if(Signal == "B" && BuyIsActive == False){int b0 = OrderSend(_Symbol,OP_BUY ,Lots*20,Ask,0,0,0,NULL,0,clrIndigo);BuyIsActive = True;Price = Bid;}
   if(SellIsActive == True && OrdersTotal() == 1 && Bid-Price > 100*_Point){int s1 = OrderSend(_Symbol,OP_SELL,Lots*20,Bid,0,0,0,NULL,0,clrIndigo);SellIsActive = True; Price = Bid;}
   if(BuyIsActive == True  && OrdersTotal() == 1 && Price-Bid > 100*_Point ){int b1 = OrderSend(_Symbol,OP_BUY ,Lots*20,Ask,0,0,0,NULL,0,clrIndigo);BuyIsActive = True;Price = Bid;} 
   if(SellIsActive == True && OrdersTotal() == 2 && Bid-Price > 100*_Point){int s2 = OrderSend(_Symbol,OP_SELL,Lots*20,Bid,0,0,0,NULL,0,clrIndigo);SellIsActive = True; Price = Bid;}
   if(BuyIsActive == True  && OrdersTotal() == 2 && Price-Bid > 100*_Point ){int b2 = OrderSend(_Symbol,OP_BUY ,Lots*20,Ask,0,0,0,NULL,0,clrIndigo);BuyIsActive = True;Price = Bid;} 
   if(SellIsActive == True && OrdersTotal() == 3 && Bid-Price > 100*_Point){int s3 = OrderSend(_Symbol,OP_SELL,Lots*20,Bid,0,0,0,NULL,0,clrIndigo);SellIsActive = True; Price = Bid;}
   if(BuyIsActive == True  && OrdersTotal() == 3 && Price-Bid > 100*_Point ){int b3 = OrderSend(_Symbol,OP_BUY ,Lots*20,Ask,0,0,0,NULL,0,clrIndigo);BuyIsActive = True;Price = Bid;} 
         
   if(rsi < 20 && Ask < ma1 - 400*_Point){Signal = "B";}    
   if(rsi > 80 && Bid > ma1 + 400*_Point){Signal = "S";}
   if(rsi < 80 && rsi > 20){Signal = "N";} 
   
   if(BuyIsActive == True && AccountEquity() > AccountBalance() * 1.3){CloseTrades();BuyIsActive = False;} 
   if(SellIsActive == True && AccountEquity() > AccountBalance() * 1.3){CloseTrades();SellIsActive = False;}

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















   //if (OrdersTotal() == 1 && SellIsActive == True && AccountEquity() < AccountBalance() - (AccountBalance() * 0.08)){int s1 = OrderSend(_Symbol,OP_SELL,Lots*150,Bid,0,0,0,NULL,0,clrIndigo);SellIsActive = True; Price = Bid;}
   //if (OrdersTotal() == 1 && BuyIsActive == True && AccountEquity() < AccountBalance() - (AccountBalance() * 0.08)){int b1 = OrderSend(_Symbol,OP_BUY ,Lots*150,Ask,0,0,0,NULL,0,clrIndigo);BuyIsActive = True;Price = Bid;







//string GetSignal()
//{  
//   string Signal = "";
//   MathSrand(GetTickCount());
//   int Direction = MathRand()%2;
//   if (Direction == 0){Signal = "B";}
//   if (Direction == 1){Signal = "S";}
//   return(Signal);   
//}
//












//   if (SellIsActive == True && OrdersTotal() == 1 && AccountEquity() < AccountBalance() - (AccountBalance() * 0.03)){int s1 = OrderSend(_Symbol,OP_SELL,Lots*3,Bid,0,0,0,NULL,0,clrIndigo);SellIsActive = True;}
//   if (BuyIsActive == True && OrdersTotal() == 1 && AccountEquity() < AccountBalance() - (AccountBalance() * 0.03)){int b1 = OrderSend(_Symbol,OP_BUY ,Lots*3,Ask,0,0,0,NULL,0,clrIndigo);BuyIsActive = True;}
//  
   



   //double avg0 = iMA(_Symbol,_Period,3,0,MODE_EMA,PRICE_CLOSE,0);
   //double avg1 = iMA(_Symbol,_Period,2,0,MODE_EMA,PRICE_CLOSE,0);
   //double avg2 = iMA(_Symbol,_Period,1,0,MODE_EMA,PRICE_CLOSE,0);
   //double  rsi = iRSI(_Symbol,_Period,5,PRICE_CLOSE,0);
   //double macd = iMACD(_Symbol,_Period,12,26,9,PRICE_CLOSE,MODE_EMA,0);





   //if(BuyIsActive == True && rsi > 80 && Bid - Open[0] > 4000*_Point){CloseTrades(); BuyIsActive = False; Trend = "S";}
   //if(SellIsActive == True && rsi < 20 && Open[0] - Bid > 4000*_Point){CloseTrades(); SellIsActive = False; Trend = "B";}  









//   if(rsi > 80 && BuyIsActive == True && Bid - avg0 > 1300*_Point){CloseTrades();BuyIsActive = False;};   
//   if(rsi < 20 && SellIsActive == True && avg0 - Bid > 1300*_Point){CloseTrades();SellIsActive = False;};




   //if (OrdersTotal() == 0 && AccountBalance() == AccountEquity() && rsi < 30)
   //{ 
   //   int b0 = OrderSend(_Symbol,OP_BUY,Lots,Ask,0,0,0,0,0,0,clrIndigo);
   //   BuyIsActive = True;
   //}



//
//   if (OrdersTotal() == 0 && AccountBalance() == AccountEquity() && Bid - Low[0] > D20 *_Point && Bid > Low[0] && rsi > 70)
//   { 
//      int s1 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,0,0,0,clrIndigo);
//      SellIsActive = True;
//   }
//   
//   if (OrdersTotal() == 1 && SellIsActive == True && Bid - Low[25] > 1800*_Point && Bid > Low[0] && rsi > 70)
//   { 
//      int s2 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,0,0,0,clrIndigo);
//      SellIsActive = True;
//   }
//   
//   if (OrdersTotal() == 2 && SellIsActive == True && Bid - Low[13] > 3000*_Point && Bid > Low[0] && rsi > 70)
//   { 
//      int s3 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,0,0,0,clrIndigo);
//      SellIsActive = True;
//   }
//   
//   if (OrdersTotal() == 3 && SellIsActive == True && Bid - Low[20] > 3000*_Point && Bid > Low[0] && rsi > 70)
//   { 
//      int s4 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,0,0,0,clrIndigo);
//      SellIsActive = True;
//   }
//   
//   if (OrdersTotal() == 4 && SellIsActive == True && Bid - Low[1] > 1600*_Point && Bid > Low[0] && rsi > 70)
//   { 
//      int s5 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,0,0,0,clrIndigo);
//      SellIsActive = True;
//   }
//   
//   
//   if (OrdersTotal() == 0 && Bid - Low[20] > 3200*_Point && Bid > Low[0] && rsi > 70)
//   { 
//      int s6 = OrderSend(_Symbol,OP_SELL,Lots,Bid,0,0,0,0,0,0,clrIndigo);
//      SellIsActive = True;
//   }   











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