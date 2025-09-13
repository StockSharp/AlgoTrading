//+------------------------------------------------------------------+ 
//|                                           DiNapoliStochastic.mq5 | 
//|                                      Copyright © 2010, LenIFCHIK |
//|                                                                  |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2010, LenIFCHIK"
#property link      ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора Stochastic    |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета основной линии индикатора использован цвет DarkOrange
#property indicator_color1  clrDarkOrange
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//---- отображение метки линии индикатора
#property indicator_label1  "Stochastic"
//+----------------------------------------------+
//| Параметры отрисовки индикатора Signal        |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета сигнальной линии индикатора использован цвет BlueViolet
#property indicator_color2  clrBlueViolet
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//---- отображение метки линии индикатора
#property indicator_label2  "Signal"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level3 70.0
#property indicator_level2 50.0
#property indicator_level1 30.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET 0       // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint FastK=8;    // Быстрый период %K
input uint SlowK=3;    // Медленный период %K
input uint SlowD=3;    // Медленный период %D
input int Shift=0;     // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double StoBuffer[];
double SigBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(FastK);
//---- превращение динамического массива StoBuffer[] в индикаторный буфер
   SetIndexBuffer(0,StoBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(StoBuffer,true);
//---- превращение динамического массива SignalBuffer[] в индикаторный буфер
   SetIndexBuffer(1,SigBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2 на min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SigBuffer,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"DiNapoliStochastic(",FastK,", ",SlowK,", ",SlowD,", ",Shift,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//----
   return(0);
//----
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
//---- объявление переменных с плавающей точкой  
   double HH,LL,Range,Res;
//---- объявление целочисленных переменных
   int limit;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
      StoBuffer[limit+1]=50.0;
      SigBuffer[limit+1]=50.0;
     }
   else limit=rates_total-prev_calculated;  // стартовый номер для расчета только новых баров
//---- индексация элементов в массивах как в таймсериях
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- основной цикл расчета индикатора
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      HH=high[ArrayMaximum(high,bar,FastK)];
      LL=low [ArrayMinimum(low, bar,FastK)];
      Range=MathMax(HH-LL,1*_Point);
      Res=100*(close[bar]-LL)/Range;
      StoBuffer[bar]=StoBuffer[bar+1]+(Res-StoBuffer[bar+1])/SlowK;            //расчет стохастической линии
      SigBuffer[bar]=SigBuffer[bar+1]+(StoBuffer[bar]-SigBuffer[bar+1])/SlowD; //расчет сигнальной линии
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
