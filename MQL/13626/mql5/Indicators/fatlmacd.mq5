//+---------------------------------------------------------------------+ 
//|                                                        FatlMacd.mq5 | 
//|                                  Copyright © 2014, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+ 
#property copyright "Copyright © 2014, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора в виде цветных значков
#property indicator_type1 DRAW_COLOR_ARROW
//---- в качестве цветов четырехцветной гистограммы использованы
#property indicator_color1 clrMagenta,clrDeepPink,clrGray,clrDodgerBlue,clrOliveDrab
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 3
#property indicator_width1 3
//---- отображение метки индикатора
#property indicator_label1 "FatlMacd"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 0
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Описание классов усреднений                  |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных классов CFATL, CJJMA и CMomentum из файла JJMASeries_Cls.mqh
CXMA XMA1;
CFATL FATL1;
//+----------------------------------------------+
//| Объявление перечислений                      |
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
input Smooth_Method XMA_Method=MODE_T3; // Метод усреднения гистограммы
input int XMA_Lengh = 20; // Период усреднения
input int XMA_Phase= 100; // Параметр усреднения скользящих средних
                          // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                          // для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ AppliedPrice=PRICE_CLOSE_;// Ценовая константа
//+----------------------------------------------+
//---- объявление целых переменных начала отсчета данных
int min_rates_total,min_rates_1;
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_1=39;
   min_rates_total=min_rates_1+XMA1.GetStartBars(XMA_Method,XMA_Lengh,XMA_Phase);
//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Fast_XMA",XMA_Phase);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("Phase",XMA_Phase,XMA_Method);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"FatlMacd");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
//---- объявление целочисленных переменных
   int first,bar;
//---- объявление переменных с плавающей точкой  
   double price,fatl,macd;
//---- инициализация индикатора в блоке OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      first=0; // стартовый номер для расчета всех баров первого цикла
     }
   else // стартовый номер для расчета новых баров
     {
      first=prev_calculated-1;
     }
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- получаем входную цену price
      price=PriceSeries(AppliedPrice,bar,open,low,high,close);
      //---- грузим входную цену price в FATLSeries() и получаем fatl
      fatl=FATL1.FATLSeries(0,prev_calculated,rates_total,price,bar,false);
      //---- получим MACD
      macd=price-fatl;
      //---- грузим macd в XMASeries() для усреднения
      IndBuffer[bar]=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XMA_Phase,XMA_Lengh,macd,bar,false);
      //---- меняем размерность индикатора до целых значений
      IndBuffer[bar]/=_Point;
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//---- основной цикл раскраски индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;
      //----
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }
      //----
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }
      //----
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
