//+------------------------------------------------------------------+
//|                                              FisherTransform.mq5 |
//|                                                                  |
//| Fisher Transform                                                 |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Coded by Witold Wozniak"
//---- авторство индикатора
#property link      "www.mqlsoft.com"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Параметры отрисовки индикатора Fisher       |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета бычей линии индикатора использован красный цвет
#property indicator_color1  Red
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//---- отображение бычей метки индикатора
#property indicator_label1  "Fisher"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора Trigger      |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета медвежьей линии индикатора использован синий цвет
#property indicator_color2  Blue
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//---- отображение медвежьей метки индикатора
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int Length=10; // период индикатора 
input int Shift=0; // сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double FisherBuffer[];
double TriggerBuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,FisherBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 1 на Length
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,Length);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 2 на Length
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,Length);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"FisherTransform(",Length,", ",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
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
   if(rates_total<Length) return(0);

//---- объявления локальных переменных 
   int first,bar,kkk;
   double price,price1,MaxH,MinL,Value;
   static double Value_;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=Length-1; // стартовый номер для расчёта всех баров
      Value_=0.0;
      FisherBuffer[first-1]=0.0;
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- восстанавливаем значения переменных
   Value=Value_;

//---- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==rates_total-1)
         Value_=Value;

      price=(high[bar]+low[bar])/2.0;
      MaxH = price;
      MinL = price;

      for(int iii=0; iii<Length; iii++)
        {
         kkk=bar-iii;
         price1=(high[kkk]+low[kkk])/2.0;
         if(price1 > MaxH) MaxH = price1;
         if(price1 < MinL) MinL = price1;
        }

      double res=MaxH-MinL;
      if(res) Value=0.5*2.0 *((price-MinL)/res-0.5)+0.5*Value;
      else Value=0.0;

      if(Value>+0.9999) Value=+0.9999;
      if(Value<-0.9999) Value=-0.9999;

      FisherBuffer[bar]=0.25*MathLog((1+Value)/(1-Value))+0.5*FisherBuffer[bar-1];
      TriggerBuffer[bar]=FisherBuffer[bar-1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
