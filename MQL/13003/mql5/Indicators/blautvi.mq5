//+---------------------------------------------------------------------+
//|                                                         BlauTVI.mq5 |
//|                                  Copyright © 2013, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2013, Nikolay Kositsin"
//---- ссылка на сайт автора
#property link "farria@mail.redcom.ru" 
#property description "Tick Volume Index"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора 2             |
//+----------------------------------------------+
//---- отрисовка индикатора в виде пятицветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов использованы
#property indicator_color1 clrMagenta,clrOrange,clrGray,clrDeepSkyBlue,clrBlue
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1  "Tick Volume Index"
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5;
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
input Smooth_Method XMA_Method=MODE_EMA;           // Метод усреднения
input uint XLength1=12;                            // Глубина первого усреднения
input uint XLength2=12;                            // Глубина второго усреднения
input uint XLength3=5;                             // Глубина третьего усреднения
input int XPhase=15;                               // Параметр сглаживания
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
//---- для VIDIA это период CMO, для AMA это период медленной скользящей
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // Объем
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//---- объявление целочисленных переменных начала отсчёта данных
int min_rates_total,min_rates_1,min_rates_2;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчёта данных
   min_rates_1=XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,4);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"Tick Volume Index");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,4);
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
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);
//---- объявления локальных переменных 
   double UpTicks,XUpTicks,XXUpTicks,DnTicks,XDnTicks,XXDnTicks,TVI_Raw,XTVI_Raw;
   int first,bar;
   long Vol;
//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=min_rates_1; // стартовый номер для расчёта всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//---- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      if(VolumeType==VOLUME_TICK) Vol=tick_volume[bar];
      else Vol=volume[bar];
      //---
      UpTicks=(Vol+(close[bar]-open[bar])/_Point)/2;
      DnTicks=Vol-UpTicks;
      XUpTicks=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,UpTicks,bar,false);
      XDnTicks=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,DnTicks,bar,false);
      XXUpTicks=XMA3.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,XUpTicks,bar,false);
      XXDnTicks=XMA4.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,XDnTicks,bar,false);
      TVI_Raw=100.0*(XXUpTicks-XXDnTicks)/(XXUpTicks+XXDnTicks);
      XTVI_Raw=XMA5.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,TVI_Raw,bar,false);
      IndBuffer[bar]=XTVI_Raw;
     }
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//---- основной цикл раскраски индикатора Ind
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;
      //---
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }
      //---
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }
      //---
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
