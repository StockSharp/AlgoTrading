//+------------------------------------------------------------------+
//|                                                    HullTrend.mq5 |
//|                                     Copyright © 2005, adoleh2000 |
//|                                             adoleh2000@yahoo.com |
//+------------------------------------------------------------------+
#property  copyright "Copyright © 2005, adoleh2000."
#property  link      "adoleh2000@yahoo.com"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window
//--- количество индикаторных буферов 2
#property indicator_buffers 2 
//--- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrMediumSpringGreen,clrViolet
//--- отображение метки индикатора
#property indicator_label1  "HullTrend"
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3;
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
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint XLength=20;                           // Период индикатора
input Applied_price_ IPC=PRICE_CLOSE;            // Цена индикатора
input Smooth_Method XMA_Method=MODE_LWMA;        // Метод усреднения
input int XPhase=15;                             // Параметр сглаживания
//--- для JJMA изменяющийся в пределах -100..+100, влияет на качество переходного процесса;
//+----------------------------------------------+
//--- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double ExtABuffer[],ExtBBuffer[];
//--- объявление целочисленных переменных начала отсчёта данных
int  min_rates_1,min_rates_2,min_rates_total;
//---
int XLength2,SqrXLength;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- инициализация переменных   
   XLength2=int(XLength/2);
   SqrXLength=int(MathFloor(MathSqrt(XLength)));
//--- инициализация переменных начала отсчёта данных
   min_rates_1=XMA1.GetStartBars(XMA_Method,XLength,XPhase);
   min_rates_total=min_rates_1+XMA1.GetStartBars(XMA_Method,SqrXLength,XPhase);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ExtABuffer,INDICATOR_DATA);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,ExtBBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"HullTrend");
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
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(RESET);
//--- объявления локальных переменных 
   int first,bar;
//--- объявление переменных с плавающей точкой  
   double price,xma,xma2,hma,xhma;
//--- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=0; // стартовый номер для расчёта всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//--- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      xma2=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,price,bar,false);
      xma=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price,bar,false);
      hma=2*xma2-xma;
      xhma=XMA3.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,SqrXLength,hma,bar,false);     
      ExtABuffer[bar]=hma;
      ExtBBuffer[bar]=xhma;
     } 
//---    
   return(rates_total);
  }
//+------------------------------------------------------------------+
