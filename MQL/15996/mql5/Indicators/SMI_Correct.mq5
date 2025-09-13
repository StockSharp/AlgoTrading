//+------------------------------------------------------------------+
//|                                                  SMI_Correct.mq5 |
//|                                Copyright © 2016, transport_david | 
//|                                                                  | 
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2016, transport_david"
//---- авторство индикатора
#property link      ""
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
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrAqua,clrMagenta
//--- отображение метки индикатора
#property indicator_label1  "SMI_Correct"
//+----------------------------------------------+
//|  Объявление констант                         |
//+----------------------------------------------+
#define RESET 0       // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5;
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
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint ExtrPeriod=13; //период поиска экстремумов                  
input Smooth_Method MA_Method1=MODE_EMA_; //метод усреднения первого сглаживания 
input uint Length1=25; //глубина  первого сглаживания                    
input int Phase1=15; //параметр первого сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method2=MODE_JJMA; //метод усреднения второго сглаживания 
input uint Length2=3; //глубина  второго сглаживания 
input int Phase2=15;  //параметр второго сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method3=MODE_JJMA; //метод усреднения третьего сглаживания 
input uint Length3 = 5; //глубина  третьего сглаживания 
input int Phase3=15;  //параметр третьего сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE_;//ценовая константа
input int Shift=0; // сдвиг индикатора по горизонтали в барах
input int HighLevel=+50;
input int MiddleLevel=0;
input int LowLevel=-50;
input color HighLevelsColor=clrBlue;
input color MiddleLevelsColor=clrGray;
input color LowLevelsColor=clrRed;
//+----------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double SMIBuffer[];
double TriggerBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_1=int(ExtrPeriod);
   min_rates_2=min_rates_1+GetStartBars(MA_Method1,Length1,Phase1);
   min_rates_3=min_rates_2+GetStartBars(MA_Method2,Length2,Phase2);
   min_rates_total=min_rates_3+GetStartBars(MA_Method3,Length3,Phase3);

//---- превращение динамического массива SMIBuffer[] в индикаторный буфер
   SetIndexBuffer(0,SMIBuffer,INDICATOR_DATA);
//---- превращение динамического массива TriggerBuffer[] в индикаторный буфер
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- инициализации переменной для короткого имени индикатора
   string shortname="SMI_Correct";
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- количество  горизонтальных уровней индикатора 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MiddleLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- в качестве цветов линий горизонтальных уровней использованы серый и розовый цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,HighLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,MiddleLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,LowLevelsColor);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//----
   return(0);
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
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(RESET);

//---- объявления локальных переменных 
   int first,bar,barx;
   double HH,LL,price,SM,HQ,XSM,XHQ,XXSM,XXHQ;

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=min_rates_1;                   // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров

//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      HH=-9999999999.0;
      LL=+9999999999.0;
      for(int index=0; index<int(ExtrPeriod); index++)
       {
         barx=bar-index;
         if(high[barx]>HH) HH=high[barx];
         if(low[barx]<LL) LL=low[barx];
       }
      price=PriceSeries(IPC,bar,open,low,high,close);
      SM=price-(HH+LL)/2.0;
      HQ=HH-LL;
      XSM=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,MA_Method1,Phase1,Length1,SM,bar,false);
      XHQ=XMA2.XMASeries(min_rates_1,prev_calculated,rates_total,MA_Method1,Phase1,Length1,HQ,bar,false);     
      XXSM=XMA3.XMASeries(min_rates_2,prev_calculated,rates_total,MA_Method2,Phase2,Length2,XSM,bar,false);
      XXHQ=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,MA_Method2,Phase2,Length2,XHQ,bar,false);
      if(XXHQ) SMIBuffer[bar]=100.0*(XXSM/(XXHQ/2)); 
      else SMIBuffer[bar]=100.0;
      TriggerBuffer[bar]=XMA5.XMASeries(min_rates_3,prev_calculated,rates_total,MA_Method3,Phase3,Length3,SMIBuffer[bar],bar,false);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
