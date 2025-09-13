//+---------------------------------------------------------------------+
//|                                                  AFL_WinnerSign.mq5 | 
//|                                   Copyright © 2011, Andrey Voytenko | 
//|                           https://login.mql5.com/en/users/avoitenko | 
//+---------------------------------------------------------------------+ 
//| Для работы индикатора следует положить файл SmoothAlgorithms.mqh    |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, Andrey Voytenko"
#property link "https://login.mql5.com/en/users/avoitenko"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован Magenta цвет
#property indicator_color1  clrMagenta
//--- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//--- отображение бычей метки индикатора
#property indicator_label1  "NRatioSign Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьго индикатора        |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычей линии индикатора использован BlueViolet цвет
#property indicator_color2  clrBlueViolet
//--- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//--- отображение медвежьей метки индикатора
#property indicator_label2 "NRatioSign Buy"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//--- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//| объявление констант               |
//+-----------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчет индикатора
//+-----------------------------------+
//| объявление перечислений           |
//+-----------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//| объявление перечислений           |
//+-----------------------------------+
/*enum Smooth_Method - перечисление объявлено в файле SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint iAverage=5;                            // Период для обработки входных данных
input uint iPeriod=10;                            // Период поиска экстремумов
input Smooth_Method iMA_Method=MODE_SMA;          // Метод усреднения первого сглаживания 
input uint iLength=5;                             // Глубина сглаживания
input int iPhase=15;                              // Параметр сглаживания
//--- iPhase: для JJMA изменяется в пределах -100 ... +100, влияет на качество переходного процесса;
//--- iPhase: для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_WEIGHTED;          // Ценовая константа
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK; // Объем
input int Shift=0;                                // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double SellBuffer[],BuyBuffer[];
//--- объявление глобальных переменных
int Count1[],Count2[];
double Value[],Price[];
int ATR_Handle;
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+
//| Пересчет позиции самого нового элемента в массиве                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos1(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
                           int Size)
  {
//---
   int numb,Max1,Max2;
   static int count=1;
//---
   Max2=Size;
   Max1=Max2-1;
//---
   count--;
   if(count<0) count=Max1;
//---
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//---
  }
//+------------------------------------------------------------------+
//| Пересчет позиции самого нового элемента в массиве                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos2(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
                           int Size)
  {
//---
   int numb,Max1,Max2;
   static int count=1;
//---
   Max2=Size;
   Max1=Max2-1;
//---
   count--;
   if(count<0) count=Max1;
//---
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//---
  }
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- инициализация переменных начала отсчета данных
   min_rates_1=int(iAverage);
   min_rates_2=min_rates_1+int(iPeriod);
   min_rates_3=min_rates_2+XMA1.GetStartBars(iMA_Method,iLength,iPhase);
   min_rates_total=min_rates_3+XMA1.GetStartBars(iMA_Method,iLength,iPhase);
   int ATR_Period=10;
   min_rates_total=int(MathMax(min_rates_total+1,ATR_Period));
//--- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Length",iLength);
//--- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("Phase",iPhase,iMA_Method);
//--- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }
//--- распределение памяти под массивы переменных  
   ArrayResize(Count1,iPeriod);
   ArrayResize(Value,iPeriod);
   ArrayResize(Count2,iAverage);
   ArrayResize(Price,iAverage);
//---
   ArrayInitialize(Count1,0);
   ArrayInitialize(Value,0.0);
   ArrayInitialize(Count2,0);
   ArrayInitialize(Price,0.0);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,175);
//--- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,175);
//--- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- инициализации переменной для короткого имени индикатора
   string shortname="AFL_WinnerSign";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
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
//--- проверка количества баров на достаточность для расчета
   if(BarsCalculated(ATR_Handle)<Bars(Symbol(),PERIOD_CURRENT) || rates_total<min_rates_total) return(RESET);
//--- объявление переменных с плавающей точкой  
   double rsv,scost5,max,min,x1xma,x2xma,ATR[1];
//--- объявление целочисленных переменных и получение уже подсчитанных баров
   int first,bar,trend0;
   long svolume5;
   static int trend1;
//--- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=0; // стартовый номер для расчета всех баров
      trend1=0;
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//--- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //--- вызов функции PriceSeries для получения входной цены
      Price[Count2[0]]=PriceSeries(IPC,bar,open,low,high,close);
      if(bar<min_rates_1-1)
        {
         if(bar<rates_total-1) Recount_ArrayZeroPos2(Count2,iAverage);
         continue;
        }
      //--- 
      scost5=0;
      svolume5=0;
      for(int kkk=0; kkk<int(iAverage); kkk++)
        {
         long res;
         if(VolumeType==VOLUME_TICK) res=long(tick_volume[bar-kkk]);
         else res=long(volume[bar-kkk]);
         scost5+=res*Price[Count2[kkk]];
         svolume5+=res;
        }
      svolume5=MathMax(svolume5,1);
      Value[Count1[0]]=scost5/svolume5;
      //---
      if(bar<min_rates_2-1)
        {
         if(bar<rates_total-1)
           {
            Recount_ArrayZeroPos1(Count1,iPeriod);
            Recount_ArrayZeroPos2(Count2,iAverage);
           }
         continue;
        }
      //---
      max=Value[ArrayMaximum(Value,0,iPeriod)];
      min=Value[ArrayMinimum(Value,0,iPeriod)];
      rsv=((Value[Count1[0]]-min)/MathMax(max-min,_Point))*100-50;
      x1xma=XMA1.XMASeries(min_rates_2,prev_calculated,rates_total,iMA_Method,iPhase,iLength,rsv,bar,false);
      x2xma=XMA2.XMASeries(min_rates_3,prev_calculated,rates_total,iMA_Method,iPhase,iLength,x1xma,bar,false);
      //---  
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //---      
      if(x1xma>x2xma)
        {
         if(trend1<=0)
           {
            //--- копируем вновь появившиеся данные в массив
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
           }
         trend0=+1;
        }
      else
        {
         if(trend1>=0)
           {
            //--- копируем вновь появившиеся данные в массив
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            SellBuffer[bar]=high[bar]+ATR[0]*3/8;
           }
         trend0=-1;
        }
      //---
      if(bar<rates_total-1)
        {
         trend1=trend0;
         Recount_ArrayZeroPos1(Count1,iPeriod);
         Recount_ArrayZeroPos2(Count2,iAverage);
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
