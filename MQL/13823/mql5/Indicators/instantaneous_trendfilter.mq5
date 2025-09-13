//+------------------------------------------------------------------+
//|                                    Instantaneous_TrendFilter.mq5 |
//|                         Copyright © 2006, Luis Guilherme Damiani |
//|                                      http://www.damianifx.com.br |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2006, Luis Guilherme Damiani"
//---- авторство индикатора
#property link      "http://www.damianifx.com.br"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора ITrend        |
//+----------------------------------------------+
//---- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветов индикатора использованы
#property indicator_color1  clrMagenta,clrBlue
//---- отображение метки индикатора
#property indicator_label1  "Instantaneous_TrendFilter"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input double Alpha=0.07; // Коэффициент индикатора
input int Shift=0;       // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ITrendBuffer[];
double TriggerBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление глобальных переменных
double K0,K1,K2,K3,K4;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=4;
//---- инициализация переменных
   double A2=Alpha*Alpha;
   K0=Alpha-A2/4.0;
   K1=0.5*A2;
   K2=Alpha-0.75*A2;
   K3=2.0 *(1.0 - Alpha);
   K4=MathPow((1.0 - Alpha),2);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ITrendBuffer,INDICATOR_DATA);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Instantaneous_TrendFilter(",Alpha,", ",Shift,")");
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
                const int begin,          // номер начала достоверного отсчета баров
                const double &price[])    // ценовой массив для расчета индикатора
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total+begin) return(0);
//---- объявление локальных переменных 
   int first,bar;
   double price0,price1,price2;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=min_rates_total+begin; // стартовый номер для расчета всех баров
      //---- осуществление сдвига начала отсчета отрисовки индикатора
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
      //----
      for(bar=0; bar<first && !IsStopped(); bar++) ITrendBuffer[bar]=price[bar];
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price0=price[bar];
      price1=price[bar-1];
      price2=price[bar-2];
      //----
      if(bar<min_rates_total) ITrendBuffer[bar]=(price0+2.0*price1+price2)/4.0;
      else ITrendBuffer[bar]=K0*price0+K1*price1-K2*price2+K3*ITrendBuffer[bar-1]-K4*ITrendBuffer[bar-2];
      //----
      TriggerBuffer[bar]=2.0*ITrendBuffer[bar]-ITrendBuffer[bar-2];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
