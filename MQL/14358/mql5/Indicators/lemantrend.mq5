//+------------------------------------------------------------------+
//|                                                   LeManTrend.mq5 |
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
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки бычьего индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета бычьей линии индикатора использован зеленый цвет
#property indicator_color1  Lime
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//---- отображение бычьей метки индикатора
#property indicator_label1  "LeManTrend Bulls"
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета медвежьей линии индикатора использован красный цвет
#property indicator_color2  Red
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//---- отображение медвежьей метки индикатора
#property indicator_label2  "LeManTrend Bears"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int Min       = 13;
input int Midle     = 21;
input int Max       = 34;
input int PeriodEMA = 3; // Период индикатора
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double BullsBuffer[];
double BearsBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,start;
//+------------------------------------------------------------------+
//| Описание класса CMoving_Average                                  |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   start=MathMax(MathMax(Min,Midle),Max);
   min_rates_total=start+PeriodEMA;
//---- превращение динамического массива BullsBuffer в индикаторный буфер
   SetIndexBuffer(0,BullsBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BullsBuffer,true);
//---- превращение динамического массива BearsBuffer в индикаторный буфер
   SetIndexBuffer(1,BearsBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2 на min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BearsBuffer,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"LeManTrend(",Min,", ",Midle,", ",Max,", ",PeriodEMA,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   if(rates_total<min_rates_total) return(0);
//---- объявления локальных переменных 
   int limit,bar,maxbar;
   double High1,High2,High3,Low1,Low2,Low3,HH,LL;
//---- расчет стартового номера maxbar для функции MASeries()
   maxbar=rates_total-1-start;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit=maxbar; // стартовый номер для расчета всех баров
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- объявление переменных класса CMoving_Average из файла SmoothAlgorithms.mqh
   static CMoving_Average BULLS,BEARS;
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
      BullsBuffer[bar]=BULLS.MASeries(maxbar,prev_calculated,rates_total,PeriodEMA,MODE_EMA,HH,bar,true);
      BearsBuffer[bar]=BEARS.MASeries(maxbar,prev_calculated,rates_total,PeriodEMA,MODE_EMA,LL,bar,true);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
