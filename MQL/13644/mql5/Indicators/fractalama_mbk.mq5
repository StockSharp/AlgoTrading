//+----------------------------------------------------------------------------------+
//| FractalAMA                                                                       |
//|                                                                                  |
//| Description:  Fractal Adaptive Moving Average - by John Ehlers                   |
//|               Version 1.1 7/17/2006                                              |
//|                                                                                  |
//| Heavily modified and reprogrammed by Matt Kennel (mbkennelfx@gmail.com)          |
//|                                                                                  |
//| Notes:                                                                           |
//|               October 2005 Issue - "FRAMA - Fractal Adaptive Moving Average"     |
//|               Length will be forced to be an even number.                        |
//|               Odd numbers will be bumped up to the                               |
//|               next even number.                                                  |
//| Formula Parameters:     Defaults:                                                |
//| RPeriod                 16                                                       |
//+----------------------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2005, MrPip"
//---- авторство индикатора
#property link      "mbkennelfx@gmail.com"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора FractalAMA    |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован DarkOrange цвет
#property indicator_color1  clrDarkOrange
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "FractalAMA"
//+----------------------------------------------+
//| Параметры отрисовки индикатора Trigger       |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета линии индикатора использован SlateBlue цвет
#property indicator_color2  clrSlateBlue
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//---- отображение метки индикатора
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint RPeriod=16;
input double multiplier=4.6;
input double signal_multiplier=2.5;
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double FrAmaBuffer[];
double TriggerBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,N;
//+------------------------------------------------------------------+
//| Range()                                                          |
//+------------------------------------------------------------------+   
double Range(int index,const double &Low[],const double &High[],int period)
  {
//----
   return(High[ArrayMaximum(High,index,period)]-Low[ArrayMinimum(Low,index,period)]);
  }
//+------------------------------------------------------------------+
//| DEst()                                                           |
//+------------------------------------------------------------------+   
double DEst(int index,const double &Low[],const double &High[],int period)
  {
//----
   double R1,R2,R3;
   int n2=period/2;
//----
   R3=Range(index,Low,High,period)/period;
   R1=Range(index,Low,High,n2)/n2;
   R2=Range(index+n2,Low,High,n2)/n2;
//----
   return((MathLog(R1+R2)-MathLog(R3)) *1.442695);// log_2(e) = 1.442694
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(RPeriod);
   N=int(MathFloor(RPeriod/2)*2);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,FrAmaBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(FrAmaBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2 на min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(TriggerBuffer,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"FractalAMA(",RPeriod,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   if(rates_total<min_rates_total) return(0);
//---- объявления локальных переменных 
   int limit,bar;
   double dimension_estimate,alpha,alphas;
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
      int start=limit+1;
      FrAmaBuffer[start]=close[start];
      TriggerBuffer[start]=close[start];
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      dimension_estimate=DEst(bar,low,high,N);
      alpha=MathExp(-multiplier*(dimension_estimate-1.0));
      alphas=MathExp(-signal_multiplier*(dimension_estimate-1.0));
      //----
      alpha=MathMin(alpha,1.0);
      alpha=MathMax(alpha,0.01);
      //----
      FrAmaBuffer[bar]=alpha*close[bar]+(1.0-alpha) *FrAmaBuffer[bar+1];
      TriggerBuffer[bar]=alphas*FrAmaBuffer[bar]+(1.0-alphas)*TriggerBuffer[bar+1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
