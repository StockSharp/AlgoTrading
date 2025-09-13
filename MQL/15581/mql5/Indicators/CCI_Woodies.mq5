//+------------------------------------------------------------------+ 
//|                                                  CCI_Woodies.mq5 | 
//|                                        Copyright © 2013, Woodies | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
//---- авторство индикатора
#property copyright "Copyright © 2013, Woodies"
//---- авторство индикатора
#property link      ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET 0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветов облака
#property indicator_color1  clrLime,clrPlum
//---- отображение метки индикатора
#property indicator_label1  "CCI_Woodies"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint FastPeriod=6;                              // Период быстрого CCI индикатора
input ENUM_APPLIED_PRICE FastPrice=PRICE_MEDIAN;      // Ценовая константа быстрого CCI индикатора
input uint SlowPeriod=14;                             // Период медленного CCI индикатора
input ENUM_APPLIED_PRICE SlowPrice=PRICE_MEDIAN;      // Ценовая константа медленного CCI индикатора
input int Shift=0;                                    // сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double FastBuffer[];
double SlowBuffer[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//---- Объявление целых переменных для хранения хендлов индикаторов
int Fast_Handle,Slow_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=int(MathMax(FastPeriod,SlowPeriod));
   
//--- получение хендла индикатора Fast iCCI
   Fast_Handle=iCCI(Symbol(),PERIOD_CURRENT,FastPeriod,FastPrice);
   if(Fast_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Fast iCCI");
      return(INIT_FAILED);
     }
     
//--- получение хендла индикатора Slow iCCI
   Slow_Handle=iCCI(Symbol(),PERIOD_CURRENT,SlowPeriod,SlowPrice);
   if(Slow_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Slow iCCI");
      return(INIT_FAILED);
     }
    
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,FastBuffer,INDICATOR_DATA);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,SlowBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Instantaneous Trendline(",FastPeriod,", ",SlowPeriod,", ",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- количество  горизонтальных уровней индикатора 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,+100);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,0);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,-100);
//---- в качестве цветов линий горизонтальных уровней использованы цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrMagenta);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASH);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
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
   if(rates_total<min_rates_total
   || BarsCalculated(Fast_Handle)<rates_total
   || BarsCalculated(Slow_Handle)<rates_total) return(RESET);

//---- объявления локальных переменных 
   int to_copy;
   
//---- расчёты необходимого количества копируемых данных
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      to_copy=rates_total; // стартовый номер для расчёта всех баров
     }
   else to_copy=rates_total-prev_calculated+1; // стартовый номер для расчёта новых баров

//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(Fast_Handle,0,0,to_copy,FastBuffer)<=0) return(RESET);
   if(CopyBuffer(Slow_Handle,0,0,to_copy,SlowBuffer)<=0) return(RESET);
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
