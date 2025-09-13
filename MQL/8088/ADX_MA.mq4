//+------------------------------------------------------------------+
//|                                                     ADX & MA.mq4 |
//|                                                        fortrader |
//|                                                 www.fortrader.ru |
//+------------------------------------------------------------------+
#property copyright "fortrader"
#property link      "www.fortrader.ru"

//---- input parameters
extern int       per_MA = 21;
extern int       per_ADX = 14;
extern int       porog_ADX = 16;
extern int       TakeProfit_Buy = 1300;
extern int       StopLoss_Buy = 30;
extern int       TrailingStop_Buy = 270;
extern int       TakeProfit_Sell = 160;
extern int       StopLoss_Sell = 50;
extern int       TrailingStop_Sell = 20;
extern double     Lots = 0.1;


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
  
  if (Volume[0] > 1) return(0);

//----
// Объявляем переменные
int total, cnt;
double MA, ADX;
int err;

// Вычисляем начальные параметры индикаторов для поиска условий входа

  MA = iMA(NULL,0,per_MA,0,MODE_SMMA,PRICE_MEDIAN,1);
  ADX = iADX(NULL,0,per_ADX,PRICE_MEDIAN,MODE_MAIN,1);
  
  total=OrdersTotal();

  // Проверка средств
  if(AccountFreeMargin()<(1000*Lots))
     {
       Print("We have no money. Free Margin = ", AccountFreeMargin());   
       return(0);  
     }
  
  // Проверка условий для совершения сделки
  if(iClose(NULL,0,1)>MA && iClose(NULL,0,2)<MA && ADX>porog_ADX)
     {
       OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-StopLoss_Buy*Point,Ask+TakeProfit_Buy*Point,"Покупаем",16384,0,Green);
     }

  if(iClose(NULL,0,1)<MA && iClose(NULL,0,2)>MA && ADX>porog_ADX)
     {
       OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+StopLoss_Sell*Point,Bid-TakeProfit_Sell*Point,"Продаем",16385,0,Red);
     }
     
  for(cnt=total-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderType()==OP_BUY)
         {
           if(iClose(NULL,0,1)<MA)
             {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
             }
           if(TrailingStop_Buy>0)  
             {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop_Buy) // Bid - цена покупки
                 {
                   if(OrderStopLoss()<Bid-Point*TrailingStop_Buy)
                     {
                       OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop_Buy,OrderTakeProfit(),0,Green);
                       return(0);
                     }
                 }
             }
         }
       if(OrderType()==OP_SELL)
         {
           if(iClose(NULL,0,1)>MA)
             {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
             }
           if(TrailingStop_Sell>0)  
             {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop_Sell))  // Ask - цена продажи
                 {
                   if((OrderStopLoss()>(Ask+Point*TrailingStop_Sell)) || (OrderStopLoss()==0))
                     {
                       OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop_Sell,OrderTakeProfit(),0,Red);
                       return(0);
                     }
                 }
             }
         }
  
     }

   
//----   return(0);
  }
//+------------------------------------------------------------------+