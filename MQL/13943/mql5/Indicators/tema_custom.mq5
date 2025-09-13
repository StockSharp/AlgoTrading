//+---------------------------------------------------------------------+
//|                                                     TEMA_CUSTOM.mq5 | 
//|                                      Copyright © 2015, Sergey Vrady | 
//|                                  https://www.mql5.com/ru/code/13263 | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2015, Sergey Vrady"
#property link "https://www.mql5.com/ru/code/13263"
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
//---- в качестве цвета линии индикатора использован MediumOrchid цвет
#property indicator_color1 clrMediumOrchid
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "TEMA_CUSTOM"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3;
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
   PRICE_SIMPL_,         //Simple Price (OC/2)
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
//| Входные параметры индикатора     |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_EMA_; // Метод усреднения первого сглаживания
input int Length1=12; // Глубина  первого сглаживания
input int Phase1=15;  // Параметр первого сглаживания
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method2=MODE_EMA; // Метод усреднения второго сглаживания
input int Length2 = 5; // Глубина  второго сглаживания
input int Phase2=15;   // Параметр второго сглаживания
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method3=MODE_EMA; // Метод усреднения третьего сглаживания
input int Length3 = 5; // Глубина третьего сглаживания
input int Phase3=15;   // Параметр третьего сглаживания
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_MEDIAN; // Ценовая константа, по которой производится расчет индикатора
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // Сдвиг индикатора по вертикали в пунктах
//+-----------------------------------+
//---- объявление динамического массива, который будет в 
//---- дальнейшем использован в качестве индикаторного буфера
double IndBuffer[];
//---- объявление переменной значения вертикального сдвига скользящей средней
double dPriceShift;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,StartBars1,StartBars2,StartBars3;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   StartBars1=GetStartBars(MA_Method1, Length1, Phase1);
   StartBars2=GetStartBars(MA_Method2, Length2, Phase2);
   StartBars2=GetStartBars(MA_Method3, Length3, Phase3);
   min_rates_total=StartBars1+StartBars2+StartBars3;
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Length1", Length1);
   XMA2.XMALengthCheck("Length2", Length2);
   XMA3.XMALengthCheck("Length3", Length3);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("Phase1", Phase1, MA_Method1);
   XMA2.XMAPhaseCheck("Phase2", Phase2, MA_Method2);
   XMA3.XMAPhaseCheck("Phase3", Phase3, MA_Method3);
//---- инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(MA_Method1);
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   string Smooth3=XMA1.GetString_MA_Method(MA_Method3);
   StringConcatenate(shortname,"TEMA_CUSTOM(",Length1,", ",Length2,", ",Length3,", ",Smooth1,", ",Smooth2,", ",Smooth3,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- завершение инициализации
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
//---- объявление переменных с плавающей точкой  
   double price,x1xma,x2xma,x3xma;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=0; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,price,bar,false);
      x2xma=XMA2.XMASeries(StartBars1,prev_calculated,rates_total,MA_Method2,Phase2,Length2,x1xma,bar,false);
      x3xma=XMA3.XMASeries(StartBars2,prev_calculated,rates_total,MA_Method3,Phase3,Length3,x2xma,bar,false);
      IndBuffer[bar]=3*x1xma-3*x2xma+x3xma;
      IndBuffer[bar]+=dPriceShift;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
