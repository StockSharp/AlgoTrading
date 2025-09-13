//+------------------------------------------------------------------+
//|                                                AroonHornSign.mq5 |
//|                                        Copyright © 2011, tonyc2a | 
//|                                         mailto:tonyc2a@yahoo.com | 
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2011, tonyc2a"
//---- ссылка на сайт автора
#property link "mailto:tonyc2a@yahoo.com"
//---- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Объявление констант                         |
//+----------------------------------------------+
#define RESET 0       // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован Crimson цвет
#property indicator_color1  clrCrimson
//--- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//--- отображение бычей метки индикатора
#property indicator_label1  "AroonHornSign Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычей линии индикатора использован LimeGreen цвет
#property indicator_color2  clrLimeGreen
//--- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//--- отображение медвежьей метки индикатора
#property indicator_label2 "AroonHornSign Buy"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint AroonPeriod= 9; // период индикатора 
input int AroonShift = 0; // сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double BullsAroonBuffer[];
double BearsAroonBuffer[];
//---- объявление целочисленных переменных для хендлов индикаторов
int ATR_Handle;
//---- Объявление целых переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- Инициализация переменных начала отсчета данных   
   int ATR_Period=10;
   min_rates_total=int(MathMax(AroonPeriod,ATR_Period));
//---- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     } 
//---- превращение динамического массива BullsAroonBuffer в индикаторный буфер
   SetIndexBuffer(0,BearsAroonBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на AroonShift
   PlotIndexSetInteger(0,PLOT_SHIFT,AroonShift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 1 на AroonPeriod
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,AroonPeriod);

//---- превращение динамического массива BearsAroonBuffer в индикаторный буфер
   SetIndexBuffer(1,BullsAroonBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на AroonShift
   PlotIndexSetInteger(1,PLOT_SHIFT,AroonShift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 2 на AroonPeriod
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,AroonPeriod);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"AroonHornSign(",AroonPeriod,", ",AroonShift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчёта индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчёта индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- объявления локальных переменных 
   int first,bar,trend;
   static int trend_prev;
   double BULLS,BEARS,ATR[1];

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=min_rates_total-1; // стартовый номер для расчёта всех баров
      trend_prev=0;
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
   

//---- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int barx=rates_total-bar-1;
      //---- вычисление индикаторных значений
      BULLS=NormalizeDouble(100-(ArrayMaximum(high,barx,AroonPeriod)-barx+0.5)*100/AroonPeriod,0);
      BEARS=NormalizeDouble(100-(ArrayMinimum(low,barx,AroonPeriod)-barx+0.5)*100/AroonPeriod,0);
      BullsAroonBuffer[bar]=0;
      BearsAroonBuffer[bar]=0;
      trend=trend_prev;
      if(BULLS>BEARS && BULLS>=50) trend=+1;
      if(BULLS<BEARS && BEARS>=50) trend=-1;
      if(trend_prev<0 && trend>0)
       {
         //---- копируем вновь появившиеся данные в массив
         if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
         BullsAroonBuffer[bar]=low[barx]-ATR[0]*3/8;
       }
      if(trend_prev>0 && trend<0)
       {         
         //---- копируем вновь появившиеся данные в массив
         if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
         BearsAroonBuffer[bar]=high[barx]+ATR[0]*3/8;
       }
       
      if(bar<rates_total-1) trend_prev=trend;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
