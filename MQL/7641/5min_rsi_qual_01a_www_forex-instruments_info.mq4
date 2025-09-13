//+------------------------------------------------------------------+
//+ Trade on qualified RSI                                           |
//+------------------------------------------------------------------+
#property copyright "Ron Thompson"
#property link      "http://www.lightpatch.com/forex"
// User Input
extern double Lots=0.1;
extern double Stop_Loss=21;
//+------------------------------------------------------------------+
//| What to do 1st                                                   |
//+------------------------------------------------------------------+
int init ()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double total;
   int cnt, i;
   bool foundorder=false;
   double rsi=0;
   bool flagA=0;
   double nslB=0,nslS=0,osl=0,ccl=0;
   // Error checking
   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);
     }

   if(AccountFreeMargin()<(1000*Lots))
     {
      Print("We have no money");
      return(0);
     }
   // only one order at a time/per symbol 
   // so see if our symbol has an order open
   total=OrdersTotal();
   for(cnt=0;cnt<OrdersTotal();cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()==Symbol())
        {
         // An order for this Symbol() is open so check
         // the stoploss, adjust as price changes
         // in the favorable direction.
         ccl=Close[1]; // 1 NOT 0 or the swings will kill ya!
         osl=OrderStopLoss();
         nslB=ccl-(Stop_Loss*Point);
         nslS=ccl+(Stop_Loss*Point);
         // Existing BUY orders trailing stop
         if(OrderType()==0 )
           {
            if(nslB > osl )
              {
               Print  ("BUY MODIFY ",Symbol()," osl=",osl," nsl=",nslB);
               Comment("BUY MODIFY ",Symbol()," osl=",osl," nsl=",nslB );
               OrderModify(OrderTicket(),OrderOpenPrice(),nslB,OrderTakeProfit(),0,Red);
              }
           }
         // Existing SELL orders trailing stop
         if(OrderType()==1 )
           {
            if(nslS < osl )
              {
               Print  ("SELL MODIFY ",Symbol()," osl=",osl," nsl=",nslS );
               Comment("SELL MODIFY ",Symbol()," osl=",osl," nsl=",nslS );
               OrderModify(OrderTicket(),OrderOpenPrice(),nslS,OrderTakeProfit(),0,Red);
              }
           }
         // set the 'found' flag so we don't buy/sell any more
         foundorder=true;
         break;
        }
     }
   if(foundorder==false )
     {
      Comment(" ");
      //
      rsi=iRSI(Symbol(),0,28,PRICE_CLOSE,1);
      //
      if (rsi>=55)
        {
         flagA=true;
         for(i=1; i<=12; i++)
           {
            if (iRSI(Symbol(),0,28,PRICE_CLOSE,i)<55) flagA=false;
           }
         if (flagA==true)
           {
            // it seems backwards, but it's because qualified RSI turns
            // out to be a top or bottom indicator much more often than
            // it turns out to be a trend indicator.
            Print("SELL Order started  ",Bid);
            OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+(11*Point),0,"11RSI Sell",16321,0,Red);
            if(GetLastError()==0)Comment("SELL Order opened : ",Bid );
           }
        }
      if (rsi<=45)
        {
         flagA=true;
         for(i=1; i<=12; i++)
           {
            if (iRSI(Symbol(),0,28,PRICE_CLOSE,i)>45) flagA=false;
           }
         if (flagA==true)
           {
            // it seems backwards, but it's because qualified RSI turns
            // out to be a top or bottom indicator much more often than
            // it turns out to be a trend indicator.
            Print("BUY  Order started  ",Ask);
            OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-(11*Point),0,"11RSI Buy",16123,0,White);
            if(GetLastError()==0)Comment("BUY Order opened : ",Ask);
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+