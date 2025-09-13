//+------------------------------------------------------------------+
//|                                                    Get trend.mq4 |
//|                                                                  |
//|                                          http://www.fortrader.ru |
//+------------------------------------------------------------------+
#property link      "http://www.fortrader.ru"
//---- input parameters
extern int       porog = 50;
extern int       per_MA1 = 200;
extern int       per_MA2 = 200;
extern int       per_Stoh_slow = 14;
extern int       per_Stoh_fast = 14;
extern int       TakeProfit = 570;
extern int       StopLoss = 30;
extern int       TrailingStop = 200;
extern double       Lots = 0.1;
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
int total, cnt;
double MA1, MA2, Stoh_slow, Stoh_fast, Stoh_fast_prew, price_M15, price_H1;
int err;

// Вычисляем начальные параметры индикаторов для поиска условий входа

  MA1 = iMA(NULL,PERIOD_M15,per_MA1,8,MODE_SMMA,PRICE_MEDIAN,1);
  MA2 = iMA(NULL,PERIOD_H1,per_MA2,8,MODE_SMMA,PRICE_MEDIAN,1);
  
  Stoh_slow = iStochastic(NULL,PERIOD_M15,per_Stoh_slow,3,3,MODE_SMA,0,MODE_SIGNAL,1);
  Stoh_fast = iStochastic(NULL,PERIOD_M15,per_Stoh_fast,3,3,MODE_SMA,0,MODE_MAIN,1);
  Stoh_fast_prew = iStochastic(NULL,PERIOD_M15,per_Stoh_fast,3,3,MODE_SMA,0,MODE_MAIN,2);
  
  price_M15 = iClose(NULL,15,1);
  //Print(" price_M15 = ", price_M15);
  price_H1 = iClose(NULL,60,1);
  
  total=OrdersTotal();

  // Проверка средств
  if(AccountFreeMargin()<(1000*Lots))
     {
       Print("We have no money. Free Margin = ", AccountFreeMargin());   
       return(0);  
     }
  
  // Проверка условий для совершения сделки
  if(price_M15<MA1 && price_H1<MA2 && (MA1-price_M15)<=porog*Point)
     {
       if(Stoh_slow<20 && Stoh_fast<20 && Stoh_fast_prew<Stoh_slow && Stoh_fast>Stoh_slow)
          {
            Print("BUY разница (MA1-price_M15) = ",(MA1-price_M15)," MA1 = ", MA1," price_M15 = ", price_M15);
            OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-StopLoss*Point,Ask+TakeProfit*Point,"Покупаем",16384,0,Green);
          }
     }

  if(price_M15>MA1 && price_H1>MA2 && (price_M15-MA1)<=porog*Point)
     {
       if(Stoh_slow>80 && Stoh_fast>80 && Stoh_fast_prew>Stoh_slow && Stoh_fast<Stoh_slow)
          {
            Print("Sell разница (price_M15-MA1) = ",(price_M15-MA1)," MA1 = ", MA1," price_M15 = ", price_M15);
            OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+StopLoss*Point,Bid-TakeProfit*Point,"Продаем",16385,0,Red);
          }
     }
     
  for(cnt=total-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderType()==OP_BUY)
         {
           if(TrailingStop>0)  
             {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop) // Bid - цена покупки
                 {
                   if(OrderStopLoss()<Bid-Point*TrailingStop)
                     {
                       OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
                       return(0);
                     }
                 }
             }
         }
       if(OrderType()==OP_SELL)
         {
           if(TrailingStop>0)  
             {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))  // Ask - цена продажи
                 {
                   if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                     {
                       OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                       return(0);
                     }
                 }
             }
         }
  
     }

   
//----
   return(0);
  }
//+------------------------------------------------------------------+