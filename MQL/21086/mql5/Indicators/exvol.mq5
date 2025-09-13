//+------------------------------------------------------------------+
//|                                                        ExVol.mq5 |
//|                           Copyright © 2006, Alex Sidd (Executer) |
//|                                           mailto:work_st@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, Alex Sidd (Executer)"
#property link      "mailto:work_st@mail.ru" 
//--- номер версии индикатора
#property version   "1.01"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//--- количество индикаторных буферов 2
#property indicator_buffers 2 
//--- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//--- отрисовка индикатора в виде четырёхцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//--- в качестве цветов четырёхцветной гистограммы использованы
#property indicator_color1 clrRed,clrLightSalmon,clrGray,clrSkyBlue,clrBlue
//--- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width1 2
//--- отображение метки индикатора
#property indicator_label1 "ExVol"
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint ExPeriod=15;
//+-----------------------------------+
//--- объявление целочисленных переменных начала отсчёта данных
int min_rates_total;
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_total=int(ExPeriod);
//--- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"ExVol("+string(ExPeriod)+")");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
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
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);
///--- объявления локальных переменных 
   int first,bar;
   double;
//--- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=min_rates_total;  // стартовый номер для расчёта всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//--- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double negative=0;
      double positive=0;
      int kkk=int(bar-ExPeriod+1);
      while(kkk<=bar)
        {
         double res=(close[kkk]-open[kkk])/_Point;
         if(res>0) positive+=res;
         if(res<0) negative-=res;
         kkk++;
        }
      IndBuffer[bar]=(positive-negative)/ExPeriod;
     }
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//--- основной цикл раскраски индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=0;

      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }

      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }
      ColorIndBuffer[bar]=clr;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
