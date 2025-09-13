//+------------------------------------------------------------------+
//|                                               LeManTrendHist.mq5 |
//|                                         Copyright © 2009, LeMan. | 
//|                                                 b-market@mail.ru | 
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2009, LeMan."
//---- ссылка на сайт автора
#property link "b-market@mail.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//--- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET 0                        // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора в виде четырехцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов четырехцветной гистограммы использованы
#property indicator_color1 clrMagenta,clrPurple,clrGray,clrOliveDrab,clrLime
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "LeManTrend"
//+----------------------------------------------+
//| Описание класса CMoving_Average              |
//+----------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//| Объявление перечислений                      |
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
input int Min       = 13;
input int Midle     = 21;
input int Max       = 34;
input int PeriodEMA = 3;               // Период индикатора
input Smooth_Method XMethod=MODE_JJMA; // Метод сглаживания  индикатора
input int XLength=5;                   // Глубина сглаживания индикатора
input int XPhase=100;                  // Параметр сглаживания индикатора
input int Shift=0;                     // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_1,min_rates_2;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_1=MathMax(MathMax(Min,Midle),Max)+1;
   min_rates_2=GetStartBars(XMethod,XLength,XPhase);
   min_rates_total=min_rates_1+min_rates_2+1;
//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorIndBuffer,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"LeManTrend(",Min,", ",Midle,", ",Max,", ",PeriodEMA,")");
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
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(RESET);
//---- объявление локальных переменных 
   int limit,bar,maxbar1,maxbar2,clr;
   double High1,High2,High3,Low1,Low2,Low3,HH,LL,Bulls,Bears,Range,XRange;
//---- расчет стартового номера
   maxbar1=rates_total-min_rates_1-1;
   maxbar2=maxbar1-min_rates_2;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=maxbar1; // стартовый номер для расчета всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- объявление переменных класса CMoving_Average из файла SmoothAlgorithms.mqh
   static CMoving_Average BULLS,BEARS;
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
   static CXMA SMOOTH;
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      High1=high[ArrayMaximum(high,bar+1,Min)];
      High2=high[ArrayMaximum(high,bar+1,Midle)];
      High3=high[ArrayMaximum(high,bar+1,Max)];
      HH=((high[bar]-High1)+(high[bar]-High2)+(high[bar]-High3));
      //----
      Low1=low[ArrayMinimum(low,bar+1,Min)];
      Low2=low[ArrayMinimum(low,bar+1,Midle)];
      Low3=low[ArrayMinimum(low,bar+1,Max)];
      LL=((Low1-low[bar])+(Low2-low[bar])+(Low3-low[bar]));
      //----
      Bulls=BULLS.MASeries(maxbar1,prev_calculated,rates_total,PeriodEMA,MODE_EMA,HH,bar,true);
      Bears=BEARS.MASeries(maxbar1,prev_calculated,rates_total,PeriodEMA,MODE_EMA,LL,bar,true);
      Range=Bulls-Bears;
      XRange=SMOOTH.XMASeries(maxbar1,prev_calculated,rates_total,XMethod,XPhase,XLength,Range,bar,true);
      IndBuffer[bar]=XRange;
      clr=2;
      if(XRange>0)
        {
         if(XRange>IndBuffer[bar+1]) clr=4;
         if(XRange<IndBuffer[bar+1]) clr=3;
        }
      //----
      if(XRange<0)
        {
         if(XRange<IndBuffer[bar+1]) clr=0;
         if(XRange>IndBuffer[bar+1]) clr=1;
        }
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
