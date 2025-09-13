//+---------------------------------------------------------------------+
//|                                                        ColorHMA.mq5 |
//|                                  Copyright © 2010, Nikolay Kositsin |
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "2010,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- для расчета и отрисовки индикатора использован один буфер
#property indicator_buffers 2
//---- использовано всего одно графическое построение
#property indicator_plots   1
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цветов трехцветной линии использованы
#property indicator_color1  clrGray,clrMediumPurple,clrRed
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  2
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input int HMA_Period=13; // Период скользящей средней
input int HMA_Shift=0;   // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ExtLineBuffer[];
double ColorExtLineBuffer[];
//---- объявление целочисленных переменных
int Hma2_Period,Sqrt_Period;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
   Hma2_Period=int(MathFloor(HMA_Period/2));
   Sqrt_Period=int(MathFloor(MathSqrt(HMA_Period)));
//---- инициализация переменных начала отсчета данных
   min_rates_total=HMA_Period+Sqrt_Period;
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- осуществление сдвига скользящей средней по горизонтали на HMAShift
   PlotIndexSetInteger(0,PLOT_SHIFT,HMA_Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора HMAPeriod
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);
//--- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- имя для окон данных и метка для подокон 
   string short_name="HMA";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name+"("+string(HMA_Period)+")");
  }
//+------------------------------------------------------------------+
//| Описание класса CMoving_Average                                  |
//+------------------------------------------------------------------+  
#include <SmoothAlgorithms.mqh>
//+------------------------------------------------------------------+ 
//| Moving Average                                                   |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const int begin,          // номер начала достоверного отсчета баров
                const double &price[])    // ценовой массив для расчета индикатора
  {
   int begin0=min_rates_total+begin;
//---- проверка количества баров на достаточность для расчета
   if(rates_total<begin0) return(0);
//---- объявления локальных переменных 
   int first,bar,begin1;
   double lwma1,lwma2,dma;
//----
   begin1=HMA_Period+begin;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated==0) // проверка на первый старт расчета индикатора
     {
      first=begin; // стартовый номер для расчета всех баров      
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,begin0+1);
      for(bar=0; bar<=begin0; bar++) ColorExtLineBuffer[bar]=0;
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- объявление переменной класса CMoving_Average из файла HMASeries_Cls.mqh
   static CMoving_Average MA1,MA2,MA3;
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      lwma1=MA1.LWMASeries(begin,prev_calculated,rates_total,Hma2_Period,price[bar],bar,false);
      lwma2=MA2.LWMASeries(begin,prev_calculated,rates_total,HMA_Period, price[bar],bar,false);
      dma=2*lwma1-lwma2;
      ExtLineBuffer[bar]=MA3.LWMASeries(begin1,prev_calculated,rates_total,Sqrt_Period,dma,bar,false);
     }
//---- пересчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=begin0;
//---- основной цикл раскраски сигнальной линии
   for(bar=first; bar<rates_total; bar++)
     {
      ColorExtLineBuffer[bar]=0;
      if(ExtLineBuffer[bar-1]<ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=1;
      if(ExtLineBuffer[bar-1]>ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
