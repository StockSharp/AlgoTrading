//+------------------------------------------------------------------+
//|                                               MACDcrossAUDD1.mq4 |
//|                                        Ilkyu Lee, Forex Eternity |
//| History                                                          |
//| =======                                                          |
//| 04/05/12 initial version                                         |
//+------------------------------------------------------------------+
#property copyright "Forex Eternity"

#define  LOTS           0.1
#define  STOPLOSSPIPS   40
#define  REWARDRATIO    3


int init()
{
  Print("Start of AUDUSD D1 MACD cross Robot trading.");
  Print("Trading volume: ",LOTS," lot, StopLoss: ", STOPLOSSPIPS, " pips, Reward ratio: ",REWARDRATIO);
  return(0);
}

int deinit()
{
  Print("End   of AUDUSD D1 MACD cross Robot trading.");
  return(0);
}

int start()
{     
      if (OrdersTotal() < 1)         // 1 trade per time.
      if(Hour() > 5 && Hour() < 15)  // during peak times
      if( Symbol() == "AUDUSD" )     // trading AUDUSD only
      if( Period() == 1440 )         // using daily chart only
          if( iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,2) < iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,2)     // Buy when MACD is crossing from sell zone      
              &&
              iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1) > iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1) )   // to buy zone      
                  OrderSend(Symbol(),OP_BUY,LOTS,Ask,3,Ask-STOPLOSSPIPS*0.0001,Ask+STOPLOSSPIPS*0.0001*REWARDRATIO,"MACD cross Buy",12345,0,Blue);
                  
          else if( iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,2) > iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,2) // Sell when MACD is crossing from buy zone     
              &&
              iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1) < iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1) )    // to sell zone      
                  OrderSend(Symbol(),OP_SELL,LOTS, Bid,3,Bid+STOPLOSSPIPS*0.0001,Bid-STOPLOSSPIPS*0.0001*REWARDRATIO,"MACD cross Sell",12345,0,Red); 
               
      // return
      return(0);              
}