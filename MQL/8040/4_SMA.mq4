//+------------------------------------------------------------------+
//|                                                        4 SMA.mq4 |
//|                                                        fortrader |
//|                                                 www.fortrader.ru |
//+------------------------------------------------------------------+
#property copyright "fortrader"
#property link      "www.fortrader.ru"

//---- input parameters
extern int       TakeProfit = 50;
extern int       StopLoss = 50;
extern int       TrailingStop = 11;
extern double    Lots = 0.1;
extern int       per_SMA5 = 5;
extern int       per_SMA20 = 20;
extern int       per_SMA40 = 40;
extern int       per_SMA60 = 60;
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
//----
// Объявляем переменные
int total, cnt, Buy=0, Sell=0;
double SMA5, SMA20, SMA40, SMA60, SMA40_prew;
int err;

// Вычисляем начальные параметры индикаторов для поиска условий входа

  SMA5 = iMA(NULL,PERIOD_M30,per_SMA5,0,MODE_SMA,PRICE_MEDIAN,1);
  SMA20 = iMA(NULL,PERIOD_M30,per_SMA20,0,MODE_SMA,PRICE_MEDIAN,1);
  SMA40_prew = iMA(NULL,PERIOD_M30,per_SMA40,0,MODE_SMA,PRICE_MEDIAN,2);
  SMA40 = iMA(NULL,PERIOD_M30,per_SMA40,0,MODE_SMA,PRICE_MEDIAN,1);
  SMA60 = iMA(NULL,PERIOD_M30,per_SMA60,0,MODE_SMA,PRICE_MEDIAN,1);
  
    total=OrdersTotal();

  // Проверка средств
  if(AccountFreeMargin()<(1000*Lots))
     {
       Print("We have no money. Free Margin = ", AccountFreeMargin());   
       return(0);  
     }
 //if(total<1)
 // { 
  // Проверка условий для совершения сделки
  if(Buy<1 && SMA5>SMA20 && SMA20>SMA40 && (SMA40-SMA60)>=0.0001 && SMA40_prew<=SMA60)  
     {
            Print("BUY SMA40 = ",SMA40 ," SMA60 = ", SMA60 ," SMA40_prew = ",SMA40_prew);
            OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-StopLoss*Point,Ask+TakeProfit*Point,"Покупаем",16384,0,Green);
            Buy=1;
            //return(0);
     }

  if(Sell<1 && SMA5<SMA20 && SMA20<SMA40 && (SMA60-SMA40)>=0.0001 && SMA40_prew>=SMA60) 
     {
            Print("SELL SMA40 = ",SMA40 ," SMA60 = ", SMA60 ," SMA40_prew = ",SMA40_prew);
            OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+StopLoss*Point,Bid-TakeProfit*Point,"Продаем",16385,0,Red);
            Sell=1;
            //return(0);
     }
 // }     
  for(cnt=total-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderType()==OP_BUY)
         { 
           if(SMA40<=SMA60)
             {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
               Buy=0;
//               return(0); // exit
             }
           if(TrailingStop>0)  
             {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop) // Bid - цена покупки
                 {
                   if(OrderStopLoss()<Bid-Point*TrailingStop)
                     {
                       OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
                       //return(0);
                     }
                 }
             }
         }
       if(OrderType()==OP_SELL)
         {
           if(SMA40>=SMA60)
             {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
               Sell=0;
//               return(0); // exit
             }
           if(TrailingStop>0)  
             {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))  // Ask - цена продажи
                 {
                   if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                     {
                       OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                       //return(0);
                     }
                 }
             }
         }
     }
     
   
//----
   return(0);
  }
//+------------------------------------------------------------------+   }
     }
     
   
//----
   return(0);
  }
//+------------------------------------------------------------------+