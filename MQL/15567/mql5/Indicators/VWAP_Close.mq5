//+------------------------------------------------------------------+
//|                                                   VWAP_Close.mq5 |
//|                                            Copyright © 2016, STS | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2016, STS"
#property link ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчёта и отрисовки индикатора использован один буфер
#property indicator_buffers 1
//---- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета бычей линии индикатора использован DarkOrchid цвет
#property indicator_color1  clrDarkOrchid
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 3
#property indicator_width1  3
//---- отображение метки индикатора
#property indicator_label1  "VWAP_Close"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint n=2;
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // объём
input int Shift=0; //сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total,size;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=int(n)+1;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"VWAP_Close");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
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
   if(rates_total<min_rates_total) return(0);

//---- объявления локальных переменных 
   long vol,sum2;
   int limit,bar;
   double sum1;

//---- расчёт стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
      limit=rates_total-min_rates_total-1; // стартовый номер для расчёта всех баров
   else limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров

//---- индексация элементов в массивах как в таймсериях  
   if(VolumeType==VOLUME_TICK) ArraySetAsSeries(tick_volume,true);
   else ArraySetAsSeries(volume,true);
   ArraySetAsSeries(close,true);

//---- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      sum1=0;
      sum2=0;
      for(int ntmp=0; ntmp<int(n); ntmp++)
        {
         if(VolumeType==VOLUME_TICK) vol=long(tick_volume[bar+ntmp]);
         else vol=long(volume[bar+ntmp]);
         sum1+=close[bar+ntmp]*vol;
         sum2+=vol;
        }

      if(sum2) IndBuffer[bar]=sum1/sum2;
      else IndBuffer[bar]=IndBuffer[bar+1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
