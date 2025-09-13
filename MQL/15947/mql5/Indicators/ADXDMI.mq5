//+------------------------------------------------------------------+ 
//|                                                       ADXDMI.mq5 | 
//|                                  Copyright © 2006, FXTEAM Turkey |
//|                                                                  | 
//+------------------------------------------------------------------+  
//---- авторство индикатора
#property copyright "Copyright © 2006, FXTEAM Turkey"
//---- авторство индикатора
#property link      ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора ADXDMI        |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде цветного облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветjd индикатора использованы
#property indicator_color1  clrBlue,clrRed
//---- отображение метки линии индикатора
#property indicator_label1  "ADXDMI"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Объявление констант                         |
//+----------------------------------------------+
#define RESET 0       // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint DMIPeriod=14;
input uint Smooth=10;
input int Shift=0;        // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double UpBuffer[];
double DnBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=4;

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);

//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"ADXDMI(",DMIPeriod,", ",Smooth,", ",Shift,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,4);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
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
   if(rates_total<min_rates_total) return(RESET);

//---- объявления локальных переменных 
   int first,bar;
   double tr,xx,price_high,price_low,PD,ND,Buff,ADX;
   static double PREADX,PREP,PREN,PRETR;

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=2;                   // стартовый номер для расчета всех баров
      PREP=NULL;
      PREN=NULL;
      PRETR=NULL;
      ADX=NULL;
      PREADX=NULL;
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров

//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {

      if(high[bar]>high[bar-1] && (high[bar]-high[bar-1])>(low[bar-1]-low[bar]))
        {
         xx=high[bar]-high[bar-1];
        }
      else xx=NULL;
      //----
      PD=(((DMIPeriod-1.0)*PREP)+xx)/(DMIPeriod);
      //----
      if(low[bar]<low[bar-1] && (low[bar-1]-low[bar])>(high[bar]-high[bar-1]))
        {
         xx=low[bar-1]-low[bar];
        }
      else xx=NULL;
      //----
      ND=(((DMIPeriod-1.0)*PREN)+xx)/(DMIPeriod);
      Buff=MathAbs(PD-ND);
      if(!Buff) ADX=(((Smooth-1.0)*PREADX))/Smooth;
      else ADX=(((Smooth-1.0)*PREADX)+(MathAbs(PD-ND)/(PD+ND)))/Smooth;
      //----
      price_high=MathMax(high[bar],close[bar-1]);
      price_low=MathMin(low[bar],close[bar-1]);
      double num1=MathAbs(price_high-price_low);
      double num2=MathAbs(price_high-close[bar-1]);
      double num3=MathAbs(close[bar-1]-price_low);
      //----
      tr=MathMax(num1,num2);
      tr=MathMax(tr,num3);
      //----
      tr=(((DMIPeriod-1.0)*PRETR)+tr)/DMIPeriod;
      //----
      UpBuffer[bar]=100000*(PD/tr);
      DnBuffer[bar]=100000*(ND/tr);
      //----
      if(bar<rates_total-1)
        {
         PREN=ND;
         PREP=PD;
         PREADX=ADX;
         PRETR=tr;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
