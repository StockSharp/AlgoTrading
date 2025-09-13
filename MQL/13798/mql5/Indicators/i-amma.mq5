//+------------------------------------------------------------------+ 
//|                                                       i-AMMA.mq5 | 
//|                                          Copyright © 2007, RickD |
//|                                                   www.e2e-fx.net |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2007, RickD"
//---- ссылка на сайт автора
#property link      "www.e2e-fx.net"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов
#property indicator_buffers 1 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован Orange цвет
#property indicator_color1 clrOrange
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "i-AMMA"
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint MA_Period=25; // Глубина сглаживания
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // Сдвиг индикатора по вертикали в пунктах
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[];
//---- 
double dPriceShift;
//---- объявление целочисленных переменных начала отсчета данных
int  min_rates_total;
//+------------------------------------------------------------------+    
//| i-AMMA indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=2;
//---- инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"i-AMMA(",MA_Period,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| i-AMMA iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const int begin,          // номер начала достоверного отсчета баров
                const double &price[])    // ценовой массив для расчета индикатора
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total+begin) return(0);
//---- объявление локальных переменных
   int first,bar;
   double AMMA;
//----
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      first=min_rates_total+begin; // стартовый номер для расчета всех баров
      IndBuffer[first-1]=price[first-1];
      //---- осуществление сдвига начала отсчета отрисовки индикатора
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      AMMA=((MA_Period-1)*(IndBuffer[bar-1]-dPriceShift)+price[bar])/MA_Period;
      IndBuffer[bar]=AMMA+dPriceShift;
     }
//----
   return(rates_total);
  }
//+------------------------------------------------------------------+
