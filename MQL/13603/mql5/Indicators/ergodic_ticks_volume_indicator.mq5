//+---------------------------------------------------------------------+
//|                                  Ergodic_Ticks_Volume_Indicator.mq5 |
//|                                       Copyright © 2006, Profitrader | 
//|                                                profitrader@inbox.ru | 
//+---------------------------------------------------------------------+
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2006, Profitrader"
//---- ссылка на сайт автора
#property link "profitrader@inbox.ru"
#property description "Ergodic Ticks Volume Indicator"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора 1             |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован Red цвет
#property indicator_color1  clrRed
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "Ergodic_Ticks_Volume_Indicator"
//+----------------------------------------------+
//| Параметры отрисовки индикатора 2             |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета линии индикатора использован BlueViolet цвет
#property indicator_color2  clrBlueViolet
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение метки индикатора
#property indicator_label2  "Signal"
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5,XMA6,XMA7,XMA8;
//+----------------------------------------------+
//| Объявление перечислений                      |
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
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // Объем
input Smooth_Method XMA_Method=MODE_EMA; // Метод усреднения
input uint XLength1=12; // Глубина первого усреднения
input uint XLength2=12; // Глубина второго усреднения
input uint XLength3=1;  // Глубина третьего усреднения
input uint XLength4=5;  // Глубина первого усреднения
input uint XLength5=5;  // Глубина второго усреднения
input uint XLength6=5;  // Глубина третьего усреднения
input int XPhase=15; // Параметр сглаживания
                     // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                     // для VIDIA это период CMO, для AMA это период медленной скользящей
input int Shift=0; // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ETVIBuffer[],SigBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_1,min_rates_2,min_rates_3,min_rates_4,min_rates_5;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_1=XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
   min_rates_4=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength4,XPhase);
   min_rates_5=min_rates_4+XMA1.GetStartBars(XMA_Method,XLength5,XPhase);
   min_rates_total=min_rates_5+XMA1.GetStartBars(XMA_Method,XLength6,XPhase);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ETVIBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,SigBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2 на 1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"Ergodic_Ticks_Volume_Indicator");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   if(rates_total<min_rates_total) return(0);
//---- объявления локальных переменных 
   double UpTicks,DownTicks,EMA_UpTicks,EMA_DownTicks,DEMA_UpTicks;
   double DEMA_DownTicks,res,TVI_calculate,TVI,EMA_TVI,Ergodic_TVI,Ergodic_Signal;
   int first,bar;
   long Vol;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=0; // стартовый номер для расчета всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      if(VolumeType==VOLUME_TICK) Vol=long(tick_volume[bar]);
      else Vol=long(volume[bar]);
      //----
      UpTicks=(Vol+(close[bar]-open[bar])/_Point)/2;
      DownTicks=Vol-UpTicks;
      //----
      EMA_UpTicks=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,UpTicks,bar,false);
      EMA_DownTicks=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,DownTicks,bar,false);
      //----
      DEMA_UpTicks=XMA3.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,EMA_UpTicks,bar,false);
      DEMA_DownTicks=XMA4.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,EMA_DownTicks,bar,false);
      //----
      res=(DEMA_UpTicks+DEMA_DownTicks);
      //----
      if(res) TVI_calculate=100.0*(DEMA_UpTicks-DEMA_DownTicks)/res;
      else TVI_calculate=0.0;
      //----
      TVI=XMA5.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,TVI_calculate,bar,false);
      //----
      EMA_TVI=XMA6.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength4,TVI,bar,false);
      Ergodic_TVI=XMA7.XMASeries(min_rates_4,prev_calculated,rates_total,XMA_Method,XPhase,XLength5,EMA_TVI,bar,false);
      Ergodic_Signal=XMA8.XMASeries(min_rates_5,prev_calculated,rates_total,XMA_Method,XPhase,XLength6,Ergodic_TVI,bar,false);
      //----
      ETVIBuffer[bar]=Ergodic_TVI;
      SigBuffer[bar]=Ergodic_Signal;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
