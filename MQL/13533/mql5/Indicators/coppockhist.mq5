//+---------------------------------------------------------------------+
//|                                                     CoppockHist.mq5 |
//|                                 Based on Coppock.mq4 by Robert Hill |
//|                                     Copyright © 2010, EarnForex.com |
//|                                           http://www.earnforex.com/ |
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2010, EarnForex.com"
#property link      "http://www.earnforex.com/"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде четырехцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов четырехцветной гистограммы использованы
#property indicator_color1 clrDeepPink,clrViolet,clrGray,clrDodgerBlue,clrLime
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "Coppock"
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
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
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint ROC1Period = 14;
input uint ROC2Period = 11;
input uint SmoothPeriod=3; // Период сглаживания сигнальной линии
input ENUM_MA_METHOD MA_Method_=MODE_SMA; // Метод усреднения сигнальной линии
input Applied_price_ AppliedPrice=PRICE_CLOSE_;// Ценовая константа
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ExtBuffer[];
double ColorExtBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Описание функции iPriceSeries                                    |
//| Описание класса Moving_Average                                   | 
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация констант
   min_rates_total=int(MathMax(ROC1Period,ROC2Period)+SmoothPeriod+2);
//---- превращение динамического массива MAMABuffer в индикаторный буфер
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"Coppock");
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   double price0,price1,price2,diff,ROCSum;
//---- объявление целочисленных переменных
   int first,bar,clr;
   static int startbar1;
//---- инициализация индикатора в блоке OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      first=int(MathMax(ROC1Period,ROC2Period)); // стартовый номер для расчета всех баров первого цикла
      startbar1=first;
     }
   else // стартовый номер для расчета новых баров
     {
      first=prev_calculated-1;
     }
//---- объявление переменных класса Moving_Average
   static CMoving_Average SMOOTH;
//---- основной цикл расчета средней линии канала
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- вызовы функции PriceSeries для получения входной цены Series
      price0=PriceSeries(AppliedPrice,bar,open,low,high,close);
      price1=PriceSeries(AppliedPrice,bar-ROC1Period,open,low,high,close);
      price2=PriceSeries(AppliedPrice,bar-ROC2Period,open,low,high,close);
      ROCSum=(price0-price1)/price1+(price0-price2)/price2;
      ExtBuffer[bar]=SMOOTH.MASeries(startbar1,prev_calculated,rates_total,SmoothPeriod,MA_Method_,ROCSum,bar,false);
     }

   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      first=min_rates_total;
//---- основной цикл раскраски сигнальной линии
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      clr=2;
      diff=ExtBuffer[bar]-ExtBuffer[bar-1];
      //---
      if(ExtBuffer[bar]>0)
        {
         if(diff>0) clr=4;
         if(diff<0) clr=3;
        }
      //---
      if(ExtBuffer[bar]<0)
        {
         if(diff<0) clr=0;
         if(diff>0) clr=1;
        }
      //---
      ColorExtBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
