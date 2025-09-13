//+------------------------------------------------------------------+ 
//|                                         i-SpectrAnalysis_RVI.mq5 | 
//|                                           Copyright © 2006, klot | 
//|                                                     klot@mail.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2006, klot" 
#property link      "klot@mail.ru" 
//--- номер версии индикатора 
#property version   "1.00" 
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+ 
//| Параметры отрисовки индикатора               | 
//+----------------------------------------------+ 
//---- отрисовка индикатора 1 в виде цветного облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветjd индикатора использованы
#property indicator_color1  clrDodgerBlue,clrDarkViolet
//--- отображение метки индикатора 
#property indicator_label1  "i-SpectrAnalysis_RVI" 
//+----------------------------------------------+ 
//| Параметры отображения горизонтальных уровней | 
//+----------------------------------------------+ 
#property indicator_level1 0.0 
#property indicator_levelcolor clrGray 
#property indicator_levelstyle STYLE_DASHDOTDOT 
//+----------------------------------------------+ 
//| Объявление констант                          | 
//+----------------------------------------------+ 
#define RESET 0     // константа для возврата терминалу команды на пересчет индикатора 
//+----------------------------------------------+ 
//| Описание библиотеки dt_FFT.mqh               | 
//+----------------------------------------------+ 
#include <dt_FFT.mqh> 
//+----------------------------------------------+ 
//| Входные параметры индикатора                 | 
//+----------------------------------------------+ 
input uint RVIPeriod=14;                          // averaging period 
input uint N = 7;                                 // number Length 
input uint SS = 20;                               // smoothing factor 
input int Shift=0;                                // The shift indicator in the horizontal bars
//+----------------------------------------------+ 
//---- объявление динамических массивов, которые в дальнейшем будут использованы в качестве индикаторных буферов
double RVIBuffer[],SignBuffer[];
//--- 
int M,tnn1,ss;
//--- 
double aa[];
//--- объявление целочисленных переменных начала отсчета данных 
int min_rates_total;
//--- объявление целочисленных переменных для хендлов индикаторов 
int Ind_Handle;
//+------------------------------------------------------------------+   
//| i-SpectrAnalysis_RVI indicator initialization function           | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- инициализация переменных начала отсчета данных 
   tnn1=int(MathPow(2,N));
   M=ArrayResize(aa,tnn1+1);
   ArraySetAsSeries(aa,true);
   ss=int(MathMin(SS,M));
   min_rates_total=int(M);
//--- получение хендла индикатора iRVI 
   Ind_Handle=iRVI(Symbol(),PERIOD_CURRENT,RVIPeriod);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iRVI");
      return(INIT_FAILED);
     }
//--- превращение динамического массива в индикаторный буфер 
   SetIndexBuffer(0,RVIBuffer,INDICATOR_DATA);
//--- превращение динамического массива в индикаторный буфер 
   SetIndexBuffer(1,SignBuffer,INDICATOR_DATA);
//--- осуществление сдвига индикатора 1 по горизонтали 
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчета отрисовки индикатора 
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике 
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//--- индексация элементов в буфере как в таймсерии 
   ArraySetAsSeries(RVIBuffer,true);
   ArraySetAsSeries(SignBuffer,true);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке 
   IndicatorSetString(INDICATOR_SHORTNAME,"i-SpectrAnalysis_RVI");
//--- определение точности отображения значений индикатора 
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- завершение инициализации 
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| i-SpectrAnalysis_RVI iteration function                          | 
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
   int end=M-1;
//---- осуществление сдвига начала отсчёта отрисовки индикаторов
   int drawbegin=rates_total-end;
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,drawbegin);
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,drawbegin);
//--- делаем расчёт RVI 
   if(CopyBuffer(Ind_Handle,0,0,M,aa)<=0) return(RESET);
   fastcosinetransform(aa,tnn1,false);
   for(int kkk=0; kkk<=end && !IsStopped(); kkk++) if(kkk>=ss) aa[kkk]=0.0;
   fastcosinetransform(aa,tnn1,true);
   for(int rrr=0; rrr<=end && !IsStopped(); rrr++) RVIBuffer[rrr]=aa[rrr];
//--- делаем расчёт сигнальной линии 
   if(CopyBuffer(Ind_Handle,1,0,M,aa)<=0) return(RESET);
   fastcosinetransform(aa,tnn1,false);
   for(int kkk=0; kkk<=end && !IsStopped(); kkk++) if(kkk>=ss) aa[kkk]=0.0;
   fastcosinetransform(aa,tnn1,true);
   for(int rrr=0; rrr<=end && !IsStopped(); rrr++) SignBuffer[rrr]=aa[rrr];
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+ 
