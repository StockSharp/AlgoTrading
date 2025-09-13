//+------------------------------------------------------------------+
//|                                                     Extrem_N.mq5 |
//|                                 Copyright © 2014 Serkov Alexandr | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2014 Serkov Alexandr"
//---- ссылка на сайт автора
#property link "serkov-alexandr@mail.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window
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
#property indicator_color1  clrLime
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 3
#property indicator_width1  3
//---- отображение бычьей метки индикатора
#property indicator_label1  "Bulls"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета медвежьей линии индикатора использован красный цвет
#property indicator_color2  clrRed
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 3
#property indicator_width2  3
//---- отображение медвежьей метки индикатора
#property indicator_label2  "Bears"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int period= 9; // Период индикатора 
input int Shift = 0; // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double BullsBuffer[];
double BearsBuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- превращение динамического массива BullsBuffer в индикаторный буфер
   SetIndexBuffer(0,BullsBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на period
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,period);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Bears");
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- превращение динамического массива BearsBuffer в индикаторный буфер
   SetIndexBuffer(1,BearsBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2 на period
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,period);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Bulls");
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"(",period,", ",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   if(rates_total<period-1) return(0);
//---- объявления локальных переменных 
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=period; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      if(high[bar]>high[bar-period]) BullsBuffer[bar] = high[bar]; else BullsBuffer[bar] = 0.0;
      if(low[bar]<low[bar-period]) BearsBuffer[bar] = low[bar]; else BearsBuffer[bar] = 0.0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
