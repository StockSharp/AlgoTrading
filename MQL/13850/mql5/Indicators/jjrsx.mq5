//+---------------------------------------------------------------------+ 
//|                                                           JJRSX.mq5 | 
//|                                Copyright © 2010,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| Для работы индикатора файл SmoothAlgorithms.mqh следует положить    |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+ 
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов
#property indicator_buffers 1 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован фиолетово-синий цвет
#property indicator_color1 clrBlueViolet
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "JJRSX"
//---- параметры горизонтальных уровней индикатора
#property indicator_level1  0.5
#property indicator_level2 -0.5
#property indicator_level3  0.0
#property indicator_levelcolor clrMagenta
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
enum Applied_price_      // тип константы
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price 
  };
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint JLength=8;   // Глубина  сглаживания
input uint Smooth = 8;  // Глубина JJMA усреднения
input int JPhase = 100; // Параметр JJMA усреднения
                        // изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
input Applied_price_ IPC=PRICE_CLOSE_; // Ценовая константа
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- индикаторные буферы
double JJRSX[];
//+------------------------------------------------------------------+
//| Описание функции iPriceSeries                                    |
//| Описание класса CJurX                                            |
//| Описание класса CJJMA                                            |
//+------------------------------------------------------------------+  
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| JJRSX indicator initialization function                          | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,JJRSX,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,32);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"JJRSX( Length = ",JLength,
                     ", Smooth = ",Smooth,", Phase = ",JPhase,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- объявление переменной класса CJJMA из файла JJMASeries_Cls.mqh
   CJJMA JMA;
//---- установка алертов на недопустимые значения внешних переменных
   JMA.JJMALengthCheck("Length",JLength);
   JMA.JJMALengthCheck("Smooth",Smooth);
   JMA.JJMAPhaseCheck ("Phase",JPhase);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| JJRSX iteration function                                         | 
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
   if(rates_total<32) return(0);
//---- объявление переменных с плавающей точкой  
   double dprice,udprice,up_jrsx,dn_jrsx,jrsx;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=1; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- объявление переменных класса JurX из файла JurXSeries_Cls.mqh
   static CJurX Jur1,Jur2;
//---- объявление переменной класса CJJMA из файла JJMASeries_Cls.mqh
   static CJJMA JMA;
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- вызов функции PriceSeries для получения приращения входной цены dprice
      dprice=PriceSeries(IPC,bar,open,low,high,close)-PriceSeries(IPC,bar-1,open,low,high,close);
      //----
      udprice=MathAbs(dprice);
      //---- два вызова функции JurXSeries
      up_jrsx = Jur1.JurXSeries(1,prev_calculated,rates_total,0,JLength,dprice,bar,false);
      dn_jrsx = Jur2.JurXSeries(1,prev_calculated,rates_total,0,JLength,udprice,bar,false);
      //---- предотвращение деления на ноль на пустых значениях
      if(!dn_jrsx) jrsx=EMPTY_VALUE;
      else
        {
         jrsx=up_jrsx/dn_jrsx;
         //---- ограничение индикатора сверху и снизу
         jrsx=MathMax(MathMin(jrsx,+1),-1);
        }
      //---- один вызов функции JJMASeries
      JJRSX[bar]=JMA.JJMASeries(1,prev_calculated,rates_total,0,JPhase,Smooth,jrsx,bar,false);
     }
//----
   return(rates_total);
  }
//+------------------------------------------------------------------+
