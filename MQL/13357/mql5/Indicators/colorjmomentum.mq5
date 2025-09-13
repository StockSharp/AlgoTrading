//+---------------------------------------------------------------------+
//|                                                  ColorJMomentum.mq5 | 
//|                                Copyright © 2010,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.10"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 4
#property indicator_buffers 4 
//---- использовано всего четыре графических построения
#property indicator_plots   4
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован серый цвет
#property indicator_color1 clrGray
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "JMomentum"
//+----------------------------------------------+
//| Параметры отрисовки бычьего индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован салатовый цвет
#property indicator_color2 clrSpringGreen
//---- толщина линии индикатора равна 3
#property indicator_width2 3
//---- отображение бычьей метки индикатора
#property indicator_label2 "Up_Signal"
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//---- отрисовка индикатора в виде символа
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован темно-розовый цвет
#property indicator_color3  clrDeepPink
//---- толщина линии индикатора равна 3
#property indicator_width3 3
//---- отображение медвежьей метки индикатора
#property indicator_label3 "Dn_Signal"
//+----------------------------------------------+
//| Параметры отрисовки безтрендового индикатора |
//+----------------------------------------------+
//---- отрисовка индикатора в виде символа
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета безтрендового индикатора использован серый
#property indicator_color4  clrGray
//---- толщина линии индикатора равна 3
#property indicator_width4 3
//---- отображение безтрендовой метки индикатора
#property indicator_label4 "No_Signal"
//+----------------------------------------------+
//| Объявление перечислений                      |
//+----------------------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price 
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int MLength=8;  // Период индикатора Momentum 
input int JMLength=8; // Глубина JMA сглаживания индикатора Momentum                  
input int JPhase=100; // Параметр JMA сглаживания
                      // изменяющийся в пределах -100 ... +100,
                      // влияет на качество переходного процесса
input Applied_price_ IPC=PRICE_CLOSE; // Ценовая константа
/* , по которой производится расчет индикатора ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- индикаторные буферы
double JMomentum[];
double UpBuffer[];
double DnBuffer[];
double FlBuffer[];
//----
int start;
//+------------------------------------------------------------------+
//| Описание функции iPriceSeries                                    |
//| Описание функции iPriceSeriesAlert                               |
//| Описание класса CMomentum                                        |
//+------------------------------------------------------------------+
#include <SmoothAlgorithms.mqh>  
//+------------------------------------------------------------------+   
//| JMomentum indicator initialization function                      | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- 
   start=MLength+31;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,JMomentum,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,MLength);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"JMomentum");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,UpBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,start);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Up Signal");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- выбор символа для отрисовки
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,DnBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,start);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"Dn Signal");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- выбор символа для отрисовки
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,FlBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,start);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"No Signal");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- выбор символа для отрисовки
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"JMomentum( MLength = ",MLength,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- объявление переменной класса CMomentum из файла SmoothAlgorithms.mqh
   CMomentum Mom;
//---- объявление переменной класса CJJMA из файла SmoothAlgorithms.mqh
   CJJMA JMA;
//---- установка алертов на недопустимые значения внешних переменных
   Mom.MALengthCheck("MLength",MLength);
//---- установка алертов на недопустимые значения внешних переменных
   JMA.JJMALengthCheck("JMLength",JMLength);
//---- установка алертов на недопустимые значения внешних переменных
   JMA.JJMAPhaseCheck("JPhase",JPhase);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| JMomentum iteration function                                     | 
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
   if(rates_total<start) return(0);
//---- объявление переменных с плавающей точкой  
   double price,momentum,jmomentum,dmomentum;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated==0) // проверка на первый старт расчета индикатора
      first=0; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- объявление переменных классов CMomentum и CJJMA из файла SmoothAlgorithms.mqh
   static CMomentum Mom;
   static CJJMA JMA;
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      //---- вызов функции PriceSeries для получения приращения входной цены dprice_
      price=PriceSeries(IPC,bar,open,low,high,close);
      //---- два вызова функции MomentumSeries
      momentum=Mom.MomentumSeries(0,prev_calculated,rates_total,MLength,price,bar,false);
      //---- один вызов функции JJMASeries. 
      //---- Параметры Phase и MLength не меняются на каждом баре (Din = 0) 
      jmomentum=JMA.JJMASeries(MLength+1,prev_calculated,rates_total,0,JPhase,JMLength,momentum,bar,false);
      //---- загрузка полученного значения в индикаторный буфер
      JMomentum[bar]=jmomentum/_Point;
      //---- инициализация ячеек индикаторных буферов нулями
      UpBuffer[bar] = EMPTY_VALUE;
      DnBuffer[bar] = EMPTY_VALUE;
      FlBuffer[bar] = EMPTY_VALUE;
      //----
      if(bar<start) continue;
      //---- инициализация ячеек индикаторных буферов полученными значениями 
      dmomentum=NormalizeDouble(JMomentum[bar]-JMomentum[bar-1],0);
      if(dmomentum>0) UpBuffer[bar] = JMomentum[bar]; //есть восходящий тренд
      if(dmomentum<0) DnBuffer[bar] = JMomentum[bar]; //есть нисходящий тренд
      if(dmomentum==0) FlBuffer[bar]= JMomentum[bar]; //нет тренда
     }
//----     
   return(rates_total);
  }
//+X----------------------+ <<< The End >>> +-----------------------X+
