//+------------------------------------------------------------------+ 
//|                                             ColorZerolagTriX.mq5 | 
//|                               Copyright © 2011, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//---- авторство индикатора
#property copyright "Copyright © 2011, Nikolay Kositsin"
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
//----
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
//---- в качестве цветов заливки индикатора использованы темно-голубой и красный цвета
#property indicator_color3  clrDodgerBlue,clrDeepPink
//---- отображение метки индикатора
#property indicator_label3 "ZerolagTriX"
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint    smoothing=15;
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // Ценовая константа
//----
input double Factor1=0.05;
input uint    TriX_period1=8;
//----
input double Factor2=0.10;
input uint    TriX_period2=21;
//----
input double Factor3=0.16;
input uint    TriX_period3=34;
//----
input double Factor4=0.26;
input int    TriX_period4=55;
//----
input double Factor5=0.43;
input uint    TriX_period5=89;
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
int TriX1_Handle,TriX2_Handle,TriX3_Handle,TriX4_Handle,TriX5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagTriX indicator initialization function                    | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация констант
   smoothConst=(smoothing-1.0)/smoothing;
//---- 
   uint PeriodBuffer[5];
//---- расчет стартового бара
   PeriodBuffer[0] = TriX_period1;
   PeriodBuffer[1] = TriX_period2;
   PeriodBuffer[2] = TriX_period3;
   PeriodBuffer[3] = TriX_period4;
   PeriodBuffer[4] = TriX_period5;
//----
   StartBar=int(3*PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)])+2;
//---- получение хендла индикатора iTriX1
   TriX1_Handle=iTriX(NULL,0,TriX_period1,IPC);
   if(TriX1_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX1");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iTriX2
   TriX2_Handle=iTriX(NULL,0,TriX_period2,IPC);
   if(TriX2_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX2");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iTriX3
   TriX3_Handle=iTriX(NULL,0,TriX_period3,IPC);
   if(TriX3_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX3");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iTriX4
   TriX4_Handle=iTriX(NULL,0,TriX_period4,IPC);
   if(TriX4_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX4");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iTriX5
   TriX5_Handle=iTriX(NULL,0,TriX_period5,IPC);
   if(TriX5_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX5");
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
//----
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
//----
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,FastBuffer_,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(FastBuffer_,true);
//----
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
//---- инициализации переменной для короткого имени индикатора
   string shortname="ZerolagTriX";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,6);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagTriX iteration function                                   | 
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
   if(BarsCalculated(TriX1_Handle)<rates_total
      || BarsCalculated(TriX2_Handle)<rates_total
      || BarsCalculated(TriX3_Handle)<rates_total
      || BarsCalculated(TriX4_Handle)<rates_total
      || BarsCalculated(TriX5_Handle)<rates_total
      || rates_total<StartBar)
      return(0);
//---- объявление переменных с плавающей точкой  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend;
   double TriX1[],TriX2[],TriX3[],TriX4[],TriX5[];
//---- объявление целых переменных
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
   ArraySetAsSeries(TriX1,true);
   ArraySetAsSeries(TriX2,true);
   ArraySetAsSeries(TriX3,true);
   ArraySetAsSeries(TriX4,true);
   ArraySetAsSeries(TriX5,true);
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(TriX1_Handle,0,0,to_copy,TriX1)<=0) return(0);
   if(CopyBuffer(TriX2_Handle,0,0,to_copy,TriX2)<=0) return(0);
   if(CopyBuffer(TriX3_Handle,0,0,to_copy,TriX3)<=0) return(0);
   if(CopyBuffer(TriX4_Handle,0,0,to_copy,TriX4)<=0) return(0);
   if(CopyBuffer(TriX5_Handle,0,0,to_copy,TriX5)<=0) return(0);
//---- расчет стартового номера limit для цикла пересчета баров и стартовая инициализация переменных
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      bar=limit+1;
      Osc1 = Factor1 * TriX1[bar];
      Osc2 = Factor2 * TriX2[bar];
      Osc3 = Factor2 * TriX3[bar];
      Osc4 = Factor4 * TriX4[bar];
      Osc5 = Factor5 * TriX5[bar];
      //----
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      FastBuffer[bar]=FastBuffer_[bar]=FastTrend;
      SlowBuffer[bar]=SlowBuffer_[bar]=FastTrend/smoothing;
     }
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * TriX1[bar];
      Osc2 = Factor2 * TriX2[bar];
      Osc3 = Factor2 * TriX3[bar];
      Osc4 = Factor4 * TriX4[bar];
      Osc5 = Factor5 * TriX5[bar];
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
