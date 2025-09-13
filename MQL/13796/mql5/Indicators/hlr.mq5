//+------------------------------------------------------------------+
//|                                                          HLR.mq4 |
//|                                      Copyright © 2007, Alexandre |
//|                      http://www.kroufr.ru/content/view/1184/124/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, Alexandre"
#property link      "http://www.kroufr.ru/content/view/1184/124/"
#property description "Hi-Lo Range Oscillator"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов
#property indicator_buffers 1 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован красный цвет
#property indicator_color1 clrRed
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "Hi-Lo Range Oscillator"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 80
#property indicator_level2 50
#property indicator_level3 20
#property indicator_levelcolor clrLimeGreen
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint HLR_Range=40; // Период усреднения индикатора
input int  Shift=0;      // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамического массива, который будет в 
//---- дальнейшем использован в качестве индикаторного буфера
double HLRBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+   
//| HLR indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(HLR_Range+1);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,HLRBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(HLRBuffer,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Hi-Lo Range Oscillator(",HLR_Range,", ",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| HLR iteration function                                           | 
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
   if(rates_total<min_rates_total) return(RESET);
//---- объявление переменных с плавающей точкой  
   double m_pr,HH,LL, HL;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int limit,bar;
//---- расчеты необходимого количества копируемых данных и
//---- стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- пересчет значений индикаторов
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      HH=high[ArrayMaximum(high,bar,HLR_Range)];
      LL=low [ArrayMinimum(low, bar,HLR_Range)];    
      m_pr=(high[bar]+low[bar])/2.0;
      HL=HH-LL;
      if(HL) HLRBuffer[bar]=100.0*(m_pr-LL)/(HL);
      else HLRBuffer[bar]=0.0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
