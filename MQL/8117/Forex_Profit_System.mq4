//+------------------------------------------------------------------+
//|                                       Система "Forex Profit".mq4 |
//|                                                        fortrader |
//|                                                 www.fortrader.ru |
//+------------------------------------------------------------------+
#property copyright "fortrader"
#property link      "www.fortrader.ru"

//---- input parameters
extern int       TakeProfit = 50;
extern int       TakeProfit1 = 50;
extern int       StopLoss = 30;
extern int       StopLoss1 = 30;
extern int       TrailingStop = 10;
extern int       TrailingStop1 = 10;
extern double    Lots = 0.1;
extern int       per_EMA10 = 10;
extern int       per_EMA25 = 25;
extern int       per_EMA50 = 50;


// уникальный номер (магическое число) для ордеров эксперта
int magic_Buy=10000;//SMA40 и SMA60
int magic_Sell=11000;//SMA40 и SMA60
int magic_Buy1=12000;//SMA5 и SMA20
int magic_Sell1=13000;//SMA5 и SMA20

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
int total, cnt, Buy=0, Sell=0, Buy1=0, Sell1=0;
double EMA10, EMA25, EMA50, EMA10_prew, SAR;
int err;

// Вычисляем начальные параметры индикаторов для поиска условий входа

  EMA10 = iMA(NULL,0,per_EMA10,0,MODE_EMA,PRICE_MEDIAN,1);
  EMA10_prew = iMA(NULL,0,per_EMA10,0,MODE_EMA,PRICE_MEDIAN,2);
  EMA25 = iMA(NULL,0,per_EMA25,0,MODE_EMA,PRICE_MEDIAN,1);
//  SMA40_prew = iMA(NULL,PERIOD_M30,per_SMA40,0,MODE_SMA,PRICE_MEDIAN,2);
  EMA50 = iMA(NULL,0,per_EMA50,0,MODE_EMA,PRICE_MEDIAN,1);
  SAR = iSAR(NULL,0,0.02,0.2,1);
  
    total=OrdersTotal();

  // Проверка средств
  if(AccountFreeMargin()<(1000*Lots))
     {
       Print("We have no money. Free Margin = ", AccountFreeMargin());   
       return(0);  
     }
 if(total<1)
  { 
  // Проверка условий для совершения сделки
  if(EMA10>EMA25 && EMA10>EMA50 && EMA10_prew<=EMA50 && SAR<iClose(NULL,0,1))  
     {
       //Print("BUY SMA40 = ",SMA40 ," SMA60 = ", SMA60 ," SMA40_prew = ",SMA40_prew);
       OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-StopLoss*Point,Ask+TakeProfit*Point,"Покупаем",10000,0,Green);
       //Buy=1;
     }
  if(EMA10<EMA25 && EMA10<EMA50 && EMA10_prew>=EMA50 && SAR>iClose(NULL,0,1))  
     {
       //Print("BUY SMA40 = ",SMA40 ," SMA60 = ", SMA60 ," SMA40_prew = ",SMA40_prew);
        OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+StopLoss1*Point,Bid-TakeProfit1*Point,"Продаем",11000,0,Red);
     }
  }     
  for(cnt=total-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderType()==OP_BUY)
         { 
           if(EMA10<EMA10_prew && OrderProfit()>10) //EMA10<EMA25 || EMA10<EMA50   
             {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
//               Buy=0;
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
           if(EMA10>EMA10_prew && OrderProfit()>5) //EMA10>EMA25 || EMA10>EMA50    
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
//               Sell=0;
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
//+------------------------------------------------------------------+