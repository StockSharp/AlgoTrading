//+------------------------------------------------------------------+ 
//|                                         ColorZerolagDeMarker.mq5 | 
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
//|  Параметры отрисовки индикатора   |
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
//|  Параметры отрисовки заливки      |
//+-----------------------------------+
//---- отрисовка индикатора в виде заливки между двумя линиями
#property indicator_type3   DRAW_FILLING
//---- в качестве цветов заливки индикатора использованы DodgerBlue и Red цвета
#property indicator_color3  clrDodgerBlue,clrRed
//---- отображение метки индикатора
#property indicator_label3 "ZerolagDeMarker"
//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input uint    smoothing=15;
//----
input double Factor1=0.05;
input uint    DeMarker_period1=8;
//----
input double Factor2=0.10;
input uint    DeMarker_period2=21;
//----
input double Factor3=0.16;
input uint    DeMarker_period3=34;
//----
input double Factor4=0.26;
input int    DeMarker_period4=55;
//----
input double Factor5=0.43;
input uint    DeMarker_period5=89;
//+-----------------------------------+

//---- Объявление целых переменных начала отсчёта данных
int StartBar;
//---- Объявление переменных с плавающей точкой
double smoothConst;
//---- индикаторные буферы
double FastBuffer[];
double SlowBuffer[];
double FastBuffer_[];
double SlowBuffer_[];
//---- Объявление переменных для хранения хендлов индикаторов
int DeMarker1_Handle,DeMarker2_Handle,DeMarker3_Handle,DeMarker4_Handle,DeMarker5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagDeMarker indicator initialization function                | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- Инициализация констант
   smoothConst=(smoothing-1.0)/smoothing;
//---- 
   uint PeriodBuffer[5];
//---- Расчёт стартового бара
   PeriodBuffer[0] = DeMarker_period1;
   PeriodBuffer[1] = DeMarker_period2;
   PeriodBuffer[2] = DeMarker_period3;
   PeriodBuffer[3] = DeMarker_period4;
   PeriodBuffer[4] = DeMarker_period5;
//----
   StartBar=int(3*PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)])+2;

//---- получение хендла индикатора iDeMarker1
   DeMarker1_Handle=iDeMarker(NULL,0,DeMarker_period1);
   if(DeMarker1_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iDeMarker1");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iDeMarker2
   DeMarker2_Handle=iDeMarker(NULL,0,DeMarker_period2);
   if(DeMarker2_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iDeMarker2");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iDeMarker3
   DeMarker3_Handle=iDeMarker(NULL,0,DeMarker_period3);
   if(DeMarker3_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iDeMarker3");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iDeMarker4
   DeMarker4_Handle=iDeMarker(NULL,0,DeMarker_period4);
   if(DeMarker4_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iDeMarker4");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iDeMarker5
   DeMarker5_Handle=iDeMarker(NULL,0,DeMarker_period5);
   if(DeMarker5_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iDeMarker5");
      return(INIT_FAILED);
     }

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,FastBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBar);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"FastTrendLine");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(FastBuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,SlowBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 2
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

//---- осуществление сдвига начала отсчёта отрисовки индикатора 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBar);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"FastTrendLine");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- инициализации переменной для короткого имени индикатора
   string shortname="ZerolagDeMarker";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagDeMarker iteration function                               | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(DeMarker1_Handle)<rates_total
      || BarsCalculated(DeMarker2_Handle)<rates_total
      || BarsCalculated(DeMarker3_Handle)<rates_total
      || BarsCalculated(DeMarker4_Handle)<rates_total
      || BarsCalculated(DeMarker5_Handle)<rates_total
      || rates_total<StartBar)
      return(0);

//---- Объявление переменных с плавающей точкой  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend;
   double DeMarker1[],DeMarker2[],DeMarker3[],DeMarker4[],DeMarker5[];

//---- Объявление целых переменных
   int limit,to_copy,bar;

//---- расчёт стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-StartBar-2; // стартовый номер для расчёта всех баров
      to_copy=limit+2;
     }
   else // стартовый номер для расчёта новых баров
     {
      limit=rates_total-prev_calculated;  // стартовый номер для расчёта только новых баров
      to_copy=limit+1;
     }

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(DeMarker1,true);
   ArraySetAsSeries(DeMarker2,true);
   ArraySetAsSeries(DeMarker3,true);
   ArraySetAsSeries(DeMarker4,true);
   ArraySetAsSeries(DeMarker5,true);

//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(DeMarker1_Handle,0,0,to_copy,DeMarker1)<=0) return(0);
   if(CopyBuffer(DeMarker2_Handle,0,0,to_copy,DeMarker2)<=0) return(0);
   if(CopyBuffer(DeMarker3_Handle,0,0,to_copy,DeMarker3)<=0) return(0);
   if(CopyBuffer(DeMarker4_Handle,0,0,to_copy,DeMarker4)<=0) return(0);
   if(CopyBuffer(DeMarker5_Handle,0,0,to_copy,DeMarker5)<=0) return(0);

//---- расчёт стартового номера limit для цикла пересчёта баров и стартовая инициализация переменных
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      bar=limit+1;
      Osc1 = Factor1 * DeMarker1[bar];
      Osc2 = Factor2 * DeMarker2[bar];
      Osc3 = Factor2 * DeMarker3[bar];
      Osc4 = Factor4 * DeMarker4[bar];
      Osc5 = Factor5 * DeMarker5[bar];

      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      FastBuffer[bar]=FastBuffer_[bar]=FastTrend;
      SlowBuffer[bar]=SlowBuffer_[bar]=FastTrend/smoothing;
     }

//---- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * DeMarker1[bar];
      Osc2 = Factor2 * DeMarker2[bar];
      Osc3 = Factor2 * DeMarker3[bar];
      Osc4 = Factor4 * DeMarker4[bar];
      Osc5 = Factor5 * DeMarker5[bar];

      FastTrend = Osc1 + Osc2 + Osc3 + Osc4 + Osc5;
      SlowTrend = FastTrend / smoothing + SlowBuffer[bar + 1] * smoothConst;

      SlowBuffer[bar]=SlowTrend;
      FastBuffer[bar]=FastTrend;

      SlowBuffer_[bar]=SlowTrend;
      FastBuffer_[bar]=FastTrend;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
