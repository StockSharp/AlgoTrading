//+---------------------------------------------------------------------+ 
//|                                                  XKRI_Histogram.mq5 | 
//|                                  Copyright © 2015, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2015, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде пятицветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов пятицветной гистограммы использованы
#property indicator_color1 clrGray,clrOliveDrab,clrDodgerBlue,clrDeepPink,clrMagenta
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "XKRI"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1;
//---- объявление переменных класса Moving_Average из файла SmoothAlgorithms.mqh
CMoving_Average MA;
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
enum ENUM_PRICE_TYPE //тип константы
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
input uint KRIPeriod=20; // Период усреднения
input ENUM_MA_METHOD MA_Method_=MODE_SMA; // Метод усреднения
input double Ratio=1.0;
input ENUM_PRICE_TYPE IPC=PRICE_CLOSE_; // Ценовая константа
input Smooth_Method XMA_Method=MODE_JJMA; // Метод усреднения
input uint XLength=7; // Глубина сглаживания
input int XPhase=15;  // Параметр сглаживания
//--- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//--- для VIDIA это период CMO, для AMA это период медленной скользящей
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input double HighLevel=+1; // Уровень перекупленности
input double LowLevel=-1;  // Уровень перепроданности
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_1,min_rates_total;
//+------------------------------------------------------------------+    
//| XKRI indicator initialization function                           | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация констант
   min_rates_1=int(KRIPeriod+1);
   min_rates_total=min_rates_1+GetStartBars(XMA_Method,XLength,XPhase);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"XKRI( KRIPeriod = ",KRIPeriod,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- количество  горизонтальных уровней индикатора 2   
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,LowLevel);
//---- в качестве цветов линий горизонтальных уровней использованы цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrPurple);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| XKRI iteration function                                          | 
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
   double price,KRI,mov;
//---- объявление целочисленных переменных
   int first,bar,clr;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated==0) // проверка на первый старт расчета индикатора
     {
      first=0; // стартовый номер для расчета всех баров
     }
   else // стартовый номер для расчета новых баров
     {
      first=prev_calculated-1;
     }
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- вызов функции PriceSeries для получения входной цены Series
      price=PriceSeries(IPC,bar,open,low,high,close);
      mov=MA.MASeries(0,prev_calculated,rates_total,KRIPeriod,MA_Method_,price,bar,false);
      KRI=100*(price-mov)/mov;
      IndBuffer[bar]=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,KRI,bar,false);
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;
//---- основной цикл раскраски индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      clr=0;
      //----
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=2;
        }
      //----
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
        }
      ColorIndBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
