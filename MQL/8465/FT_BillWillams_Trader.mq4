//+------------------------------------------------------------------+
//|                                        FT_BillWillams_Trader.mq4 |
//|                                                     FORTRADER.RU |
//|                                              http://FORTRADER.RU |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"

extern string FT1="------Настройки фрактала:----------";
extern int CountBarsFractal=5;//количество баров из которых состоит фрактал
extern int ClassicFractal=1; //включение выключение классического паттерна
extern int MaxDistance=1000;//включение контроля расстояния от зеленой линии до точки входа
extern string FT2="------Настройки типа пробоя фрактала:----------";
extern int indent=1; //количество пунктов для отступа от максимума и минимума
extern int TypeEntry=2; //тип входа после пробоя фрактала 1 - на текущем баре 2 - на баре закрытия 3 на откате к точке входа после пробоя
extern int RedContol=1; //контролировать находится ли пробойная цена выше ниже уровня красной линии
extern string FT3="------Настройки аллигатора:----------";
extern int jaw_period=13;  // -   Период усреднения синей линии (челюсти аллигатора). 
extern int jaw_shift=8;  // -   Смещение синей линии относительно графика цены. 
extern int teeth_period=8;  // -   Период усреднения красной линии (зубов аллигатора). 
extern int teeth_shift=5;  // -   Смещение красной линии относительно графика цены. 
extern int lips_period=5; //  -   Период усреднения зеленой линии (губ аллигатора). 
extern int lips_shift=3;  // -   Смещение зеленой линии относительно графика цены. 
extern int ma_method=0;   //- от 0 до 3 метод усреднения. Может быть любым из значений методов скользящего среднего (Moving Average). 
extern int applied_price=4; // - от 0 до 6  -   Используемая цена. Может быть любой из ценовых констант. 
extern string FT4="-------Настройки контроля тренда по аллигатору:----------";
extern int TrendAligControl=0; // включение контроля тренда по алигатору
extern int jaw_teeth_distense=10; //разница между зеленой и красной
extern int teeth_lips_distense=10;//разница между красной и синией
extern string FT5="-------Настройки контроля закрытия сделки:----------";
extern int CloseDropTeeth=2; //Включение закрытия сделки при косании или пробое челюсти. 0 - отключение 1 - по касанию 2 по закрытию бара
extern int CloseReversSignal=2;//Включение закрытия сделки при 1- образовании обратного фрактала 2 - при срабатывании обратного фрактала 0 выключено 
extern string FT6="-------Настройки сопровождения StopLoss сделки:----------";
extern int TrailingGragus=1; //Включение трейлинг стопа по ценовому градусу наклона, если сильный угол то трейлинг по зеленой, если малый угол то трейлинг по красной
extern int smaperugol=5;
extern int raznica=5;
extern string FT7="-------Настройки  StopLoss и TakeProfit ибьема сделки:----------";
extern double  StopLoss=50;
extern double  TakeProfit=50;
extern double  Lots=0.1;

int start()
  {

   ClassicFractal();
   return(0);
  }
  double oldopb,opb,ops,oldops, otkatb,otkats;
  int fractalnew,vpravovlevo,numsredbar,colish;
  
 int  ClassicFractal()
  {   int buy,sell;double sl,tp;
   

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
   if(High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,numsredbar+1)] && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,1)] && (RedContol(Close[1],0)==true && RedContol==1))
   {
   opb=NormalizeDouble(High[numsredbar]+indent*Point,4);

   }
    //проверка входа на касании или по закрытию бара
   if(buy==0&&  ((Ask>opb && TypeEntry==1 ) || (Close[1]>opb && TypeEntry==2)) 
   && opb!=oldopb && MaxDistance(opb)==true && opb>0 
   && ((RedContol(Close[1],0)==true && RedContol==1) || RedContol==0)
   && ((TrendAligControl(0)==true && TrendAligControl==1) || TrendAligControl==0))
   {oldopb=opb;
   sl=NormalizeDouble(Ask-StopLoss*Point,4);
   tp=NormalizeDouble(Ask+TakeProfit*Point,4);
   OrderSend(Symbol(),OP_BUY,Lots,Ask,3,sl,tp,"FORTRADER.RU",16384,10,Green);
   }
   
   /*------------------------------------------ПРОДАЖА----------------------------------------*/
   
   //найдем фрактал на продажу
   if(Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,numsredbar+1)] && Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,0)]  && (RedContol(Close[1],1)==true && RedContol==1) )
   {
   ops=NormalizeDouble(Low[numsredbar]-indent*Point,4);
  

   }
   //проверка входа на касании или по закрытию бара
   if(sell==0&& ( (Bid<ops && TypeEntry==1) ||  (Close[1]<ops && TypeEntry==2))   
   && oldops!=ops && MaxDistance(ops)==true 
   && ((RedContol(Close[1],1)==true && RedContol==1) ||RedContol==0)
   && ((TrendAligControl(1)==true && TrendAligControl==1) || TrendAligControl==0))
   {
   oldops=ops;
   sl=NormalizeDouble(Bid+StopLoss*Point,4);
   tp=NormalizeDouble(Bid-TakeProfit*Point,4);
   OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sl,tp,"FORTRADER.RU",16384,10,Green);
   }
   

  return(0);
  }

bool MaxDistance(double entryprice)
{

double lips=iMA(NULL,0,lips_period,lips_shift,ma_method,applied_price,1);

if(MathAbs(entryprice-lips)<MaxDistance*Point){return(true);}
return(false);
}

bool RedContol(double entryprice,int  type)
{

double teeth=iMA(NULL,0,teeth_period,teeth_shift,ma_method,applied_price,1);

if(entryprice>teeth && type==0){return(true);}
if(entryprice<teeth && type==1){return(true);}
return(false);
}

bool TrendAligControl(int type)
{

double teeth=iMA(NULL,0,teeth_period,teeth_shift,ma_method,applied_price,1);
double lips=iMA(NULL,0,lips_period,lips_shift,ma_method,applied_price,1);
double jaw=iMA(NULL,0,jaw_period,jaw_shift,ma_method,applied_price,1);


if(type==0 && lips-teeth>teeth_lips_distense*Point && teeth-jaw>jaw_teeth_distense*Point ){return(true);}
if(type==1 && teeth-lips>teeth_lips_distense*Point && jaw-teeth>jaw_teeth_distense*Point ){return(true);}


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
          
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_BUY && TrailingGragus==1 && lips-lipsl>sma-smal && OrderProfit()>0)
             {
             if(OrderStopLoss()<lips)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),lips,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
           
           
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_BUY && TrailingGragus==1 && lips-lipsl<=sma-smal && OrderProfit()>0)
             {
             if(OrderStopLoss()<teeth || lips>teeth)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),teeth,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
         
         if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
           if(OrderType()==OP_SELL && ((CloseDropTeeth==1 && Ask>=jaw ) ||  (CloseDropTeeth==2 && Close[1]>=jaw )))
           {
           OrderClose(OrderTicket(),Lots,Ask,3,Violet); 
           return(0);
           }
         }
          
          if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
           if(OrderType()==OP_SELL && ((CloseReversSignal==1 && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,numsredbar+1)] && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,1)]) 
           ||  (CloseReversSignal==2 && buy==1  )))
           {
           OrderClose(OrderTicket(),Lots,Ask,3,Violet); 
           return(0);
           }
         }     
         
         
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_SELL && TrailingGragus==1 && lipsl-lips<smal-sma && OrderProfit()>0)
             {
             if(OrderStopLoss()>lips)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),lips,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
           
           
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_SELL && TrailingGragus==1 && lipsl-lips>smal-sma && OrderProfit()>0)
             {
             if(OrderStopLoss()>teeth || lips<teeth)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),teeth,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
                      
       }
       

}