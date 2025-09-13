//+------------------------------------------------------------------+
//|                                                   SimpleBars.mq5 |
//|                                  Copyright © 2012, Ivan Kornilov |
//|                                                 excelf@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Ivan Kornilov"
#property link "excelf@gmail.com"
#property description "SimpleBars"
//--- номер версии индикатора
#property version   "1.01"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчета и отрисовки индикатора использовано пять буферов
#property indicator_buffers 5
//--- использовано всего одно графическое построение
#property indicator_plots   1
//--- в качестве индикатора использованы цветные свечи
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1  clrSeaGreen,clrRed
//--- отображение метки индикатора
#property indicator_label1  "Upper;lower"
//+----------------------------------------------+
#define SIGNAL_NONE        0  // Пустой сигнал
#define SIGNAL_BUY         1  // Сигнал на покупку 
#define SIGNAL_SELL       -1  // Сигнал на продажу 
#define SIGNAL_TRADE_ALLOW 3  // Сигнал, разрешающий торговлю
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint period=6;
input bool useclose=true;
//+----------------------------------------------+
//--- объявление динамических массивов, которые будут
//--- в дальнейшем использованы в качестве индикаторных буферов
double ExtopenBuffer[];
double ExthighBuffer[];
double ExtlowBuffer[];
double ExtcloseBuffer[];
double ExtColorBuffer[];
//---
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//--- инициализация глобальных переменных 
   min_rates_total=int(period)+1;
//--- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(0,ExtopenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExthighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtlowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtcloseBuffer,INDICATOR_DATA);
//--- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//--- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- имя для окон данных и метка для субъокон 
   string short_name="SimpleBars";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);
//--- объявления локальных переменных 
   int first,bar,trend=0;
   static int prev_trend;
   double buyPrice,sellPrice;
//--- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=min_rates_total; // стартовый номер для расчета всех баров
      prev_trend=SIGNAL_NONE;
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//--- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      if(useclose)
        {
         buyPrice=close[bar];
         sellPrice=close[bar];
        }
      else
        {
         buyPrice=low[bar];
         sellPrice=high[bar];
        }

      if(prev_trend==SIGNAL_NONE)
        {
         if(close[bar]>open[bar]) trend=SIGNAL_BUY;
         else trend=SIGNAL_SELL;
        }
      else
        {
         if(prev_trend==SIGNAL_BUY)
           {
            if(buyPrice>low[bar-1]) trend=SIGNAL_BUY;
            else
              {
               for(int j=2; j<=int(period); j++)
                 {
                  if(buyPrice>low[bar-j])
                    {
                     trend=SIGNAL_BUY;
                     break;
                    }
                  else trend=SIGNAL_SELL;
                 }
              }
           }

         if(prev_trend==SIGNAL_SELL)
           {
            if(sellPrice<high[bar-1]) trend=SIGNAL_SELL;
            else
              {
               for(int j=2; j<=int(period); j++)
                 {
                  if(sellPrice<high[bar-j])
                    {
                     trend=SIGNAL_SELL;
                     break;
                    }
                  else trend=SIGNAL_BUY;
                 }
              }
           }
        }
      //--- раскрашивание свечей
      if(trend==SIGNAL_SELL) ExtColorBuffer[bar]=1.0;
      if(trend==SIGNAL_BUY) ExtColorBuffer[bar]=0.0;
      //---
      ExtopenBuffer[bar]=open[bar];
      ExtcloseBuffer[bar]=close[bar];
      ExthighBuffer[bar]=high[bar];
      ExtlowBuffer[bar]=low[bar];
      //---
      if(bar<rates_total-1) prev_trend=trend;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
