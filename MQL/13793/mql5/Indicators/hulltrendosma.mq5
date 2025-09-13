//+---------------------------------------------------------------------+
//|                                                   HullTrendOSMA.mq5 |
//|                                        Copyright © 2005, adoleh2000 |
//|                                                adoleh2000@yahoo.com |
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property  copyright "Copyright © 2005, adoleh2000."
#property  link      "adoleh2000@yahoo.com"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора в виде четырехцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов четырехцветной гистограммы использованы
#property indicator_color1 clrRed,clrMagenta,clrGray,clrDodgerBlue,clrLime
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1  "HullTrendOSMA"
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4;
//+----------------------------------------------+
//| Объявление перечислений                      |
//+----------------------------------------------+
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
//+----------------------------------------------+
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
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint XLength=20;                           // Период индикатора
input Applied_price_ IPC=PRICE_CLOSE;            // Цена индикатора
input Smooth_Method XMA_Method=MODE_LWMA;        // Метод усреднения
input int XPhase=15;                             // Параметр усреднения
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
input uint XLength1=5;                           // Период сглаживания индикатора
input Smooth_Method XMA_Method1=MODE_JJMA;       // Метод сглаживания
input int XPhase1=100;                           // Параметр сглаживания
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int  min_rates_1,min_rates_2,min_rates_total;
//----
int XLength2,SqrXLength;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- инициализация переменных
   XLength2=int(XLength/2);
   SqrXLength=int(MathFloor(MathSqrt(XLength)));
//---- инициализация переменных начала отсчета данных
   min_rates_1=GetStartBars(XMA_Method,XLength,XPhase);
   min_rates_2=min_rates_1+GetStartBars(XMA_Method,SqrXLength,XPhase);
   min_rates_total=min_rates_2+GetStartBars(XMA_Method1,XLength1,XPhase1)+1;
//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"HullTrendOSMA");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
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
   if(rates_total<min_rates_total) return(RESET);
//---- объявление локальных переменных 
   int first,bar,clr;
//---- объявление переменных с плавающей точкой  
   double price,xma,xma2,hma,xhma,osma,xosma,diff;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=0; // стартовый номер для расчета всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      xma2=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,price,bar,false);
      xma=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price,bar,false);
      hma=2*xma2-xma;
      xhma=XMA3.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,SqrXLength,hma,bar,false);
      osma=hma-xhma;
      xosma=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method1,XPhase1,XLength1,osma,bar,false);
      xosma/=_Point;
      IndBuffer[bar]=xosma;
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;
//---- основной цикл раскраски индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      clr=2;
      xosma=IndBuffer[bar];
      diff=xosma-IndBuffer[bar-1];
      //----
      if(xosma>0)
        {
         if(diff>0) clr=4;
         if(diff<0) clr=3;
        }
      //----
      if(xosma<0)
        {
         if(diff<0) clr=0;
         if(diff>0) clr=1;
        }
      //----
      ColorIndBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
