//+------------------------------------------------------------------+
//|                                                  LeManSignal.mq5 |
//|                                         Copyright © 2009, LeMan. |
//|                                                 b-market@mail.ru |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2009, LeMan."
//---- ссылка на сайт автора
#property link      "b-market@mail.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//---- в качестве цвета медвежьей стрелки индикатора использован цвет Magenta
#property indicator_color1  Magenta
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение метки индикатора
#property indicator_label1  "LeManSell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьего индикатора      |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычьей стрелки индикатора использован цвет Lime
#property indicator_color2  Lime
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение метки индикатора
#property indicator_label2 "LeManBuy"

//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int LPeriod=12; // Период индикатора 

//+----------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//---
int StartBars;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация глобальных переменных 
   StartBars=LPeriod+LPeriod+2+1;

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"LeManSell");
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(SellBuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"LeManBuy");
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);

//---- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="LeManSignal";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
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
//---- проверка количества баров на достаточность для расчета
   if(rates_total<StartBars) return(0);

//---- объявления локальных переменных 
   int limit,bar,bar1,bar2,bar1p,bar2p;
   double H1,H2,H3,H4,L1,L2,L3,L4;

//---- индексация элементов в массивах как в таймсериях
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);

//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      limit=rates_total-1-StartBars;                     // стартовый номер для расчета всех баров
   else limit=rates_total-prev_calculated;               // стартовый номер для расчета новых баров

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0; bar--)
     {
      bar1=bar+1;
      bar2=bar+2;
      bar1p=bar1+LPeriod;
      bar2p=bar2+LPeriod;
      //----
      H1 = high[ArrayMaximum(high,bar1, LPeriod)];
      H2 = high[ArrayMaximum(high,bar1p,LPeriod)];
      H3 = high[ArrayMaximum(high,bar2, LPeriod)];
      H4 = high[ArrayMaximum(high,bar2p,LPeriod)];
      L1 = low [ArrayMinimum(low, bar1, LPeriod)];
      L2 = low [ArrayMinimum(low, bar1p,LPeriod)];
      L3 = low [ArrayMinimum(low, bar2, LPeriod)];
      L4 = low [ArrayMinimum(low, bar2p,LPeriod)];
      //----
      BuyBuffer[bar]=EMPTY_VALUE;
      SellBuffer[bar]=EMPTY_VALUE;

      //---- условие покупки                       
      if(H3<=H4 && H1>H2) BuyBuffer[bar]=high[bar+1]+_Point;
      //---- условие продажи      
      if(L3>=L4 && L1<L2) SellBuffer[bar]=low[bar+1]-_Point;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
