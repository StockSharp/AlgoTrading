//+---------------------------------------------------------------------+
//|                                                            BvsB.mq5 | 
//|                                           Copyright © 2012, BECEMAL | 
//|                                           http://www.becemal.ru/mql | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2012, BECEMAL"
#property link "http://www.becemal.ru/mql"
//--- номер версии индикатора
#property version   "1.01"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- количество индикаторных буферов 2
#property indicator_buffers 2 
//--- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrPaleGreen,clrDeepPink
//--- отображение метки индикатора
#property indicator_label1  "Buy;Sell"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных класса CBvsB из файла SmoothAlgorithms.mqh
CXMA XMA1;
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
enum Applied_price_      //Тип константы
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
//|  объявление перечислений                     |
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
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method BvsB_Method=MODE_SMA;  // Метод усреднения
input int XLength=12;                      // Глубина сглаживания                  
input int XPhase=15;                       // Параметр сглаживания
//--- XPhase: для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//--- XPhase: для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE;      // Ценовая константа
input int Shift=0;                         // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//--- объявление динамических массивов, которые будут
//--- в дальнейшем использованы в качестве индикаторных буферов
double ExtABuffer[];
double ExtBBuffer[];
//--- объявление целочисленных переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+   
//| BvsB indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_total=XMA1.GetStartBars(BvsB_Method,XLength,XPhase);
//--- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("XLength",XLength);
   XMA1.XMAPhaseCheck("XPhase",XPhase,BvsB_Method);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ExtABuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- осуществление сдвига индикатора по горизонтали на InpKijun
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,ExtBBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- осуществление сдвига индикатора по горизонтали на -InpKijun
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- инициализации переменной для короткого имени индикатора
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(BvsB_Method);
   StringConcatenate(shortname,"BvsB(",XLength,", ",Smooth1,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| BvsB iteration function                                          | 
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
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);
//--- объявление переменных с плавающей точкой  
   double price,x1xma;
//--- объявление целочисленных переменных и получение уже подсчитанных баров
   int first,bar;
//--- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first=0; // стартовый номер для расчёта всех баров
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//--- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,BvsB_Method,XPhase,XLength,price,bar,false);        
      ExtABuffer[bar]=(high[bar]-x1xma)/_Point;
      ExtBBuffer[bar]=(x1xma-low[bar])/_Point;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+