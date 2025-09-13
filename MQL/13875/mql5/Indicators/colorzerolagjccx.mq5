//+------------------------------------------------------------------+ 
//|                                             ColorZerolagJCCX.mq5 | 
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//---- авторство индикатора
#property copyright "Copyright © 2015, Nikolay Kositsin"
//---- ссылка на сайт автора
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 2
#property indicator_buffers 4 
//---- использовано три графических построения
#property indicator_plots   3
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован сине-фиолетовый цвет
#property indicator_color1 clrBlueViolet
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1 "FastTrendLine"
//---- отрисовка индикатора в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета линии индикатора использован сине-фиолетовый цвет
#property indicator_color2 clrBlueViolet
//---- линия индикатора - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width2  1
//---- отображение метки индикатора
#property indicator_label2 "SlowTrendLine"
//+-----------------------------------+
//| Параметры отрисовки заливки       |
//+-----------------------------------+
//---- отрисовка индикатора в виде заливки между двумя линиями
#property indicator_type3   DRAW_FILLING
//---- в качестве цветов заливки индикатора использованы цвета
#property indicator_color3  clrDeepSkyBlue,clrHotPink
//---- отображение метки индикатора
#property indicator_label3 "ZerolagJCCX"
//---- параметры горизонтальных уровней индикатора
#property indicator_level1  0.5
#property indicator_level2 -0.5
#property indicator_level3  0.0
#property indicator_levelcolor clrMagenta
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
enum Applied_price_      // тип константы
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price 
  };
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint    smoothing=15;
input uint Smooth = 8;  // Глубина JJMA усреднения 
input int JPhase = 100; // Параметр JJMA усреднения
//---- изменяющийся в пределах -100 ... +100,
//---- влияет на качество переходного процесса;
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // Ценовая константа
//----
input double Factor1=0.05;
input uint    JCCX_period1=8;
//----
input double Factor2=0.10;
input uint    JCCX_period2=21;
//----
input double Factor3=0.16;
input uint    JCCX_period3=34;
//----
input double Factor4=0.26;
input int    JCCX_period4=55;
//----
input double Factor5=0.43;
input uint    JCCX_period5=89;
//+-----------------------------------+
//---- объявление целочисленных переменных начала отсчета данных
int StartBar;
//---- объявление переменных с плавающей точкой
double smoothConst;
//---- индикаторные буферы
double FastBuffer[];
double SlowBuffer[];
double FastBuffer_[];
double SlowBuffer_[];
//---- объявление переменных для хранения хендлов индикаторов
int JCCX1_Handle,JCCX2_Handle,JCCX3_Handle,JCCX4_Handle,JCCX5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagJCCX indicator initialization function                    | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация констант
   smoothConst=(smoothing-1.0)/smoothing;
//----
   StartBar=int(3*32)+2;
//---- получение хендла индикатора iJCCX1
   JCCX1_Handle=iCustom(NULL,0,"JCCX",JCCX_period1,JPhase,Smooth,IPC,0);
   if(JCCX1_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iJCCX1");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iJCCX2
   JCCX2_Handle=iCustom(NULL,0,"JCCX",JCCX_period2,JPhase,Smooth,IPC,0);
   if(JCCX2_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iJCCX2");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iJCCX3
   JCCX3_Handle=iCustom(NULL,0,"JCCX",JCCX_period3,JPhase,Smooth,IPC,0);
   if(JCCX3_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iJCCX3");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iJCCX4
   JCCX4_Handle=iCustom(NULL,0,"JCCX",JCCX_period4,JPhase,Smooth,IPC,0);
   if(JCCX4_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iJCCX4");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iJCCX5
   JCCX5_Handle=iCustom(NULL,0,"JCCX",JCCX_period5,JPhase,Smooth,IPC,0);
   if(JCCX5_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iJCCX5");
      return(INIT_FAILED);
     }
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,FastBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBar);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"FastTrendLine");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(FastBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,SlowBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBar);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"SlowTrendLine");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SlowBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,FastBuffer_,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(FastBuffer_,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,SlowBuffer_,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SlowBuffer_,true);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBar);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"FastTrendLine");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname="ZerolagJCCX";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagJCCX iteration function                                   | 
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
   if(BarsCalculated(JCCX1_Handle)<rates_total
      || BarsCalculated(JCCX2_Handle)<rates_total
      || BarsCalculated(JCCX3_Handle)<rates_total
      || BarsCalculated(JCCX4_Handle)<rates_total
      || BarsCalculated(JCCX5_Handle)<rates_total
      || rates_total<StartBar)
      return(0);
//---- объявление переменных с плавающей точкой  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend;
   double JCCX1[],JCCX2[],JCCX3[],JCCX4[],JCCX5[];
//---- объявление целочисленных переменных
   int limit,to_copy,bar;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-StartBar-2; // стартовый номер для расчета всех баров
      to_copy=limit+2;
     }
   else // стартовый номер для расчета новых баров
     {
      limit=rates_total-prev_calculated;  // стартовый номер для расчета только новых баров
      to_copy=limit+1;
     }
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(JCCX1,true);
   ArraySetAsSeries(JCCX2,true);
   ArraySetAsSeries(JCCX3,true);
   ArraySetAsSeries(JCCX4,true);
   ArraySetAsSeries(JCCX5,true);
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(JCCX1_Handle,0,0,to_copy,JCCX1)<=0) return(0);
   if(CopyBuffer(JCCX2_Handle,0,0,to_copy,JCCX2)<=0) return(0);
   if(CopyBuffer(JCCX3_Handle,0,0,to_copy,JCCX3)<=0) return(0);
   if(CopyBuffer(JCCX4_Handle,0,0,to_copy,JCCX4)<=0) return(0);
   if(CopyBuffer(JCCX5_Handle,0,0,to_copy,JCCX5)<=0) return(0);
//---- расчет стартового номера limit для цикла пересчета баров и стартовая инициализация переменных
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      bar=limit+1;
      Osc1 = Factor1 * JCCX1[bar];
      Osc2 = Factor2 * JCCX2[bar];
      Osc3 = Factor2 * JCCX3[bar];
      Osc4 = Factor4 * JCCX4[bar];
      Osc5 = Factor5 * JCCX5[bar];
      //----
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      FastBuffer[bar]=FastBuffer_[bar]=FastTrend;
      SlowBuffer[bar]=SlowBuffer_[bar]=FastTrend/smoothing;
     }
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * JCCX1[bar];
      Osc2 = Factor2 * JCCX2[bar];
      Osc3 = Factor2 * JCCX3[bar];
      Osc4 = Factor4 * JCCX4[bar];
      Osc5 = Factor5 * JCCX5[bar];
      //----
      FastTrend = Osc1 + Osc2 + Osc3 + Osc4 + Osc5;
      SlowTrend = FastTrend / smoothing + SlowBuffer[bar + 1] * smoothConst;
      //----
      SlowBuffer[bar]=SlowTrend;
      FastBuffer[bar]=FastTrend;
      //----
      SlowBuffer_[bar]=SlowTrend;
      FastBuffer_[bar]=FastTrend;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
