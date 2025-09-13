//+---------------------------------------------------------------------+
//|                                                             BnB.mq5 |
//|                                           Copyright © 2012, Zhaslan |
//|                                                                     |
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2012, Zhaslan"
//--- ссылка на сайт автора
#property link "" 
#property description "BnB"
//--- номер версии индикатора
#property version   "1.01"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrMediumOrchid,clrDodgerBlue
//--- отображение метки индикатора
#property indicator_label1  "BnB"
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//--- объявление переменных классов CXMA и CMomentum из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET 0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//|  объявление перечислений                     |
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
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_T3;           // Метод усреднения
input uint XLength=14;                            // Глубина усреднения
input int XPhase=15;                              // Параметр сглаживания
//--- XPhase: для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//--- XPhase: для VIDIA это период CMO, для AMA это период медленной скользящей
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK; // Объём
//+----------------------------------------------+
//--- объявление динамических массивов, которые будут
//--- в дальнейшем использованы в качестве индикаторных буферов
double UpBuffer[],DnBuffer[];
//--- Объявление целых переменных для хранения хендлов индикаторов
int Ind_Handle;
//--- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_total=XMA1.GetStartBars(XMA_Method,XLength,XPhase);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"BnB");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
                const double& high[],     // ценовой массив максимумов цены для расчёта индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчёта индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(RESET);
//--- объявление переменных с плавающей точкой  
   double tic,diff,bears,bulls;
//--- объявление целочисленных переменных
   int first,bar;
   long vol;
//--- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first=0; // стартовый номер для расчёта всех баров
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//--- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      if(VolumeType==VOLUME_TICK) vol=tick_volume[bar];
      else vol=volume[bar];
      if(!vol) vol=1;
      tic=(high[bar]-low[bar])/vol;
      diff=0.0;
      if(open[bar]>close[bar]) diff=((high[bar]-low[bar])-(open[bar]-close[bar]))/(2*tic);
      if(open[bar]<close[bar]) diff=((high[bar]-low[bar])-(close[bar]-open[bar]))/(2*tic);
      //---
      if(open[bar]>close[bar]) bulls=(open[bar]-close[bar])/tic+diff;
      else bulls=diff;
      //---
      if(open[bar]<close[bar]) bears=(close[bar]-open[bar])/tic+diff;
      else bears=diff;
      //---
      UpBuffer[bar]=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,bulls,bar,false);
      DnBuffer[bar]=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,bears,bar,false);
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
