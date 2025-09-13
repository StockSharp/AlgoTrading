//+---------------------------------------------------------------------+
//|                                                BlauSMStochastic.mq5 |
//|                                  Copyright © 2013, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы индикатора файл SmoothAlgorithms.mqh следует положить    |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2013, Nikolay Kositsin"
//--- ссылка на сайт автора
#property link "farria@mail.redcom.ru" 
#property description "Stochastic Oscillator"
//--- номер версии индикатора
#property version   "1.01"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчета и отрисовки индикатора использовано четыре буфера
#property indicator_buffers 4
//--- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrLime,clrRed
//--- отображение метки индикатора
#property indicator_label1  "Blau SM Stochastic Signal"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 2            |
//+----------------------------------------------+
//--- отрисовка индикатора в виде четырехцветной гистограммы
#property indicator_type2 DRAW_COLOR_HISTOGRAM
//--- в качестве цветов пятицветной гистограммы использованы
#property indicator_color2 clrMagenta,clrViolet,clrGray,clrDeepSkyBlue,clrBlue
//--- линия индикатора - сплошная
#property indicator_style2 STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width2 2
//--- отображение метки индикатора
#property indicator_label2  "Blau SM Stochastic"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 +40
#property indicator_level2   0
#property indicator_level3 -40
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5,XMA6,XMA7;
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
enum Applied_price_      // Тип константы
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPL_,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_EMA; // Метод усреднения
input uint XLength=5;   //Период стохастического моментума
input uint XLength1=20; //Глубина первого усреднения
input uint XLength2=5;  //Глубина второго усреднения
input uint XLength3=3;  //Глубина третьего усреднения
input uint XLength4=3;  //Глубина усреднения сигнальной линии
input int XPhase=15;    //Параметр сглаживания
//--- XPhase: для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//--- XPhase: для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE;   //Ц еновая константа
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
double UpBuffer[],DnBuffer[];
int Count[];
double iHigh[],iLow[];
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_1,min_rates_2,min_rates_3,min_rates_4;
//+------------------------------------------------------------------+
//|  Пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
                          int Size)
  {
//---
   int numb,Max1,Max2;
   static int count=1;
//---
   Max2=Size;
   Max1=Max2-1;
//---
   count--;
   if(count<0) count=Max1;
//---
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчета данных
   min_rates_1=int(XLength);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_4=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
   min_rates_total=min_rates_4+XMA1.GetStartBars(XMA_Method,XLength4,XPhase);
//--- распределение памяти под массивы переменных  
   ArrayResize(Count,XLength);
   ArrayResize(iHigh,XLength);
   ArrayResize(iLow,XLength);
//--- 
   ArrayInitialize(Count,0);
   ArrayInitialize(iHigh,0.0);
   ArrayInitialize(iLow,0.0);
//---  
   ArraySetAsSeries(iHigh,true);
   ArraySetAsSeries(iLow,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,IndBuffer,INDICATOR_DATA);
//--- превращение динамического массива в цветовой индексный буфер   
   SetIndexBuffer(3,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"BlauSMStochastic");
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
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);
//--- объявления локальных переменных 
   double LL,HH,price,sm,xsm,xxsm,xxxsm,half,xhalf,xxhalf,xxxhalf;
   int first,bar;
//--- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=0; // стартовый номер для расчета всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//--- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      iLow[Count[0]]=low[bar];
      iHigh[Count[0]]=high[bar];
      LL=iLow[ArrayMinimum(iLow,0,XLength)];
      HH=iHigh[ArrayMaximum(iHigh,0,XLength)];
      price=PriceSeries(IPC,bar,open,low,high,close);
      //---       
      sm=price-0.5*(LL+HH);
      xsm=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,sm,bar,false);
      xxsm=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xsm,bar,false);
      xxxsm=XMA3.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,xxsm,bar,false);
      //---
      half=0.5*(HH-LL);
      xhalf=XMA4.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,half,bar,false);
      xxhalf=XMA5.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xhalf,bar,false);
      xxxhalf=XMA6.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,xxhalf,bar,false);
      //---
      if(xxxhalf) IndBuffer[bar]=100*xxxsm/xxxhalf;
      else IndBuffer[bar]=0.0;
      //---
      UpBuffer[bar]=IndBuffer[bar];
      DnBuffer[bar]=XMA7.XMASeries(min_rates_4,prev_calculated,rates_total,XMA_Method,XPhase,XLength4,IndBuffer[bar],bar,false);
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,XLength);
     }
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

   