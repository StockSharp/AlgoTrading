//+---------------------------------------------------------------------+
//|                                                            XCCX.mq5 | 
//|                                Copyright © 2013,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Commodity Chanel Index"
//---- номер версии индикатора
#property version   "1.10"
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
//---- в качестве цвета линии индикатора использован темно-оранжевый цвет
#property indicator_color1 clrDarkOrange
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "XCCX"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1       -50.0
#property indicator_level2        50.0
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMAD,XMAH,XMAL;
//+----------------------------------------------+
//| Объявление перечислений                      |
//+----------------------------------------------+
enum Applied_price //тип константы
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
//---
/*enum Smooth_Method - объявлено в файле SmoothAlgorithms.mqh
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
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method DSmoothMethod=MODE_JJMA; // Метод усреднения цены
input int DPeriod=15;  // Период скользящей средней
input int DPhase=15;   // Параметр усреднения
                       // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                       // для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MSmoothMethod=MODE_T3; // Метод усреднения отклонения
input int MPeriod=15; // Период среднего отклонения
input int MPhase=15;  // Среднего отклонения
                      // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                      // для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price IPC=PRICE_TYPICAL; // Ценовая константа
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамического массива, который будет в 
//---- дальнейшем использован в качестве индикаторного буфера
double XCCX[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_D,min_rates_M;
//+------------------------------------------------------------------+   
//| XCCX indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_D=XMAD.GetStartBars(DSmoothMethod,DPeriod,DPhase);
   min_rates_M=XMAD.GetStartBars(MSmoothMethod,MPeriod,MPhase);
   min_rates_total=min_rates_D+min_rates_M;
//---- установка алертов на недопустимые значения внешних переменных
   XMAD.XMALengthCheck("DPeriod", DPeriod);
   XMAD.XMALengthCheck("MPeriod", MPeriod);
//---- установка алертов на недопустимые значения внешних переменных
   XMAD.XMAPhaseCheck("DPhase",DPhase,DSmoothMethod);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,XCCX,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"XCCX");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname,SmoothD,SmoothM;
   SmoothD=XMAD.GetString_MA_Method(DSmoothMethod);
   SmoothM=XMAD.GetString_MA_Method(MSmoothMethod);
   StringConcatenate(shortname,"Commodity Chanel Index(",string(DPeriod),",",string(MPeriod),",",SmoothD,",",SmoothM,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| XCCX iteration function                                          | 
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
   double price,xma,upccx,dnccx,xupccx,xdnccx;
//---- объявление целочисленных переменных
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров и инициализация переменных
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=0; // стартовый номер для расчета всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- вызов функции PriceSeries для получения входной цены price
      price=PriceSeries(IPC,bar,open,low,high,close);
      //---- один вызов функции XMASeries 
      xma=XMAD.XMASeries(0,prev_calculated,rates_total,DSmoothMethod,DPhase,DPeriod,price,bar,false);
      upccx=price-xma;
      dnccx=MathAbs(upccx);
      //---- два вызова функции XMASeries  
      xupccx=XMAH.XMASeries(min_rates_D,prev_calculated,rates_total,MSmoothMethod,MPhase,MPeriod,upccx,bar,false);
      xdnccx=XMAL.XMASeries(min_rates_D,prev_calculated,rates_total,MSmoothMethod,MPhase,MPeriod,dnccx,bar,false);
      //---- инициализация индикаторного буфера
      if(xupccx) // запрет деления на ноль!
         XCCX[bar]=100*xupccx/xdnccx;
      else XCCX[bar]=EMPTY_VALUE;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
