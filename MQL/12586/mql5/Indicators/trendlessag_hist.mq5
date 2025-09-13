//+---------------------------------------------------------------------+ 
//|                                                TrendlessAG_Hist.mq5 | 
//|                                          Copyright © 2012, Barmaley | 
//|                                                                     |
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2012, Barmaley"
//--- ссылка на сайт автора
#property link ""
#property description "Осциллятор Бестрендовости написан в соответствии с описанием,"
#property description "приведенным в книге Джо ДиНаполи \"Торговля с применением уровней ДиНаполи\"."
#property description "Дополнительно использовано сглаживание итогового индикатора." 
//--- номер версии индикатора
#property version   "1.01"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//--- количество индикаторных буферов 2
#property indicator_buffers 2 
//--- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//--- отрисовка индикатора в виде четырехцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//--- в качестве цветов четырехцветной гистограммы использованы
#property indicator_color1 clrMagenta,clrDeepPink,clrGray,clrDodgerBlue,clrOliveDrab
//--- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width1 2
//--- отображение метки индикатора
#property indicator_label1 "TrendlessAG_Hist"
//+----------------------------------------------+
//|  Описание классов усреднений                 |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+----------------------------------------------+
//| объявление перечислений                      |
//+----------------------------------------------+
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
//+----------------------------------------------+
//| объявление перечислений                      |
//+----------------------------------------------+
enum Applied_price_ //Тип константы
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
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 +100
#property indicator_level2 +80
#property indicator_level3 +60
#property indicator_level4  0
#property indicator_level5 -60
#property indicator_level6 -80
#property indicator_level7 -100
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method1=MODE_EMA;  // Метод усреднения  в формуле осциллятора
input int XLength1=7;                      // Период скользящей средней в формуле осциллятора                 
input int XPhase1=15;                      // Параметр скользящей средней в формуле осциллятора,
//XPhase1: для JJMA изменяется в пределах -100 ... +100, влияет на качество переходного процесса в формуле осциллятора
//XPhase1: для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE;      // Входная цена скользящей средней в формуле осциллятора
input uint PointsCount=600;                // Количество точек для расчета индикатора. Форекс-клуб рекомендует месяц для часовиков
input uint In100=90;                       // Сколько % точек индикатора должны входить в интервал +-100%
input Smooth_Method XMA_Method2=MODE_JJMA; // Метод сглаживания индикатора
input int XLength2=5;                      // Глубина сглаживания                    
input int XPhase2=100;                     // Параметр сглаживания
//XPhase2: для JJMA изменяется в пределах -100 ... +100, влияет на качество переходного процесса;
//XPhase2: для VIDIA это период CMO, для AMA это период медленной скользящей
//+----------------------------------------------+
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_1,min_rates_2;
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//--- объявление глобальных переменных
int Count[],Start;
double Value[],Sort[];
//+------------------------------------------------------------------+
//| Пересчет позиции самого нового элемента в массиве                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
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
//| TrendlessAG_Hist indicator initialization function               | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчета данных
   min_rates_1=XMA1.GetStartBars(XMA_Method1,XLength1,XPhase1);
   min_rates_2=int(PointsCount+min_rates_1);
   min_rates_total=min_rates_2+XMA1.GetStartBars(XMA_Method2,XLength2,XPhase2)+2;  
   Start=int(PointsCount*In100/100);
//--- распределение памяти под массивы переменных  
   ArrayResize(Count,PointsCount);
   ArrayResize(Value,PointsCount);
   ArrayResize(Sort,PointsCount);
//---
   ArrayInitialize(Count,0);
   ArrayInitialize(Value,0.0);
//--- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- превращение динамического массива в цветовой индексный буфер
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"TrendlessAG_Hist");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| TrendlessAG_Hist iteration function                              | 
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);
//--- объявление переменных с плавающей точкой  
   double price,x1xma;
//--- объявление целочисленных переменных и получение уже подсчитанных баров
   int first,bar;
//--- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=0;                   // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//--- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method1,XPhase1,XLength1,price,bar,false);      
      double Res=price-x1xma;
      Value[Count[0]]=MathAbs(Res);     
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,PointsCount);      
      if(bar<min_rates_2) continue;      
      ArrayCopy(Sort,Value,0,0,WHOLE_ARRAY);
      ArraySort(Sort);      
      double level_100=Sort[Start];
      if(level_100) Res*=100/(level_100);
      else Res=EMPTY_VALUE;       
      IndBuffer[bar]=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method2,XPhase2,XLength2,Res,bar,false);
     }
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//--- основной цикл раскраски индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      ColorIndBuffer[bar]=2;
//---
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) ColorIndBuffer[bar]=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) ColorIndBuffer[bar]=3;
        }
//---
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) ColorIndBuffer[bar]=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) ColorIndBuffer[bar]=1;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
