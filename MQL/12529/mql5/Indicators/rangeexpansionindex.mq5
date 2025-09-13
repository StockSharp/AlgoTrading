//+------------------------------------------------------------------+
//|                                          RangeExpansionIndex.mq5 |
//|                                  Copyright © 2010, EarnForex.com |
//|                                        http://www.earnforex.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, EarnForex.com"
#property link      "http://www.earnforex.com"
//--- номер версии индикатора
#property version   "1.0"
#property description "Calculates Tom DeMark's Range Expansion Index."
#property description "Going above 60 and then dropping below 60 signals price weakness."
#property description "Going below -60 and the rising above -60 signals price strength."
#property description "For more info see The New Science of Technical Analysis."
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- количество индикаторных буферов 2
#property indicator_buffers 2 
//--- использовано всего одно графические построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//--- отрисовка индикатора в виде пятицветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//--- в качестве окраски гистограммы использовано пять цветов
#property indicator_color1 clrGray,clrLime,clrBlue,clrRed,clrMagenta
//--- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width1 2
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 +60
#property indicator_level2 -60
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
//--- Входные параметры индикатора
input int REI_Period=8;  // Период усреднения
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double ExtBuffer[],ColorExtBuffer[];
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Calculate the Conditional Value                                  |
//+------------------------------------------------------------------+
double SubValue(const int i,const double &High[],const double &Low[],const double &Close[])
  {
   int num_zero1,num_zero2;
//---
   double diff1 = High[i] - High[i - 2];
   double diff2 = Low[i] - Low[i - 2];
//---
   if((High[i-2]<Close[i-7]) && (High[i-2]<Close[i-8]) && (High[i]<High[i-5]) && (High[i]<High[i-6]))
      num_zero1=0;
   else
      num_zero1=1;
//---
   if((Low[i-2]>Close[i-7]) && (Low[i-2]>Close[i-8]) && (Low[i]>Low[i-5]) && (Low[i]>Low[i-6]))
      num_zero2=0;
   else
      num_zero2=1;
//---
   return(num_zero1*num_zero2 *(diff1+diff2));
  }
//+------------------------------------------------------------------+
//| Calculate the Absolute Value                                     |
//+------------------------------------------------------------------+
double AbsValue(const int i,const double &High[],const double &Low[])
  {
   double diff1 = MathAbs(High[i] - High[i - 2]);
   double diff2 = MathAbs(Low[i] - Low[i - 2]);
//---
   return(diff1+diff2);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчета данных
   min_rates_total=REI_Period+8;
//--- превращение динамического массива ExtBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора MAPeriod
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//--- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Range Expansion Index(",REI_Period,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов  цены для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total)return(0);
//--- объявления локальных переменных 
   int first1,first2,bar;
   double SubValueSum,AbsValueSum;
//--- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first1=min_rates_total-1; // стартовый номер для расчета всех баров
      first2=first1+1;
     }
   else
     {
      first1=prev_calculated-1; // стартовый номер для расчета новых баров
      first2=first1; // стартовый номер для расчета новых баров
     }
//--- основной цикл расчета индикатора
   for(bar=first1; bar<rates_total; bar++)
     {
      SubValueSum=0;
      AbsValueSum=0;
      //---
      for(int iii=0; iii<REI_Period; iii++)
        {
         SubValueSum += SubValue(bar - iii, high, low, close);
         AbsValueSum += AbsValue(bar - iii, high, low);
        }
      //---
      if(AbsValueSum!=0) ExtBuffer[bar]=SubValueSum/AbsValueSum*100;
      else ExtBuffer[bar]=0;
     }
//--- основной цикл раскраски индикатора
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorExtBuffer[bar]=0;
      //---
      if(ExtBuffer[bar]>0)
        {
         if(ExtBuffer[bar]>ExtBuffer[bar-1]) ColorExtBuffer[bar]=1;
         if(ExtBuffer[bar]<ExtBuffer[bar-1]) ColorExtBuffer[bar]=2;
        }
      //---
      if(ExtBuffer[bar]<0)
        {
         if(ExtBuffer[bar]<ExtBuffer[bar-1]) ColorExtBuffer[bar]=3;
         if(ExtBuffer[bar]>ExtBuffer[bar-1]) ColorExtBuffer[bar]=4;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
