//+---------------------------------------------------------------------+
//|                                                             CHO.mq5 |
//|                         Copyright © 2007, MetaQuotes Software Corp. |
//|                                           http://www.metaquotes.net |
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл xrangeAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 1
#property indicator_buffers 1 
//---- использовано одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован DarkTurquoise цвет
#property indicator_color1 clrDarkTurquoise
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "CHO"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//| Объявление перечислений           |
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
//| Объявление констант               |
//+-----------------------------------+
#define RESET 0 // константа для возврата терминалу команды на пересчет индикатора
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input Smooth_Method XMA_Method=MODE_SMA;     // Метод усреднения
input uint FastPeriod=3;                     // Период быстрого усреднения
input uint SlowPeriod=10;                    // Метод медленного усреднения
input int XPhase=15;                         // Параметр сглаживания
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // Объем
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ExtBuffer[];
//---- объявление целочисленных переменных для хранения хендлов индикаторов
int Ind_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int  min_rates_,min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_=2;
   int min_rates_1=XMA1.GetStartBars(XMA_Method,FastPeriod,XPhase);
   int min_rates_2=XMA1.GetStartBars(XMA_Method,SlowPeriod,XPhase);
   min_rates_total=min_rates_+int(MathMax(min_rates_1,min_rates_2));
//--- получение хендла индикатора iAD
   Ind_Handle=iAD(Symbol(),PERIOD_CURRENT,VolumeType);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iAD");
      return(INIT_FAILED);
     }
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtBuffer,true);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"CHO");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
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
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total || BarsCalculated(Ind_Handle)<rates_total) return(RESET);
//---- объявления локальных переменных 
   int to_copy,limit,bar,maxbar;
//---- объявление переменных с плавающей точкой  
   double AD[],Fast,Slow;
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(AD,true);
//----   
   maxbar=rates_total-min_rates_-1;
//---- расчеты необходимого количества копируемых данных и
//---- стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=maxbar; // стартовый номер для расчета всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//----   
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(Ind_Handle,0,0,to_copy,AD)<=0) return(RESET);
//---- первый цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Fast=XMA1.XMASeries(maxbar,prev_calculated,rates_total,XMA_Method,XPhase,FastPeriod,AD[bar],bar,true);
      Slow=XMA2.XMASeries(maxbar,prev_calculated,rates_total,XMA_Method,XPhase,SlowPeriod,AD[bar],bar,true);
      ExtBuffer[bar]=Fast-Slow;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
