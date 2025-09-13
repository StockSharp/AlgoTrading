//+------------------------------------------------------------------+
//|                                                    Forex Sky.mq4 |
//|                          Copyright © 2009, Sivakumar Paulsuyambu |
//|                                 http://softinfolife.blogspot.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

datetime Time0 = 0;

extern int TakeProfit = 100;
extern int StopLoss = 3000;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
           
          if(
             (iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0)>0 && iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0)>0.00009
             && (iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1)<=0 || iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,2)<=0
                 || iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,3)<=0
                  )                  
                  )                 
            )
          { 
           if(OrdersTotal()==0)
           {
              if (Time0 != Time[0] && CheckTodaysOrders()==0)
               {

                OrderSend(Symbol(),OP_BUY,0.1,Ask,2,Ask-StopLoss*Point,Ask+TakeProfit*Point,"My order #2",16384,0,Green);
                Time0 = Time[0];
              
                } 
           }
          } 
           else if(
             (iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0)<0 && iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0)<-0.0004
              && (iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1)>=0 || iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,2)>=0
                 || iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,3)>=0
                  )
              &&    iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,4)>=0.001  )
              || iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,4)>=0.003 
            )
          { 
           if(OrdersTotal()==0)
           {
              if (Time0 != Time[0] && CheckTodaysOrders()==0)
               {

               OrderSend(Symbol(),OP_SELL,0.1,Bid,2,Bid+StopLoss*Point,Bid-TakeProfit*Point,"My order #2",16384,0,Green);
               
                Time0 = Time[0];
              
                } 
           }
          } 
          

//----
   return(0);
  }
//+------------------------------------------------------------------+


int CheckTodaysOrders(){

int TodaysOrders = 0;

for(int i = OrdersTotal()-1; i >=0; i--){

OrderSelect(i, SELECT_BY_POS,MODE_TRADES);

if(TimeDayOfYear(OrderOpenTime()) == TimeDayOfYear(TimeCurrent())){

TodaysOrders += 1;

}

}

for(i = OrdersHistoryTotal()-1; i >=0; i--){

OrderSelect(i, SELECT_BY_POS,MODE_HISTORY);

if(TimeDayOfYear(OrderOpenTime()) == TimeDayOfYear(TimeCurrent())){

TodaysOrders += 1;

}

}

return(TodaysOrders);

}