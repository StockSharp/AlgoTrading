//+------------------------------------------------------------------+
//|                                     BalanceOfPower_Histogram.mq5 |
//|                                         Copyright © 2012, RoboFx |
//|                                            http://www.robofx.org |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2012, RoboFx"
//---- ссылка на сайт автора
#property link      "http://www.robofx.org"
#property description "Индикатор Balance of Power (BOP), описанный Игорем Лившином, измеряет силу быков и медведей оценивая способность тех и других толкать цены до предельного уровня"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//---- отрисовка индикатора в виде четырёхцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов гистограммы использованы
#property indicator_color1 clrLime,clrDeepSkyBlue,clrTeal,clrBlue,clrPurple,clrMediumVioletRed,clrMagenta,clrRed
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки линии индикатора
#property indicator_label1  "BalanceOfPower_Histogram"
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1;
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
//|  объявление перечислений                     |
//+----------------------------------------------+
enum Applied_price_ //Тип константы
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
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method XMethod=MODE_T3;              // метод усреднения
input uint XLength=15;                            // глубина усреднения          
input int XPhase=15;                              // параметр усреднения,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input int HighLevel=+20;                          // уровень перекупленности
input int LowLevel=-20;                           // уровень перепроданности
input int Shift=0;                                // сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=GetStartBars(XMethod,XLength,XPhase)+1;

//---- превращение динамического массива SignBuffer в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"BalanceOfPower_Histogram");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- количество  горизонтальных уровней индикатора 3  
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,0.0);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- в качестве цветов линий горизонтальных уровней использованы 
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrRed);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_SOLID);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчёта индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчёта индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- объявления локальных переменных 
   int first;
   double diff,bop;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=0; // стартовый номер для расчёта всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- основной цикл расчёта индикатора
   for(int bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      diff=high[bar]-low[bar];
      if(!diff) diff=_Point;
      bop=(close[bar]-open[bar])/diff;
      IndBuffer[bar]=100*XMA1.XMASeries(0,prev_calculated,rates_total,XMethod,XPhase,XLength,bop,bar,false);
     }
//---- 
   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;
//---- Основной цикл раскраски индикатора
   for(int bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=10;
      bop=IndBuffer[bar];
      diff=IndBuffer[bar]-IndBuffer[bar-1];

      if(bop>0)
        {
         if(diff>0.0)
           {
            if(bop>HighLevel) clr=0;
            else clr=2;
           }
         if(diff<0.0)
           {
            if(bop>HighLevel) clr=1;
            else clr=3;
           }
        }

      if(bop<0)
        {
         if(diff<0.0)
           {
            if(bop<LowLevel) clr=7;
            else clr=5;
           }
         if(diff>0.0)
           {
            if(bop<LowLevel) clr=6;
            else clr=4;
           }
        }
      ColorIndBuffer[bar]=clr;
     }
//----              
   return(rates_total);
  }
//+------------------------------------------------------------------+
