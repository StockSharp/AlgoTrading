//+------------------------------------------------------------------+
//|                                      Elliott_Wave_Oscillator.mq5 | 
//|                              Copyright © 2008, tonyc2a@yahoo.com |
//|                                                tonyc2a@yahoo.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, tonyc2a@yahoo.com"
#property link      "tonyc2a@yahoo.com"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде пятицветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов четырёхцветной гистограммы использованы
#property indicator_color1 clrBlue,clrTurquoise,clrGray,clrYellow,clrDarkOrange
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "Elliott_Wave_Oscillator"
//+-----------------------------------+
//|  Описание класса CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
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
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
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
//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_SMA_; //Метод усреднения первого мувинга
input int Length1=5; //Глубина  первого мувинга                   
input int Phase1=15; //Параметр первого мувинга,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC1=PRICE_MEDIAN_;//Ценовая константа первого мувинга
input Smooth_Method MA_Method2=MODE_JJMA; //Метод усреднения второго мувинга
input int Length2=35; //Глубина  второго мувинга
input int Phase2=15;  //Параметр второго мувинга,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC2=PRICE_MEDIAN_;//Ценовая константа второго мувинга
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- индикаторные буферы
double IndBuffer[],ColorIndBuffer[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+   
//| Elliott_Wave_Oscillator initialization function                  | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=MathMax(GetStartBars(MA_Method1,Length1,Phase1),GetStartBars(MA_Method2,Length2,Phase2));

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Elliott_Wave_Oscillator(",Length1,",",Length2,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| Elliott_Wave_Oscillator iteration function                       | 
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
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- Объявление переменных с плавающей точкой  
   double price,x1xma,x2xma;
//---- Объявление целых переменных и получение уже посчитанных баров
   int first,bar;

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=0; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров

//---- Основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC1,bar,open,low,high,close); 
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,price,bar,false);
      price=PriceSeries(IPC2,bar,open,low,high,close);
      x2xma=XMA2.XMASeries(0,prev_calculated,rates_total,MA_Method2,Phase2,Length2,price,bar,false);
      IndBuffer[bar]=x1xma-x2xma;
     }
if(prev_calculated>rates_total || prev_calculated<=0) first++;
//---- Основной цикл раскраски индикатора Ind
   for(bar=first; bar<rates_total; bar++)
     {
      int clr=2;

      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=1;
        }

      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=3;
        }
       ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
