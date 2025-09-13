extern int stopTrade = 600; // This is the minimum value I can take in my account
int tickA, tickB, tickC, tickD, tickE, tickF;

int cnt = 0;
int init()
{
  tickA = 0; tickB =0; tickC=0; tickD=0; tickE=0; tickF= 0;
  return(0);
}

int deinit()
{  
  return(0);
}

int start()
{
   if(tickA <1)
      stopTrade = 600;
      
      
   int total = OrdersTotal();

if(AccountFreeMargin() > 1000 && AccountFreeMargin() < 1300 && (tickB !=1 )){
   stopTrade = 1000;
   tickA = 1;
}
if(AccountFreeMargin() > 1300 && AccountFreeMargin() < 1600&& (tickC !=1 )){
   stopTrade = 1300;
   tickB=1;
}
if(AccountFreeMargin() > 1600 && AccountFreeMargin() < 1900 && (tickD !=1 ))
{ 
   tickC=1;
   stopTrade = 1500;
}
if(AccountFreeMargin() > 1900 && AccountFreeMargin() < 2100&& (tickE !=1 )){
   tickD=1;
   stopTrade = 1800;
 }
 if(AccountFreeMargin() > 2100 && AccountFreeMargin() < 2500&& (tickF !=1 )){
   stopTrade = 2000; 
   tickE=1;
}
 
 if(AccountFreeMargin() > 2500 && AccountFreeMargin() < 3000){
   stopTrade = 2500;
   tickF=1;
}


 
            if(AccountFreeMargin() < stopTrade)
               return(0);
           /* for (cnt = 0 ; cnt <= total ; cnt++)
            {
                  OrderSelect(0,SELECT_BY_POS,MODE_TRADES);
                  if(OrderType()==OP_BUY)
                     OrderClose(OrderTicket(),OrderLots(),Bid,5,Violet);
                  if(OrderType()==OP_SELL)
                     OrderClose(OrderTicket(),OrderLots(),Ask,5,Violet);
                  if(OrderType()>OP_SELL) //pending orders
                     OrderDelete(OrderTicket());
               
            }*/
          

     
         Print (stopTrade);

          
          
          
      if (OrdersTotal() < 5)   // I wont open more than 4 trade per time.
         if(Hour() > 8 && Hour() < 17)  //Will trade during volatile times when people are usually awake
         if ( ( (iMA(NULL,15,3,0,MODE_EMA,PRICE_MEDIAN,0)) < (iMA(NULL,15,34,0,MODE_EMA,PRICE_MEDIAN,0))) && (iSAR(NULL, 15, 0.02, 0.2, 0) > High[0]) &&
            ( iBearsPower(NULL,15,13,PRICE_CLOSE,0) <   0  &&  iBearsPower(NULL,15,13,PRICE_CLOSE,0) >    iBearsPower(NULL,15,13,PRICE_CLOSE,1)))          
               OrderSend(Symbol(),OP_SELL,30,Bid,10,Bid+400*Point,Bid-2000*Point,"Open a Sell Order",16384,0,Red); // Risk/Reward sucks here..
      
      if (OrdersTotal() < 2)   // I wont open more than 1 trade per time.
         if(Hour() > 8 && Hour() < 17)  //Will trade during volatile times when people are usually awake
            if ( ( (iMA(NULL,15,3,0,MODE_EMA,PRICE_MEDIAN,0)) > (iMA(NULL,15,34,0,MODE_EMA,PRICE_MEDIAN,0))) && (iSAR(NULL, 15, 0.02, 0.2, 0) < Low[0])
            &&( iBullsPower(NULL,15,13,PRICE_CLOSE,0) >   0  &&  iBullsPower(NULL,15,13,PRICE_CLOSE,0) <    iBullsPower(NULL,15,13,PRICE_CLOSE,1)))  
               OrderSend(Symbol(),OP_BUY,30,Ask,10,Ask-2000*Point,Ask+400*Point,"Open a Buy Order",16384,0,Green);// Risk/Reward sucks here..

        Print ("Account Free Margin is : "+ AccountFreeMargin ());  //For debugging!
}