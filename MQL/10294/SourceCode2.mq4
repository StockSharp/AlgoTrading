int init()
{
  return(0);
}

int deinit()
{  
  return(0);
}

int start()
{     
      int total = OrdersTotal();                  
      if (OrdersTotal() < 5)   // I wont open more than 4 trade per time.
         if(Hour() > 8 && Hour() < 17)  //Will trade during volatile times when people are usually awake
         if ( ( (iMA(NULL,15,3,0,MODE_EMA,PRICE_MEDIAN,0)) < (iMA(NULL,15,34,0,MODE_EMA,PRICE_MEDIAN,0))) && (iSAR(NULL, 15, 0.02, 0.2, 0) > High[0]) &&
            ( iBearsPower(NULL,15,13,PRICE_CLOSE,0) <   0  &&  iBearsPower(NULL,15,13,PRICE_CLOSE,0) >    iBearsPower(NULL,15,13,PRICE_CLOSE,1)))          
               OrderSend(Symbol(),OP_SELL,0.1,Bid,10,Bid+400*Point,Bid-2000*Point,"Open a Sell Order",16384,0,Red); // Risk/Reward sucks here..
      
      if (OrdersTotal() < 5)   // I wont open more than 1 trade per time.
         if(Hour() > 8 && Hour() < 17)  //Will trade during volatile times when people are usually awake
            if ( ( (iMA(NULL,15,3,0,MODE_EMA,PRICE_MEDIAN,0)) > (iMA(NULL,15,34,0,MODE_EMA,PRICE_MEDIAN,0))) && (iSAR(NULL, 15, 0.02, 0.2, 0) < Low[0])
            &&( iBullsPower(NULL,15,13,PRICE_CLOSE,0) >   0  &&  iBullsPower(NULL,15,13,PRICE_CLOSE,0) <    iBullsPower(NULL,15,13,PRICE_CLOSE,1)))  
               OrderSend(Symbol(),OP_BUY,0.1,Ask,10,Ask-2000*Point,Ask+400*Point,"Open a Buy Order",16384,0,Green);// Risk/Reward sucks here..
}