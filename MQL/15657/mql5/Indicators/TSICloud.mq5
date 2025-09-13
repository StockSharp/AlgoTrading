//+------------------------------------------------------------------+
//|                                                     TSICloud.mq5 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                                                  |
//|                                   Modified from TSI-Osc by Toshi |
//|                                  http://toshi52583.blogspot.com/ |
//+------------------------------------------------------------------+
//| Для работы  индикатора файл SmoothAlgorithms.mqh                 |
//| следует положить в папку: каталог_данных_терминала\\MQL5\Include |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_FILLING
//---- в качестве цветов облака индикатора использованы
#property indicator_color1  clrAqua,clrRed
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "TSICloud"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 +50.0
#property indicator_level2   0.0
#property indicator_level3 -50.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA MTM1,MTM2,ABSMTM1,ABSMTM2;
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
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА                |
//+----------------------------------------------+
input Smooth_Method First_Method=MODE_SMA_; //метод усреднения 1
input uint First_Length=12; //глубина сглаживания 1                    
input int First_Phase=15; //параметр сглаживания 1,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей 
input Smooth_Method Second_Method=MODE_SMA_; //метод усреднения 2
input uint Second_Length=12; //глубина сглаживания 2                    
input int Second_Phase=15; //параметр сглаживания 2,
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE_;//ценовая константа  
input int Shift=0; // сдвиг индикатора по горизонтали в барах
input uint TriggerShift=1; // cдвиг бара для тригера 
//+----------------------------------------------+
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total,min_rates_total1,min_rates_total2;
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double TSIBuffer[],TriggerBuffer[];
//+------------------------------------------------------------------+   
//| TSI indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total1=GetStartBars(First_Method,First_Length,First_Phase)+1;
   min_rates_total2=min_rates_total1+GetStartBars(First_Method,First_Length,First_Phase);
   min_rates_total=int(min_rates_total1+GetStartBars(Second_Method,Second_Length,Second_Phase)+TriggerShift);

//---- установка алертов на недопустимые значения внешних переменных
   MTM1.XMALengthCheck("First_Length",First_Length);
   MTM1.XMAPhaseCheck("First_Phase",First_Phase, First_Method);
   MTM1.XMALengthCheck("Second_Length",Second_Length);
   MTM1.XMAPhaseCheck("Second_Phase",Second_Phase,Second_Method);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,TSIBuffer,INDICATOR_DATA);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   string Smooth1=MTM1.GetString_MA_Method(First_Method);
   string Smooth2=MTM1.GetString_MA_Method(Second_Method);
   StringConcatenate(shortname,"TSI-Oscillator(",Smooth1,", ",First_Length,", ",Smooth2,", ",Second_Length,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| TSI iteration function                                           | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const int begin,          // номер начала достоверного отсчёта баров
                const double &price[]     // ценовой массив для расчёта индикатора
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total+begin) return(0);

//---- Объявление переменных с плавающей точкой  
   double dprice,absdprice,mtm1,absmtm1,mtm2,absmtm2;
//---- Объявление целых переменных и получение уже посчитанных баров
   int first,bar;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first=1; // стартовый номер для расчёта всех баров
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- Основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      dprice=price[bar]-price[bar-1];
      absdprice=MathAbs(dprice);
      mtm1=MTM1.XMASeries(1,prev_calculated,rates_total,First_Method,First_Phase,First_Length,dprice,bar,false);
      absmtm1=ABSMTM1.XMASeries(1,prev_calculated,rates_total,First_Method,First_Phase,First_Length,absdprice,bar,false);
      mtm2=MTM2.XMASeries(min_rates_total1,prev_calculated,rates_total,Second_Method,Second_Phase,Second_Length,mtm1,bar,false);
      absmtm2=ABSMTM2.XMASeries(min_rates_total1,prev_calculated,rates_total,Second_Method,Second_Phase,Second_Length,absmtm1,bar,false);
      if(bar>min_rates_total2) TSIBuffer[bar]=100.0*mtm2/absmtm2;
      else TSIBuffer[bar]=EMPTY_VALUE;
      if(bar>min_rates_total) TriggerBuffer[bar]=TSIBuffer[bar-TriggerShift];
      else                    TriggerBuffer[bar]=EMPTY_VALUE;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
