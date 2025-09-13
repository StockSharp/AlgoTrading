//+---------------------------------------------------------------------+
//|                                                         BlauHLM.mq5 |
//|                                  Copyright © 2014, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2014, Nikolay Kositsin"
//--- ссылка на сайт автора
#property link "farria@mail.redcom.ru" 
#property description "HLM Oscillator"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчёта и отрисовки индикатора использовано четыре буфера
#property indicator_buffers 4
//--- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора 1             |
//+----------------------------------------------+
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrBlue,clrPurple
//--- отображение метки индикатора
#property indicator_label1  "Blau HLM Signal"
//+----------------------------------------------+
//| Параметры отрисовки индикатора 2             |
//+----------------------------------------------+
//--- отрисовка индикатора в виде четырёхцветной гистограммы
#property indicator_type2 DRAW_COLOR_HISTOGRAM
//--- в качестве цветов пятицветной гистограммы использованы
#property indicator_color2 clrDeepPink,clrOrange,clrGray,clrYellowGreen,clrTeal
//--- линия индикатора - сплошная
#property indicator_style2 STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width2 2
//--- отображение метки индикатора
#property indicator_label2  "Blau HLM"
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4;
//+----------------------------------------------+
//| объявление перечислений                      |
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
input Smooth_Method XMA_Method=MODE_EMA; // Метод усреднения
input uint XLength=2;                    // Период моментума
input uint XLength1=20;                  // Глубина первого усреднения
input uint XLength2=5;                   // Глубина второго усреднения
input uint XLength3=3;                   // Глубина третьего усреднения
input uint XLength4=3;                   // Глубина усреднения сигнальной линии
input int XPhase=15;                     // Параметр сглаживания
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
double UpBuffer[],DnBuffer[];
//--- объявление целочисленных переменных начала отсчёта данных
int min_rates_total,min_rates_1,min_rates_2,min_rates_3,min_rates_4;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_1=int(XLength);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_4=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
   min_rates_total=min_rates_4+XMA1.GetStartBars(XMA_Method,XLength4,XPhase);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,IndBuffer,INDICATOR_DATA);
//--- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(3,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"BlauHLM");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчёта индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчёта индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);
//--- объявления локальных переменных 
   double hmu,lmd,hlm,xhlm,xxhlm,xxxhlm,sign;
   int first,bar;
//--- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=min_rates_1; // стартовый номер для расчёта всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//--- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      hmu=high[bar]-high[bar-(XLength-1)];
      lmd=-(low[bar]-low[bar-(XLength-1)]);
      //---      
      hmu=(hmu>0)?hmu:0;
      lmd=(lmd>0)?lmd:0;
      hlm=hmu-lmd;
      hlm/=_Point;
      //---  
      xhlm=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,hlm,bar,false);
      xxhlm=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xhlm,bar,false);
      xxxhlm=XMA3.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,xxhlm,bar,false);
      sign=XMA4.XMASeries(min_rates_4,prev_calculated,rates_total,XMA_Method,XPhase,XLength4,xxxhlm,bar,false);
      //---
      IndBuffer[bar]=xxxhlm;
      UpBuffer[bar]=xxxhlm;
      DnBuffer[bar]=sign;
     }
//---
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//--- основной цикл раскраски индикатора Ind
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }
      ColorIndBuffer[bar]=clr;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
