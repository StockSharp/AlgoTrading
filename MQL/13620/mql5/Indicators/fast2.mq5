//+---------------------------------------------------------------------+
//|                                                           Fast2.mq5 | 
//|                                             Copyright © 2008, xrust | 
//|                                                                     | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2008, xrust"
#property link ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- для расчета и отрисовки индикатора использовано три буфера
#property indicator_buffers 3
//---- использовано всего три графических построения
#property indicator_plots   3
//+-----------------------------------+
//| Параметры отрисовки индикатора 1  |
//+-----------------------------------+
//---- отрисовка индикатора в виде гистограммы
#property indicator_type1 DRAW_HISTOGRAM
//---- в качестве цветов гистограммы использованы
#property indicator_color1 clrBlueViolet
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "Fast2 HISTOGRAM"
//+-----------------------------------+
//| Параметры отрисовки индикатора 2  |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type2 DRAW_LINE
//---- в качестве цвета линии использован
#property indicator_color2 clrTeal
//---- линия индикатора - сплошная кривая
#property indicator_style2 STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width2 1
//---- отображение метки сигнальной линии
#property indicator_label2  "Fast Signal"
//+-----------------------------------+
//| Параметры отрисовки индикатора 3  |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type3 DRAW_LINE
//---- в качестве цвета линии использован
#property indicator_color3 clrRed
//---- линия индикатора - сплошная кривая
#property indicator_style3 STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width3 1
//---- отображение метки сигнальной линии
#property indicator_label3  "Slow Signal"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
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
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_LWMA; // Метод усреднения первого сглаживания
input uint Length1=3; // Глубина  первого сглаживания
input int  Phase1=15; // Параметр первого сглаживания
                      // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                      // для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method2=MODE_LWMA; // Метод усреднения второго сглаживания
input uint Length2=9; // Глубина  второго сглаживания
input int  Phase2=15; // Параметр второго сглаживания
                      // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                      // для VIDIA это период CMO, для AMA это период медленной скользящей
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double HistBuffer[],Sign1Buffer[],Sign2Buffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   int min_rates_1=XMA1.GetStartBars(MA_Method1,Length1,Phase1);
   int min_rates_2=XMA2.GetStartBars(MA_Method2,Length2,Phase2);
   min_rates_total=min_rates_1+min_rates_2+2;
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Length1",Length1);
   XMA2.XMALengthCheck("Length2",Length2);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("Phase1",Phase1,MA_Method1);
   XMA2.XMAPhaseCheck("Phase2",Phase2,MA_Method2);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,HistBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,Sign1Buffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,Sign2Buffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(MA_Method1);
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   StringConcatenate(shortname,"Fast2(",Length1,", ",Length2,", ",Smooth1,", ",Smooth2,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
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
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=2; // стартовый номер для расчета всех баров
     }
   else
     {
      first=prev_calculated-1; // стартовый номер для расчета новых баров
     }
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      HistBuffer[bar]=(close[bar]-open[bar]+((close[bar-1]-open[bar-1])/MathSqrt(2))+((close[bar-2]-open[bar-2])/MathSqrt(3)))/_Point;
      //---- два вызова функции XMASeries. 
      Sign1Buffer[bar]=XMA1.XMASeries(2,prev_calculated,rates_total,MA_Method1,Phase1,Length1,HistBuffer[bar],bar,false);
      Sign2Buffer[bar]=XMA2.XMASeries(2,prev_calculated,rates_total,MA_Method2,Phase2,Length2,HistBuffer[bar],bar,false);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
