//+---------------------------------------------------------------------+
//|                                                     ColorXdinMA.mq5 | 
//|                                          Copyright © 2011,   dimeon | 
//|                                                                     | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, dimeon"
#property link ""
//---- номер версии индикатора
#property version   "1.03"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде многоцветной линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цветов трехцветной линии использованы
#property indicator_color1  clrYellow,clrBlue,clrRed
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "XdinMA"
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
enum Applied_price_ //тип константы
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
input Smooth_Method MA_Method1=MODE_SMA; // Метод усреднения
input int Length_main=10; // Глубина main усреднения
input int Length_plus=20; // Глубина plus усреднения                  
input int PhaseX=15;      // Параметр усреднения
                          // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                          // для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE; // Ценовая константа
input int Shift=0;      // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // Сдвиг индикатора по вертикали в пунктах
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double XdinMA[];
double ColorXdinMA[];
//---- объявление переменной значения вертикального сдвига скользящей средней
double dPriceShift;
//---- объявление целочисленных переменных начала отсчета данных
int  min_rates_total;
//+------------------------------------------------------------------+   
//| XdinMA indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   int StartBars1=XMA1.GetStartBars(MA_Method1, Length_main, PhaseX);
   int StartBars2=XMA2.GetStartBars(MA_Method1, Length_plus, PhaseX);
   min_rates_total=MathMax(StartBars1,StartBars2)+1;
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Length_main", Length_main);
   XMA2.XMALengthCheck("Length_plus", Length_plus);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("PhaseX",PhaseX,MA_Method1);
//---- инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,XdinMA,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorXdinMA,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(MA_Method1);
   StringConcatenate(shortname,"XdinMA(",Length_main,", ",Length_plus,", ",Smooth,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| XdinMA iteration function                                        | 
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
//---- объявление переменных с плавающей точкой  
   double price_,ma_main,ma_plus;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first1,first2,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first1=0; // стартовый номер для расчета всех баров
      first2=min_rates_total;
     }
   else
     {
      first1=prev_calculated-1; // стартовый номер для расчета новых баров
      first2=first1;
     }
//---- основной цикл расчета индикатора
   for(bar=first1; bar<rates_total && !IsStopped(); bar++)
     {
      //---- вызов функции PriceSeries для получения входной цены price_
      price_=PriceSeries(IPC,bar,open,low,high,close);
      //---- два вызова функции XMASeries
      ma_main = XMA1.XMASeries(0, prev_calculated, rates_total, MA_Method1, PhaseX, Length_main, price_, bar, false);
      ma_plus = XMA2.XMASeries(0, prev_calculated, rates_total, MA_Method1, PhaseX, Length_plus, price_, bar, false);
      //----       
      XdinMA[bar]=ma_main*2-ma_plus+dPriceShift;
     }
//---- основной цикл раскраски индикатора
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorXdinMA[bar]=0;
      if(XdinMA[bar-1]<XdinMA[bar]) ColorXdinMA[bar]=1;
      if(XdinMA[bar-1]>XdinMA[bar]) ColorXdinMA[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
