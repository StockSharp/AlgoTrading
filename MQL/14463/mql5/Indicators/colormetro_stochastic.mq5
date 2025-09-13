//+------------------------------------------------------------------+
//|                                        ColorMETRO_Stochastic.mq5 | 
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
//+--------------------------------------------------+
//| Параметры отрисовки облака  StepStochastic cloud |
//+--------------------------------------------------+
//---- отрисовка индикатора 1 в виде облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цвета индикатора использованы цвета MediumSeaGreen,Tomato
#property indicator_color1  clrMediumSeaGreen,clrTomato
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "StepStochastic cloud"
//+--------------------------------------------------+
//| Параметры отрисовки индикатора Stochastic        |
//+--------------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета линии индикатора использован цвет SlateBlue
#property indicator_color2  clrSlateBlue
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение метки индикатора
#property indicator_label2  "Stochastic"
//+--------------------------------------------------+
//| Параметры отображения горизонтальных уровней     |
//+--------------------------------------------------+
#property indicator_level1  70
#property indicator_level2  50
#property indicator_level3  30
#property indicator_levelcolor clrMediumOrchid
#property indicator_levelstyle STYLE_DASHDOTDOT
//+--------------------------------------------------+
//| Входные параметры индикатора                     |
//+--------------------------------------------------+
input uint KPeriod=5;
input uint DPeriod=3;
input int Slowing=3;
input ENUM_MA_METHOD MA_Method=MODE_SMA;
input int StepSizeFast=5;                                 // Быстрый шаг
input int StepSizeSlow=15;                                // Медленный шаг
input int Shift=0;                                        // Сдвиг индикатора по горизонтали в барах 
input ENUM_STO_PRICE Applied_price=STO_LOWHIGH;           // Тип цены или handle
//+--------------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double Line1Buffer[];
double Line2Buffer[];
double Line3Buffer[];
//---- объявление целочисленных переменных для хендлов индикаторов
int Stochastic_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(KPeriod+DPeriod+Slowing);
//---- получение хендла индикатора Stochastic
   Stochastic_Handle=iStochastic(NULL,0,KPeriod,DPeriod,Slowing,MA_Method,Applied_price);
   if(Stochastic_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Stochastic");
      return(INIT_FAILED);
     }
//---- превращение динамического массива Line1Buffer[] в индикаторный буфер
   SetIndexBuffer(0,Line1Buffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(Line1Buffer,true);

//---- превращение динамического массива Line2Buffer[] в индикаторный буфер
   SetIndexBuffer(1,Line2Buffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2 на min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(Line2Buffer,true);

//---- превращение динамического массива Line3Buffer[] в индикаторный буфер
   SetIndexBuffer(2,Line3Buffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 3 по горизонтали на Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 3 на min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(Line3Buffer,true);

//---- инициализация переменной для короткого имени индикатора
   string shortname="METRO_Stochastic";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
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
   if(BarsCalculated(Stochastic_Handle)<rates_total || rates_total<min_rates_total) return(0);
//---- объявление локальных переменных 
   int limit,to_copy,bar,ftrend,strend;
   double fmin0,fmax0,smin0,smax0,Stochastic0;
   static double fmax1,fmin1,smin1,smax1;
   static int ftrend_,strend_;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      limit=rates_total-1; // стартовый номер для расчета всех баров
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
   if(CopyBuffer(Stochastic_Handle,1,0,to_copy,Line3Buffer)<=0) return(0);
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
      Stochastic0=Line3Buffer[bar]=Line3Buffer[bar];
      //----
      fmax0=Stochastic0+2*StepSizeFast;
      fmin0=Stochastic0-2*StepSizeFast;
      //----
      if(Stochastic0>fmax1)  ftrend=+1;
      if(Stochastic0<fmin1)  ftrend=-1;
      //----
      if(ftrend>0 && fmin0<fmin1) fmin0=fmin1;
      if(ftrend<0 && fmax0>fmax1) fmax0=fmax1;
      //----
      smax0=Stochastic0+2*StepSizeSlow;
      smin0=Stochastic0-2*StepSizeSlow;
      //----
      if(Stochastic0>smax1)  strend=+1;
      if(Stochastic0<smin1)  strend=-1;
      //----
      if(strend>0 && smin0<smin1) smin0=smin1;
      if(strend<0 && smax0>smax1) smax0=smax1;
      //----
      if(ftrend>0) Line1Buffer[bar]=fmin0+StepSizeFast;
      if(ftrend<0) Line1Buffer[bar]=fmax0-StepSizeFast;
      if(strend>0) Line2Buffer[bar]=smin0+StepSizeSlow;
      if(strend<0) Line2Buffer[bar]=smax0-StepSizeSlow;
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
