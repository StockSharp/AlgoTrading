//+---------------------------------------------------------------------+ 
//|                                                            JCCX.mq5 | 
//|                                Copyright © 2010,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| Для работы индикатора файл SmoothAlgorithms.mqh следует положить    |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+ 
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.02"
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
//---- в качестве цвета линии индикатора использован голубой цвет
#property indicator_color1 clrDodgerBlue
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "JCCX"
//---- параметры горизонтальных уровней индикатора
#property indicator_level1  0.5
#property indicator_level2 -0.5
#property indicator_level3  0.0
#property indicator_levelcolor clrDeepPink
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Объявление перечислений                      |
//+----------------------------------------------+
enum Applied_price_ //тип константы
  {
   PRICE_CLOSE_ = 1,     //PRICE_CLOSE
   PRICE_OPEN_,          //PRICE_OPEN
   PRICE_HIGH_,          //PRICE_HIGH
   PRICE_LOW_,           //PRICE_LOW
   PRICE_MEDIAN_,        //PRICE_MEDIAN
   PRICE_TYPICAL_,       //PRICE_TYPICAL
   PRICE_WEIGHTED_,      //PRICE_WEIGHTED
   PRICE_SIMPL_,         //PRICE_SIMPL_
   PRICE_QUARTER_,       //PRICE_QUARTER_
   PRICE_TRENDFOLLOW0_,  //PRICE_TRENDFOLLOW0_
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint JMALength=8; // Глубина  JJMA сглаживания входной цены
input int JMAPhase=100; // Параметр JJMA усреднения
                        // изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
input uint JurXLength=8; // Глубина JurX усреднения индикатора
input Applied_price_ IPC=PRICE_CLOSE_; // Ценовая константа
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- индикаторные буферы
double JCCX[];
//+------------------------------------------------------------------+
//| Описание классов CJJMA, CJurX и функции PriceSeries()            |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| JCCX indicator initialization function                           | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,JCCX,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,30);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"JCCX");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"JCCX( JMALength = ",JMALength,", JMAPhase = ",JMAPhase,", JurXLength = ",JurXLength,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- объявление переменной класса CJMA из файла SmoothAlgorithms.mqh
   CJJMA JMA;
//---- установка алертов на недопустимые значения внешних переменных
   JMA.JJMALengthCheck("JMALength",  JMALength );
   JMA.JJMALengthCheck("JurXLength", JurXLength);
   JMA.JJMAPhaseCheck ("JMAPhase",   JMAPhase  );
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| JCCX iteration function                                          | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,// количество истории в барах на текущем тике
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
   if(rates_total<0) return(0);
//---- объявление переменных с плавающей точкой  
   double price_,jma,up_cci,dn_cci,up_jccx,dn_jccx,jccx;
//---- объявление целочисленных переменных
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
        first=0; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- объявление переменных класса CJurX из файла SmoothAlgorithms.mqh
   static CJurX Jur1,Jur2;
//---- объявление переменной класса CJMA из файла SmoothAlgorithms.mqh
   static CJJMA JMA;
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- вызов функции PriceSeries для получения входной цены price_
      price_=PriceSeries(IPC,bar,open,low,high,close);
      //---- один вызов функции JJMASeries для получения мувинга JMA
      jma=JMA.JJMASeries(0,prev_calculated,rates_total,0,JMAPhase,JMALength,price_,bar,false);
      //---- определяем отклонение цены от значения мувинга
      up_cci = price_ - jma;
      dn_cci = MathAbs(up_cci);
      //---- два вызова функции JurXSeries
      up_jccx = Jur1.JurXSeries(30, prev_calculated, rates_total, 0, JurXLength, up_cci, bar, false);
      dn_jccx = Jur2.JurXSeries(30, prev_calculated, rates_total, 0, JurXLength, dn_cci, bar, false);
      //---- предотвращение деления на ноль на пустых значениях
      if(dn_jccx==0) jccx=EMPTY_VALUE;
      else
        {
         jccx=up_jccx/dn_jccx;
         //---- ограничение индикатора сверху и снизу 
         if(jccx > +1)jccx = +1;
         if(jccx < -1)jccx = -1;
        }
      //---- загрузка полученного значения в индикаторный буфер
      JCCX[bar]=jccx;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
