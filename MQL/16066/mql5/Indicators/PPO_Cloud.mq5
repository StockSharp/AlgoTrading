//+------------------------------------------------------------------+
//|                                                    PPO_Cloud.mq5 |
//|                                       Copyright © 2007 Tom Balfe | 
//|                                         redcarsarasota@yahoo.com | 
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2007 Tom Balfe"
//---- ссылка на сайт автора
#property link "redcarsarasota@yahoo.com"
#property description "This is a momentum indicator."
#property description "Signal line is EMA of PPO."
#property description "Follows formula: PPO=(FastEMA-SlowEMA)/SlowEMA"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1  0
#property indicator_levelcolor clrBlueViolet
#property indicator_levelstyle STYLE_SOLID
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде цветного облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветjd индикатора использованы
#property indicator_color1  clrDodgerBlue,clrPurple
//---- отображение метки линии индикатора
#property indicator_label1  "PPO_Cloud"
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3;
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
input Smooth_Method FastMethod=MODE_EMA_; //метод быстрого усреднения
input uint FastLength=12; //глубина быстрого усреднения          
input int FastPhase=15; //параметр быстрого усреднения,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method SlowMethod=MODE_EMA_; //метод медленного усреднения
input uint SlowLength=26; //глубина медленного усреднения                 
input int SlowPhase=15; //параметр медленного усреднения,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method SignMethod=MODE_EMA_; //метод сглаживания
input uint SignLength=9; //глубина сглаживания                    
input int SignPhase=15; //параметр сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE_;//ценовая константа
input int Shift=0; // сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double PPOBuffer[],SignBuffer[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_,min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_=int(MathMax(GetStartBars(FastMethod,FastLength,FastPhase),GetStartBars(SlowMethod,SlowLength,SlowPhase)));
   min_rates_total=min_rates_+GetStartBars(SignMethod,SignLength,SignPhase);

//---- превращение динамического массива SignBuffer в индикаторный буфер
   SetIndexBuffer(0,PPOBuffer,INDICATOR_DATA);
//---- превращение динамического массива PPOBuffer в индикаторный буфер
   SetIndexBuffer(1,SignBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 1 на 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"PPO_Cloud");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=0; // стартовый номер для расчёта всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- основной цикл расчёта индикатора
   for(int bar=first; bar<rates_total && !IsStopped(); bar++)
     {

      //---- Вызов функции PriceSeries для получения входной цены price_
      double price=PriceSeries(IPC,bar,open,low,high,close);     
      double fast=XMA1.XMASeries(0,prev_calculated,rates_total,FastMethod,FastPhase,FastLength,price,bar,false);
      double slow=XMA2.XMASeries(0,prev_calculated,rates_total,SlowMethod,SlowPhase,SlowLength,price,bar,false);
      if(!slow) slow=1;
      double ppo=(fast-slow)/slow;
      PPOBuffer[bar]=ppo;
      SignBuffer[bar]=XMA3.XMASeries(min_rates_,prev_calculated,rates_total,SignMethod,SignPhase,SignLength,ppo,bar,false);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
