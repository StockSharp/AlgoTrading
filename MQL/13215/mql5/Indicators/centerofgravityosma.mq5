//+---------------------------------------------------------------------+
//|                                             CenterOfGravityOSMA.mq5 |
//|                         Copyright © 2007, MetaQuotes Software Corp. |
//|                                           http://www.metaquotes.net |
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---- номер версии индикатора
#property version   "1.11"
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
#property indicator_color1 clrMagenta,clrViolet,clrGray,clrDodgerBlue,clrAqua
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "CenterOfGravityOSMA"
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
input uint Period_=10; // Период усреднения индикатора
input uint SmoothPeriod1=3; // Период сглаживания сигнальной линии
input ENUM_MA_METHOD MA_Method_1=MODE_SMA; // Метод усреднения сигнальной линии
input uint SmoothPeriod2=3; // Период сглаживания сигнальной линии
input ENUM_MA_METHOD MA_Method_2=MODE_SMA; // Метод усреднения сигнальной линии
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
   min_rates_total=int(Period_+1+SmoothPeriod1+SmoothPeriod2+2);
//---- превращение динамического массива MAMABuffer в индикаторный буфер
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Center of Gravity OSMA(",Period_,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
   double price,sma,lwma,res1,res2,res3,diff;
//---- объявление целочисленных переменных
   int first,bar,clr;
   static int startbar1,startbar2;
//---- инициализация индикатора в блоке OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      first=0; // стартовый номер для расчета всех баров первого цикла
      startbar1=int(Period_+1);
      startbar2=startbar1+int(SmoothPeriod1);
     }
   else // стартовый номер для расчета новых баров
     {
      first=prev_calculated-1;
     }
//---- объявление переменных класса Moving_Average
   static CMoving_Average MA,LWMA,SIGN,SMOOTH;
//---- основной цикл расчета средней линии канала
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- вызов функции PriceSeries для получения входной цены Series
      price=PriceSeries(AppliedPrice,bar,open,low,high,close);
      //----
      sma=MA.MASeries(0,prev_calculated,rates_total,Period_,MODE_SMA,price,bar,false);
      lwma=LWMA.MASeries(0,prev_calculated,rates_total,Period_,MODE_LWMA,price,bar,false);
      //----
      res1=sma*lwma/_Point;
      res2=SIGN.MASeries(startbar1,prev_calculated,rates_total,SmoothPeriod1,MA_Method_1,res1,bar,false);
      res3=res1-res2;
      ExtBuffer[bar]=SMOOTH.MASeries(startbar2,prev_calculated,rates_total,SmoothPeriod2,MA_Method_2,res3,bar,false);
     }
//---
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
