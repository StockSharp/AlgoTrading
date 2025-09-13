//+------------------------------------------------------------------+
//|                                                    CronexMFI.mq5 |
//|                                        Copyright © 2007, Cronex. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+
#property  copyright "Copyright © 2007, Cronex"
#property  link      "http://www.metaquotes.net/"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
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
#property indicator_color1  clrDodgerBlue,clrMediumOrchid
//--- отображение метки индикатора
#property indicator_label1  "CronexMFI"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 70.0
#property indicator_level2 50.0
#property indicator_level3 30.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Описание класса CXMA                         |
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
//| объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint MFIPeriod=25;                          // Период индикатора MFI
input Smooth_Method XMA_Method=MODE_SMA;          // Метод усреднения
input uint FastPeriod=14;                         // Период быстрого усреднения
input uint SlowPeriod=25;                         // Метод медленного усреднения
input int XPhase=15;                              // Параметр сглаживания (-100..+100)
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK; // Объём 
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double ExtABuffer[],ExtBBuffer[];
//--- объявление целочисленных переменных для хранения хендлов индикаторов
int Ind_Handle;
//--- объявление целочисленных переменных начала отсчёта данных
int  min_rates_1,min_rates_2,min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- иициализация переменных начала отсчёта данных
   min_rates_1=int(MFIPeriod);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,FastPeriod,XPhase);
   min_rates_total=min_rates_2+XMA1.GetStartBars(XMA_Method,SlowPeriod,XPhase);
//--- получение хендла индикатора iMFI
   Ind_Handle=iMFI(Symbol(),PERIOD_CURRENT,MFIPeriod,VolumeType);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMFI");
      return(INIT_FAILED);
     }
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ExtABuffer,INDICATOR_DATA);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtABuffer,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,ExtBBuffer,INDICATOR_DATA);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtBBuffer,true);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"CronexMFI");
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
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  { 
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total || BarsCalculated(Ind_Handle)<rates_total) return(RESET);
//--- объявления локальных переменных 
   int to_copy,limit,bar,maxbar1,maxbar2;
//--- объявление переменных с плавающей точкой  
   double iInd[];
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(iInd,true);
//--- расчёты необходимого количества копируемых данных
//--- и стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-min_rates_1-1; // стартовый номер для расчёта всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
//---
   to_copy=limit+1;
//--- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(Ind_Handle,0,0,to_copy,iInd)<=0) return(RESET);
//---   
   maxbar1=rates_total-min_rates_1-1;
   maxbar2=rates_total-min_rates_2-1;
//--- первый цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ExtABuffer[bar]=XMA1.XMASeries(maxbar1,prev_calculated,rates_total,XMA_Method,XPhase,FastPeriod,iInd[bar],bar,true);
      ExtBBuffer[bar]=XMA2.XMASeries(maxbar2,prev_calculated,rates_total,XMA_Method,XPhase,SlowPeriod,ExtABuffer[bar],bar,true);
     } 
//---    
   return(rates_total);
  }
//+------------------------------------------------------------------+
