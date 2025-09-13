//+---------------------------------------------------------------------+
//|                                                        TSI_MACD.mq4 |
//|                         Copyright © 2006, MetaQuotes Software Corp. |
//|                                           http://www.metaquotes.net |
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2006, MetaQuotes Software Corp."
//--- ссылка на сайт автора
#property link "http://www.metaquotes.net" 
#property description "TSI_MACD"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrDarkTurquoise,clrViolet
//--- отображение метки индикатора
#property indicator_label1  "TSI_MACD"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 +50
#property indicator_level2   0
#property indicator_level3 -50
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных классов CXMA и CMomentum из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5,XMA6,XMA7;
CMomentum Mom;
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
//| объявление перечислений                      |
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
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_EMA; // Метод усреднения
input uint XFast=8;                      // Быстрое усреднение цены
input uint XSlow=21;                     // Медленное усреднение цены 
input uint MomPeriod=1;                  // Период моментума
input uint XLength1=5;                   // Глубина первого усреднения
input uint XLength2=8;                   // Глубина второго усреднения
input uint XLength3=5;                   // Глубина усреднения сигнальной линии
input int XPhase=15;                     // Параметр сглаживания
//--- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//--- для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE;    // Ценовая константа
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double UpBuffer[],DnBuffer[];
//--- объявление целочисленных переменных начала отсчёта данных
int min_rates_total,min_rates_1,min_rates_2,min_rates_3,min_rates_4;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_1=int(MathMax(XMA1.GetStartBars(XMA_Method,XFast,XPhase),XMA1.GetStartBars(XMA_Method,XSlow,XPhase)));
   min_rates_2=int(MomPeriod);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_4=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total=min_rates_4+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"TSI_MACD");
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
   double price,fast,slow,macd,mtm,xmtm,xxmtm,absmtm,xabsmtm,xxabsmtm,tsi,xtsi;
   int first,bar;
//--- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=0; // стартовый номер для расчёта всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//--- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      fast=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XFast,price,bar,false);
      slow=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XSlow,price,bar,false);
      macd=fast-slow;
      mtm=Mom.MomentumSeries(min_rates_1,prev_calculated,rates_total,MomPeriod,macd,bar,false);
      absmtm=MathAbs(mtm);
      xmtm=XMA3.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,mtm,bar,false);
      xabsmtm=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,absmtm,bar,false);
      xxmtm=XMA5.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xmtm,bar,false);
      xxabsmtm=XMA6.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xabsmtm,bar,false);
      if(xxabsmtm) tsi=100*xxmtm/xxabsmtm;
      else tsi=0;
      if(!tsi) tsi=0.000000001;
      xtsi=XMA7.XMASeries(min_rates_4,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,tsi,bar,false);
      if(!xtsi) xtsi=0.000000001;
      UpBuffer[bar]=tsi;
      DnBuffer[bar]=xtsi;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
