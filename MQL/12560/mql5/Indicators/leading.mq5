//+------------------------------------------------------------------+
//|                                                      Leading.mq5 |
//|                                                                  |
//| Leading                                                          |
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
//--- авторство индикатора
#property link      "www.mqlsoft.com"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//--- в качестве цвета бычей линии индикатора использован синий цвет
#property indicator_color1  clrBlue
//--- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//--- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//--- отображение бычей метки индикатора
#property indicator_label1  "Lead"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 2            |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//--- в качестве цвета медвежьей линии индикатора использован красный цвет
#property indicator_color2  clrRed
//--- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//--- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//--- отображение медвежьей метки индикатора
#property indicator_label2  "EMA"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input double Alpha1 = 0.25;//1 коэффициент индикатора
input double Alpha2 = 0.33;//2 коэффициент индикатора 
input int Shift=0; // сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//--- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//--- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double NetLeadBuffer[],EMABuffer[];
//+------------------------------------------------------------------+
//|  получение среднего от ценовых таймсерий                         |
//+------------------------------------------------------------------+   
double Get_Price(const double  &High[],const double  &Low[],int bar)
// Get_Price(high, low, bar)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//---
   return((High[bar]+Low[bar])/2);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- Инициализация переменных начала отсчёта данных
   min_rates_total=2;

//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,NetLeadBuffer,INDICATOR_DATA);
//--- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,EMABuffer,INDICATOR_DATA);
//--- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 2 на min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//--- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,
                     "Leading(",DoubleToString(Alpha1,4),", ",DoubleToString(Alpha2,4),", ",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчёта индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчёта индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//--- объявления локальных переменных 
   int first,bar;
   double Lead;

//--- объявления статических переменных для хранения действительных значений коэфициентов
   static double Lead_;

//--- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
    {
      first=0; // стартовый номер для расчёта всех баров
    }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//--- восстанавливаем значения переменных
   Lead=Lead_;

//--- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      //--- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==rates_total-1)
         Lead_=Lead;

      if(bar>min_rates_total)
        {
         Lead=2.0*Get_Price(high,low,bar)+(Alpha1-2.0)*Get_Price(high,low,bar-1)+(1.0-Alpha1)*Lead;
         NetLeadBuffer[bar] = Alpha2 * Lead + (1 - Alpha2) * NetLeadBuffer[bar-1];
         EMABuffer[bar]=0.5 * Get_Price(high,low,bar) + 0.5 * EMABuffer[bar-1];
        }
      else
        {
         Lead=Get_Price(high,low,bar);
         NetLeadBuffer[bar]=Lead;
         EMABuffer[bar]=Lead;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
