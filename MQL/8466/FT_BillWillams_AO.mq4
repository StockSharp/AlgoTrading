//+------------------------------------------------------------------+
//|                                            FT_BillWillams_AO.mq4 |
//|                                                     FORTRADER.RU |
//|                                              http://FORTRADER.RU |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"

extern string FT1="------Настройки фрактала:----------";
extern int CountBarsFractal=5;//количество баров из которых состоит фрактал
extern string FT2="------Настройки сигнала на вход:----------";
extern int indent=1; //количество пунктов для отступа от максимума и минимума
extern string FT3="------Настройки аллигатора:----------";
extern int jaw_period=13;  // -   Период усреднения синей линии (челюсти аллигатора). 
 int jaw_shift=8;  // -   Смещение синей линии относительно графика цены. 
extern int teeth_period=8;  // -   Период усреднения красной линии (зубов аллигатора). 
 int teeth_shift=5;  // -   Смещение красной линии относительно графика цены. 
extern int lips_period=5; //  -   Период усреднения зеленой линии (губ аллигатора). 
 int lips_shift=3;  // -   Смещение зеленой линии относительно графика цены. 
 int ma_method=0;   //- от 0 до 3 метод усреднения. Может быть любым из значений методов скользящего среднего (Moving Average). 
 int applied_price=4; // - от 0 до 6  -   Используемая цена. Может быть любой из ценовых констант. 
 
extern string FT5="-------Настройки контроля закрытия сделки:----------";
extern int CloseDropTeeth=2; //Включение закрытия сделки при косании или пробое челюсти. 0 - отключение 1 - по касанию 2 по закрытию бара
extern int CloseReversSignal=2;//Включение закрытия сделки при 1- образовании обратного фрактала 2 - при срабатывании обратного фрактала 0 выключено 
 
extern string FT6="-------Настройки сопровождения StopLoss сделки:----------";
extern int TrailingGragus=1; //Включение трейлинг стопа по ценовому градусу наклона, если сильный угол то трейлинг по зеленой, если малый угол то трейлинг по красной
extern int smaperugol=5;
extern int raznica=5;
 
extern string FT7="-------Настройки  StopLoss и TakeProfit ибьема сделки:----------";
extern double  StopLoss=500;
extern double  TakeProfit=500;
extern double  Lots=0.1;

extern int shift=1;


  int fractalnew,vpravovlevo,numsredbar,colish;
  int signal,signals;
  double oldopb,opb,ops,oldops, buyprice,sellprice;
  int buy,sell;
  
int start()
  {
   //управление позами
   ClassicFractalPosManager();
   
        buy=0;sell=0;
      for(int  i=0;i<OrdersTotal();i++)
      {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()==OP_BUY ){buy=1;}
      if(OrderType()==OP_SELL ){sell=1;}
      }  
  
     //найдем скоьлко смотреть вправо и в лево
   vpravovlevo=(CountBarsFractal-1)/2;
   numsredbar=(CountBarsFractal-vpravovlevo);
   colish=numsredbar-1;
   
   /*----------------------------------------ПОКУПКА------------------------------------------*/
   
   //найдем фрактал на покупку
   if(signal==0 && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,numsredbar+1)] && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,1)] && RedContol(High[numsredbar],0)==true && buy==0)
   {
   signal=signal+1;   
   }
   
   double A =iAO(NULL,0,shift+2);
   double B =iAO(NULL,0,shift+1);
   double C =iAO(NULL,0,shift);
   
   //проверим не заходил ли С меньше нуля
   if(C<0){signal=0;}
   
   //проверим не пора ли размещать покупк
   if(A>B && B<C && C>0 && B>0 && A>0 && signal==1){signal=signal+1;buyprice=High[shift]+indent*Point;}
   
   //проверим не пора ли входить в рынок
   if(Ask>=buyprice && signal==2 && C>B)
   {
   double sl=NormalizeDouble(Ask-StopLoss*Point,4);
   double tp=NormalizeDouble(Ask+TakeProfit*Point,4);
   OrderSend(Symbol(),OP_BUY,Lots,Ask,3,sl,tp,"FORTRADER.RU",16384,10,Green);
   signal=0;
   }
   
   
   /*------------------------------------------ПРОДАЖА----------------------------------------*/
   
   //найдем фрактал на продажу
   if(signals==0&& Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,numsredbar+1)] && Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,0)]  && RedContol(Low[numsredbar],1)==true && sell==0 )
   {
   signals=signals+1;   
   }
   
   //проверим не заходил ли С больше нуля
   if(C>0 && signals==1){signals=0;}
   
    //проверим не пора ли размещать покупк
   if(A<B && B>C && C<0 && B<0 && A<0 && signals==1){signals=signals+1;sellprice=Low[shift]-indent*Point;}

   //проверим не пора ли входить на продажу
   if(Bid<=sellprice && signals==2 && C<B)
   {
   sl=NormalizeDouble(Bid+StopLoss*Point,4);
   tp=NormalizeDouble(Bid-TakeProfit*Point,4);
   OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sl,tp,"FORTRADER.RU",16384,10,Green);
   signals=0;
   }
    
   return(0);
  }






/********************************Функциии******************************************/

   //функция контроля цены фрактала, ниже она пасти или выше
   bool RedContol(double entryprice,int  type)
   {

   double teeth=iMA(NULL,0,teeth_period,teeth_shift,ma_method,applied_price,1);

   if(entryprice>teeth && type==0){return(true);}
   if(entryprice<teeth && type==1){return(true);}
   return(false);
   }
   
int ClassicFractalPosManager()
{int i,buy,sell;
double jaw=iMA(NULL,0,jaw_period,jaw_shift,ma_method,applied_price,1);
double teeth=iMA(NULL,0,teeth_period,teeth_shift,ma_method,applied_price,1);
double lips=iMA(NULL,0,lips_period,lips_shift,ma_method,applied_price,1);
double lipsl=iMA(NULL,0,lips_period,lips_shift,ma_method,applied_price,2);
double sma=iMA(NULL,0,smaperugol,0,MODE_SMA,PRICE_CLOSE,1);
double smal=iMA(NULL,0,smaperugol,0,MODE_SMA,PRICE_CLOSE,2);

      buy=0;sell=0;
      for(  i=0;i<OrdersTotal();i++)
      {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()==OP_BUY ){buy=1;}
      if(OrderType()==OP_SELL ){sell=1;}
      }  
//extern int CloseDropTeeth=2; //Включение закрытия сделки при косании или пробое челюсти. 0 - отключение 1 - по касанию 2 по закрытию бара
//extern int CloseReversSignal=2;//Включение закрытия сделки при 1- образовании обратного фрактала 2 - при срабатывании обратного фрактала 0 выключено 
   for( i=1; i<=OrdersTotal(); i++)          
     {
         if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
            if(OrderType()==OP_BUY && ((CloseDropTeeth==1 && Bid<=jaw ) ||  (CloseDropTeeth==2 && Close[1]<=jaw )))
            { 
            OrderClose(OrderTicket(),Lots,Bid,3,Violet); 
            return(0);
            }
          }
      }
      
         for( i=1; i<=OrdersTotal(); i++)          
     {
          
         if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
            if(OrderType()==OP_BUY && 
            ((CloseReversSignal==1 && Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,numsredbar+1)] && Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,0)] ) 
            ||(CloseReversSignal==2 && sell==1 )))
            { 
            OrderClose(OrderTicket(),Lots,Bid,3,Violet); 
            return(0);
            }
          }
      }
      
         for( i=1; i<=OrdersTotal(); i++)          
     {
          
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_BUY && TrailingGragus==1 && lips-lipsl>sma-smal && OrderProfit()>0)
             {
             if(OrderStopLoss()<lips && MathAbs(Bid-lips)>12*Point)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),lips,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
      }     
       
          for( i=1; i<=OrdersTotal(); i++)          
     {    
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_BUY && TrailingGragus==1 && lips-lipsl<=sma-smal && OrderProfit()>0)
             {
             if((OrderStopLoss()<teeth || lips>teeth) && MathAbs(Ask-teeth)>12*Point)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),teeth,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
      }
      
         for( i=1; i<=OrdersTotal(); i++)          
     {   
         if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
           if(OrderType()==OP_SELL && ((CloseDropTeeth==1 && Ask>=jaw ) ||  (CloseDropTeeth==2 && Close[1]>=jaw )))
           {
           OrderClose(OrderTicket(),Lots,Ask,3,Violet); 
           return(0);
           }
         }
      }
      
         for( i=1; i<=OrdersTotal(); i++)          
     {    
          if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
           if(OrderType()==OP_SELL && ((CloseReversSignal==1 && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,numsredbar+1)] && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,1)]) 
           ||  (CloseReversSignal==2 && buy==1  )))
           {
           OrderClose(OrderTicket(),Lots,Ask,3,Violet); 
           return(0);
           }
         }     
      }   
       
          for( i=1; i<=OrdersTotal(); i++)          
     {  
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_SELL && TrailingGragus==1 && lipsl-lips<smal-sma && OrderProfit()>0)
             {
             if(OrderStopLoss()>lips && MathAbs(Ask-lips)>12*Point)
              {
             OrderModify(OrderTicket(),OrderOpenPrice(),lips,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
      }     
       
          for( i=1; i<=OrdersTotal(); i++)          
     {    
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_SELL && TrailingGragus==1 && lipsl-lips>smal-sma && OrderProfit()>0)
             {
             if((OrderStopLoss()>teeth || lips<teeth) && MathAbs(Ask-teeth)>12*Point)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),teeth,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
                      
       }
       

}