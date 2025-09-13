//+------------------------------------------------------------------+
//|                             Modified_Optimum_Elliptic_Filter.mq5 |
//|                                                                  |
//| Modified Optimum Elliptic Filter                                 |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Coded by Witold Wozniak"
//--- ссылка на сайт автора
#property link      "www.mqlsoft.com"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в основном окне
#property indicator_chart_window
//--- для расчета и отрисовки индикатора использован один буфер
#property indicator_buffers 1
//--- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//--- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//--- в качестве цвета линии индикатора использован розовый цвет
#property indicator_color1  Magenta
//--- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width1  2
//--- отображение метки индикатора
#property indicator_label1  "Modified Optimum Elliptic Filter"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int Shift=0; // Сдвиг мувинга по горизонтали в барах 
//+----------------------------------------------+
//--- объявление динамического массива, который в дальнейшем
//--- будет использован в качестве индикаторного буфера
double ExtLineBuffer[];
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//--- объявление глобальных переменных
double coef1,coef2,coef3,coef4;
//+------------------------------------------------------------------+
//| Получение среднего от ценовых таймсерий                          |
//+------------------------------------------------------------------+   
double Get_Price(const double  &High[],const double  &Low[],int bar)
  {
//---
   return((High[bar]+Low[bar])/2);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчета данных
   min_rates_total=4;
//--- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//--- осуществление сдвига Фатла по горизонтали на FATLShift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Modified Optimum Elliptic Filter(",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---
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
//--- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);
//--- объявления локальных переменных 
   int first,bar;
//--- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=0;                   // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//--- основной цикл расчета индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      //--- формула для вычисления фильтра
      if(bar>min_rates_total) ExtLineBuffer[bar]=
         0.13785*(2*Get_Price(high,low,bar)-Get_Price(high,low,bar-1))
         +0.0007*(2*Get_Price(high,low,bar-1)-Get_Price(high,low,bar-2))
         + 0.13785*(2*Get_Price(high,low,bar-2) - Get_Price(high,low,bar-3))
         + 1.2103 *ExtLineBuffer[bar-1] - 0.4867*ExtLineBuffer[bar-2];
      else ExtLineBuffer[bar]=Get_Price(high,low,bar);
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
