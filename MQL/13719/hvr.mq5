//+------------------------------------------------------------------+ 
//|                                                          HVR.mq5 | 
//|                                         Copyright © 2005, Albert | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2005, Albert"
#property link ""
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов
#property indicator_buffers 1 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован фиолетово-синий цвет
#property indicator_color1 clrBlueViolet
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки линии индикатора
#property indicator_label1  "HVR"
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
enum Applied_price_ //тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input Applied_price_ IPC=PRICE_CLOSE_; // Ценовая константа
//+-----------------------------------+
//---- объявление динамического массива, который будет в 
//---- дальнейшем использован в качестве индикаторного буфера
double ExtLineBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,N;
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве кольцевых буферов
int Count[];
double diff[],x6[],x100[];
//+------------------------------------------------------------------+
//| Пересчет позиции самого нового элемента в массиве                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// возврат по ссылке номера текущего значения ценового ряда
                          int Size)    // количество элементов в кольцевом буфере
  {
//----
   int numb,Max1,Max2;
   static int count=1;
//----
   Max2=Size;
   Max1=Max2-1;
//----
   count--;
   if(count<0) count=Max1;
//----
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
  }
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   N=100;
   min_rates_total=N;
//---- распределение памяти под массивы переменных  
   ArrayResize(Count,N);
   ArrayResize(diff,N);
   ArrayResize(x100,N);
   ArrayResize(x6,N);
//---- инициализация массивов переменных
   ArrayInitialize(Count,0);
   ArrayInitialize(diff,0.0);
   ArrayInitialize(x100,0.0);
   ArrayInitialize(x6,0.0);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtLineBuffer,true);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"HVR");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);
//---- объявление переменных с плавающей точкой  
   double  hv6,hv100,mean6,mean100;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int limit,bar,bar0,i;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit=rates_total-2; // расчетное количество всех баров
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      bar0=Count[0];
      diff[bar0]=MathLog(PriceSeries(IPC,bar,open,low,high,close)/PriceSeries(IPC,bar+1,open,low,high,close));
      //----
      for(i=0; i<6; i++) x6[bar0]=diff[Count[i]];
      for(i=0; i<100; i++) x100[bar0]=diff[Count[i]];
      //----
      mean6=0;
      for(i=0; i<6; i++) mean6+=x6[Count[i]];
      mean6/=6;
      //----
      mean100=0;
      for(i=0; i<100; i++) mean100+=x100[Count[i]];
      mean100/=100;
      //----
      hv6=0;
      for(i=0; i<6; i++) hv6+=MathPow(x6[Count[i]]-mean6,2);
      hv6=MathSqrt(hv6/5)*7.211102550927978586238442534941;
      //----
      hv100=0;
      for(i=0; i<100; i++) hv100+=MathPow(x100[Count[i]]-mean100,2);
      hv100=MathSqrt(hv100/99)*7.211102550927978586238442534941;
      //----
      if(hv100) ExtLineBuffer[bar]=hv6/hv100;
      else ExtLineBuffer[bar]=ExtLineBuffer[bar+1];
      //---- пересчет позиций элементов в кольцевых буферах
      if(bar>0) Recount_ArrayZeroPos(Count,N);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+   
//| Получение значения ценовой таймсерии                             |
//+------------------------------------------------------------------+ 
double PriceSeries(uint applied_price,// ценовая константа
                   uint   bar,        // индекс сдвига относительно текущего бара на указанное количество периодов назад или вперед
                   const double &Open[],
                   const double &Low[],
                   const double &High[],
                   const double &Close[])
  {
//----
   switch(applied_price)
     {
      //---- ценовые константы из перечисления ENUM_APPLIED_PRICE
      case  PRICE_CLOSE: return(Close[bar]);
      case  PRICE_OPEN: return(Open [bar]);
      case  PRICE_HIGH: return(High [bar]);
      case  PRICE_LOW: return(Low[bar]);
      case  PRICE_MEDIAN: return((High[bar]+Low[bar])/2.0);
      case  PRICE_TYPICAL: return((Close[bar]+High[bar]+Low[bar])/3.0);
      case  PRICE_WEIGHTED: return((2*Close[bar]+High[bar]+Low[bar])/4.0);
      //----                            
      case  8: return((Open[bar] + Close[bar])/2.0);
      case  9: return((Open[bar] + Close[bar] + High[bar] + Low[bar])/4.0);
      //----                                
      case 10:
        {
         if(Close[bar]>Open[bar])return(High[bar]);
         else
           {
            if(Close[bar]<Open[bar])
               return(Low[bar]);
            else return(Close[bar]);
           }
        }
      //----         
      case 11:
        {
         if(Close[bar]>Open[bar])return((High[bar]+Close[bar])/2.0);
         else
           {
            if(Close[bar]<Open[bar])
               return((Low[bar]+Close[bar])/2.0);
            else return(Close[bar]);
           }
         break;
        }
      //----         
      case 12:
        {
         double res=High[bar]+Low[bar]+Close[bar];
         //----
         if(Close[bar]<Open[bar]) res=(res+Low[bar])/2;
         if(Close[bar]>Open[bar]) res=(res+High[bar])/2;
         if(Close[bar]==Open[bar]) res=(res+Close[bar])/2;
         return(((res-Low[bar])+(res-High[bar]))/2);
        }
      //----
      default: return(Close[bar]);
     }
//----
//return(0);
  }
//+------------------------------------------------------------------+
