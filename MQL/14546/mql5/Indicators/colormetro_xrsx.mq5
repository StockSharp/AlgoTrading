//+------------------------------------------------------------------+
//|                                              ColorMETRO_XRSX.mq5 | 
//|                           Copyright © 2005, TrendLaboratory Ltd. |
//|                                       E-mail: igorad2004@list.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, TrendLaboratory Ltd."
#property link      "E-mail: igorad2004@list.ru"
#property description "METRO"
//---- номер версии индикатора
#property version   "1.10"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 3
#property indicator_buffers 3 
//---- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора StepXRSX      |
//+----------------------------------------------+
//---- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветов облака индикатора использованы
#property indicator_color1  clrLime,clrDeepPink
//---- отображение метки индикатора
#property indicator_label1  "StepXRSX Cloud"
//+----------------------------------------------+
//| Параметры отрисовки индикатора XRSX          |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета линии индикатора использован цвет Blue
#property indicator_color2  clrBlue
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 3
#property indicator_width2  3
//---- отображение метки индикатора
#property indicator_label2  "XRSX"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1  70
#property indicator_level2  50
#property indicator_level3  30
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA UPXRSX,DNXRSX,XSIGN;
//+----------------------------------------------+
//| Объявление перечислений                      |
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
//| Объявление перечислений                      |
//+----------------------------------------------+
/*enum Smooth_Method - объявлено в файле SmoothAlgorithms.mqh
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
input Smooth_Method DSmoothMethod=MODE_JJMA; // Метод усреднения цены
input int DPeriod=15;  // Период мувинга
input int DPhase=100;  // Параметр усреднения мувинга,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- для VIDIA это период CMO, для AMA это период медленной скользящей
input int StepSizeFast=5;                             // Быстрый шаг
input int StepSizeSlow=15;                            // Медленный шаг
input Applied_price_ IPC=PRICE_CLOSE; // Ценовая константа
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double Line1Buffer[];
double Line2Buffer[];
double Line3Buffer[];
//---- объявление целочисленных переменных для хендлов индикаторов
int XRSX_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=GetStartBars(DSmoothMethod,DPeriod,DPhase)+1;
//---- получение хендла индикатора XRSX
   XRSX_Handle=iCustom(NULL,0,"XRSX",DSmoothMethod,DPeriod,DPhase,1,1,1,IPC,0,1);
   if(XRSX_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора XRSX");
      return(INIT_FAILED);
     }
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,Line2Buffer,INDICATOR_DATA);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(Line2Buffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,Line3Buffer,INDICATOR_DATA);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(Line3Buffer,true);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,Line1Buffer,INDICATOR_DATA);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(Line1Buffer,true);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2 на min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);

//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"ColorMETRO_XRSX");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
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
   if(BarsCalculated(XRSX_Handle)<rates_total || rates_total<min_rates_total) return(0);
//---- объявление локальных переменных 
   int limit,to_copy,bar,ftrend,strend;
   double fmin0,fmax0,smin0,smax0,XRSX0,XRSX[];
   static double fmax1,fmin1,smin1,smax1;
   static int ftrend_,strend_;
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(XRSX,true);
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      limit=rates_total-1; // стартовый номер для расчета всех баров
      //----
      fmin1=+999999;
      fmax1=-999999;
      smin1=+999999;
      smax1=-999999;
      ftrend_=0;
      strend_=0;
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//----
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массив
   if(CopyBuffer(XRSX_Handle,0,0,to_copy,XRSX)<=0) return(0);
//---- восстанавливаем значения переменных
   ftrend = ftrend_;
   strend = strend_;
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0)
        {
         ftrend_=ftrend;
         strend_=strend;
        }
      //----
      XRSX0=(XRSX[bar]+100)/2.0;
      //----
      fmax0=XRSX0+2*StepSizeFast;
      fmin0=XRSX0-2*StepSizeFast;
      //----
      if(XRSX0>fmax1)  ftrend=+1;
      if(XRSX0<fmin1)  ftrend=-1;
      //----
      if(ftrend>0 && fmin0<fmin1) fmin0=fmin1;
      if(ftrend<0 && fmax0>fmax1) fmax0=fmax1;
      //----
      smax0=XRSX0+2*StepSizeSlow;
      smin0=XRSX0-2*StepSizeSlow;
      //----
      if(XRSX0>smax1)  strend=+1;
      if(XRSX0<smin1)  strend=-1;
      //----
      if(strend>0 && smin0<smin1) smin0=smin1;
      if(strend<0 && smax0>smax1) smax0=smax1;
      //----
      Line1Buffer[bar]=XRSX0;
      //----
      if(ftrend>0) Line2Buffer[bar]=fmin0+StepSizeFast;
      if(ftrend<0) Line2Buffer[bar]=fmax0-StepSizeFast;
      if(strend>0) Line3Buffer[bar]=smin0+StepSizeSlow;
      if(strend<0) Line3Buffer[bar]=smax0-StepSizeSlow;
      //----
      if(bar>0)
        {
         fmin1=fmin0;
         fmax1=fmax0;
         smin1=smin0;
         smax1=smax0;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
