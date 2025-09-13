//+------------------------------------------------------------------+
//|                                         i-SpectrAnalysis_WPR.mq5 |
//|                                           Copyright © 2006, klot |
//|                                                     klot@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, klot"
#property link      "klot@mail.ru"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- количество индикаторных буферов
#property indicator_buffers 1 
//--- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//--- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//--- в качестве цвета линии индикатора использован DarkOrchid цвет
#property indicator_color1 clrDarkOrchid
//--- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width1  2
//--- отображение метки индикатора
#property indicator_label1  "i-SpectrAnalysis_WPR"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 -20.0
#property indicator_level2 -50.0
#property indicator_level3 -80.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET 0                // Константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Описание библиотеки dt_FFT.mqh               |
//+----------------------------------------------+
#include <dt_FFT.mqh> 
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint WPRPeriod=14;
input uint N = 7;   // Длина ряда
input uint SS = 20; // Коэффициент сглаживания
input int Shift=0;  // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//--- объявление динамического массива, который в дальнейшем
//--- будет использован в качестве индикаторного буфера
double IndBuffer[];
//---
int M,tnn1,ss;
//---
double aa[];
//--- объявление целочисленных переменных начала отсчёта данных
int min_rates_total;
//--- объявление целочисленных переменных для хендлов индикаторов
int Ind_Handle;
//+------------------------------------------------------------------+   
//| i-SpecktrAnalis_WPR indicator initialization function            | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   tnn1=int(MathPow(2,N));
   M=ArrayResize(aa,tnn1+1);
   ArraySetAsSeries(aa,true);
   ss=int(MathMin(SS,M));
   min_rates_total=int(M+WPRPeriod);
//--- получение хендла индикатора iWPR
   Ind_Handle=iWPR(Symbol(),PERIOD_CURRENT,WPRPeriod);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iWPR");
      return(INIT_FAILED);
     }
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//--- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"i-SpecktrAnalis_WPR");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| i-SpecktrAnalis_WPR iteration function                           | 
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
   if(rates_total<min_rates_total || BarsCalculated(Ind_Handle)<rates_total) return(RESET);
//---
   for(int bar=rates_total-1; bar>=prev_calculated && !IsStopped(); bar--) IndBuffer[bar]=0.0;
//--- копируем вновь появившиеся данные в массив
   if(CopyBuffer(Ind_Handle,0,0,M,aa)<=0) return(RESET);
//---
   int end=M-1;
   fastcosinetransform(aa,tnn1,false);
   for(int kkk=0; kkk<=end && !IsStopped(); kkk++) if(kkk>=ss) aa[kkk]=0.0;
   fastcosinetransform(aa,tnn1,true);
   for(int rrr=0; rrr<=end && !IsStopped(); rrr++) IndBuffer[rrr]=aa[rrr];
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
