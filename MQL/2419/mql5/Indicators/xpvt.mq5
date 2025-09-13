//+------------------------------------------------------------------+
//|                                                         XPVT.mq5 |
//|                                     Copyright © 2010, Martingeil | 
//+------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2010, Martingeil"
//--- ссылка на сайт автора
#property link ""
#property description "Price and Volume Trend"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора 1             |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//--- в качестве цвета линии индикатора использован Red цвет
#property indicator_color1  Red
//--- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//--- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//--- отображение бычей лэйбы индикатора
#property indicator_label1  "PVT"
//+----------------------------------------------+
//| Параметры отрисовки индикатора 2             |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//--- в качестве цвета линии индикатора использован синий цвет
#property indicator_color2  Blue
//--- линия индикатора 2 - штрихпункутир
#property indicator_style2  STYLE_DASHDOTDOT
//--- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//--- отображение бычей лэйбы индикатора
#property indicator_label2  "Signal PVT"
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA;
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
   PRICE_TRENDFOLLOW1_   //TrendFollow_2 Price 
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK; // Объём
input Smooth_Method XMA_Method=MODE_EMA;          // Метод усреднения
input int XLength=5;                              // Глубина сглаживания
input int XPhase=15;                              // Параметр сглаживания
input Applied_price_ IPC=PRICE_CLOSE;             // Ценовая константа
input int Shift=0;                                // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double PVTBuffer[],SignBuffer[];
//--- объявление целочисленных переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_total=XMA.GetStartBars(XMA_Method,XLength,XPhase)+1;
//--- превращение динамического массива SignBuffer в индикаторный буфер
   SetIndexBuffer(0,PVTBuffer,INDICATOR_DATA);
//--- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 1 на 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- превращение динамического массива PVTBuffer в индикаторный буфер
   SetIndexBuffer(1,SignBuffer,INDICATOR_DATA);
//--- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 2 на 1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"Price and Volume Trend");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчёта индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчёта индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);
//--- объявления локальных переменных 
   double dCurrentPrice,dPreviousPrice;
   int first,bar;
   long Vol;
//--- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=1; // стартовый номер для расчёта всех баров
      if(VolumeType==VOLUME_TICK) PVTBuffer[0]=double(tick_volume[0]);
      else  PVTBuffer[0]=double(volume[0]);
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//--- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      if(VolumeType==VOLUME_TICK) Vol=long(tick_volume[bar]);
      else Vol=long(volume[bar]);
      //--- вызов функции PriceSeries для получения входной цены price_
      dCurrentPrice=PriceSeries(IPC,bar,open,low,high,close);
      dPreviousPrice=PriceSeries(IPC,bar-1,open,low,high,close);
      //---
      PVTBuffer[bar]=PVTBuffer[bar-1]+Vol*(dCurrentPrice-dPreviousPrice)/dPreviousPrice;
      SignBuffer[bar]=XMA.XMASeries(1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,PVTBuffer[bar],bar,false);
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
