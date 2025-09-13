//+------------------------------------------------------------------+ 
//|                                              ColorZerolagRVI.mq5 | 
//|                               Copyright © 2011, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//---- авторство индикатора
#property copyright "Copyright © 2011, Nikolay Kositsin"
//---- ссылка на сайт автора
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.00"
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
//---- в качестве цветов заливки индикатора использованы салатовый цвет и красный цвета
#property indicator_color3  clrSpringGreen,clrRed
//---- отображение метки индикатора
#property indicator_label3 "ZerolagRVI"
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint   smoothing=15;
//----
input double Factor1=0.05;
input int    RVI_period1=8;
//----
input double Factor2=0.10;
input int    RVI_period2=21;
//----
input double Factor3=0.16;
input int    RVI_period3=34;
//----
input double Factor4=0.26;
input int    RVI_period4=55;
//----
input double Factor5=0.43;
input int    RVI_period5=89;
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
int RVI1_Handle,RVI2_Handle,RVI3_Handle,RVI4_Handle,RVI5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagRVI indicator initialization function                     | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация констант
   smoothConst=(smoothing-1.0)/smoothing;
//----
   int PeriodBuffer[5];
//---- расчет стартового бара
   PeriodBuffer[0] = RVI_period1;
   PeriodBuffer[1] = RVI_period2;
   PeriodBuffer[2] = RVI_period3;
   PeriodBuffer[3] = RVI_period4;
   PeriodBuffer[4] = RVI_period5;
//----
   StartBar=PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)]+2;
//---- получение хендла индикатора iRVI1
   RVI1_Handle=iRVI(NULL,0,RVI_period1);
   if(RVI1_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора iRVI1");
//---- получение хендла индикатора iRVI2
   RVI2_Handle=iRVI(NULL,0,RVI_period2);
   if(RVI2_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора iRVI2");
//---- получение хендла индикатора iRVI3
   RVI3_Handle=iRVI(NULL,0,RVI_period3);
   if(RVI3_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора iRVI3");
//---- получение хендла индикатора iRVI4
   RVI4_Handle=iRVI(NULL,0,RVI_period4);
   if(RVI4_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора iRVI4");
//---- получение хендла индикатора iRVI5
   RVI5_Handle=iRVI(NULL,0,RVI_period5);
   if(RVI5_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора iRVI5");
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
//---- инициализации переменной для короткого имени индикатора
   string shortname="ZerolagRVI";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,4);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| ZerolagRVI iteration function                                    | 
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
   if(BarsCalculated(RVI1_Handle)<rates_total
      || BarsCalculated(RVI2_Handle)<rates_total
      || BarsCalculated(RVI3_Handle)<rates_total
      || BarsCalculated(RVI4_Handle)<rates_total
      || BarsCalculated(RVI5_Handle)<rates_total
      || rates_total<StartBar)
      return(0);
//---- объявление переменных с плавающей точкой  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend;
   double RVI1[],RVI2[],RVI3[],RVI4[],RVI5[];
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
   ArraySetAsSeries(RVI1,true);
   ArraySetAsSeries(RVI2,true);
   ArraySetAsSeries(RVI3,true);
   ArraySetAsSeries(RVI4,true);
   ArraySetAsSeries(RVI5,true);
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(RVI1_Handle,0,0,to_copy,RVI1)<=0) return(0);
   if(CopyBuffer(RVI2_Handle,0,0,to_copy,RVI2)<=0) return(0);
   if(CopyBuffer(RVI3_Handle,0,0,to_copy,RVI3)<=0) return(0);
   if(CopyBuffer(RVI4_Handle,0,0,to_copy,RVI4)<=0) return(0);
   if(CopyBuffer(RVI5_Handle,0,0,to_copy,RVI5)<=0) return(0);
//---- расчеты необходимого количества копируемых данных,
//---- стартового номера limit для цикла пересчета баров
//---- и стартовая инициализация переменных
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      bar=limit+1;
      Osc1 = Factor1 * RVI1[bar];
      Osc2 = Factor2 * RVI2[bar];
      Osc3 = Factor2 * RVI3[bar];
      Osc4 = Factor4 * RVI4[bar];
      Osc5 = Factor5 * RVI5[bar];
      //---
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      FastBuffer[bar]=FastBuffer_[bar]=FastTrend;
      SlowBuffer[bar]=SlowBuffer_[bar]=FastTrend/smoothing;
     }
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * RVI1[bar];
      Osc2 = Factor2 * RVI2[bar];
      Osc3 = Factor2 * RVI3[bar];
      Osc4 = Factor4 * RVI4[bar];
      Osc5 = Factor5 * RVI5[bar];
      //---
      FastTrend = Osc1 + Osc2 + Osc3 + Osc4 + Osc5;
      SlowTrend = FastTrend / smoothing + SlowBuffer[bar + 1] * smoothConst;
      //---
      SlowBuffer[bar]=SlowTrend;
      FastBuffer[bar]=FastTrend;

      SlowBuffer_[bar]=SlowTrend;
      FastBuffer_[bar]=FastTrend;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
