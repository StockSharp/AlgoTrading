//+------------------------------------------------------------------+
//|                                                    NRTR_extr.mq5 |
//|                                        Copyright © 2005, Ramdass | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2005, Ramdass" 
#property link      "" 
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- количество индикаторных буферов 4
#property indicator_buffers 4 
//---- использовано всего четыре графических построения
#property indicator_plots   4
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в виде значка
#property indicator_type1 DRAW_ARROW
//---- в качестве окраски индикатора использован
#property indicator_color1 clrDodgerBlue
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки сигнальной линии
#property indicator_label1  "NRTR Up"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в виде значка
#property indicator_type2 DRAW_ARROW
//---- в качестве окраски индикатора использован
#property indicator_color2 clrMagenta
//---- линия индикатора - сплошная
#property indicator_style2 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width2 2
//---- отображение метки сигнальной линии
#property indicator_label2  "NRTR Down"
//+----------------------------------------------+
//|  Параметры отрисовки бычьего индикатора      |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде значка
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета бычей линии индикатора использован
#property indicator_color3  clrBlue
//---- линия индикатора 3 - непрерывная кривая
#property indicator_style3  STYLE_SOLID
//---- толщина линии индикатора 3 равна 2
#property indicator_width3  2
//---- отображение бычьей метки индикатора
#property indicator_label3  "Buy NRTR signal"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде значка
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован
#property indicator_color4  clrGold
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style4  STYLE_SOLID
//---- толщина линии индикатора 2 равна 2
#property indicator_width4  2
//---- отображение медвежьей метки индикатора
#property indicator_label4  "Sell NRTR signal"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint iPeriod=10;  // Период индикатора
input int iDig=0;       // Разряд
input int Shift=0;      // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double TrendUp[],TrendDown[];
double SignUp[];
double SignDown[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(iPeriod);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"NRTR(",string(iPeriod),", ",string(Shift),")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,TrendUp,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- индексация элементов в буферах, как в таймсериях   
   ArraySetAsSeries(TrendUp,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,TrendDown,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- индексация элементов в буферах, как в таймсериях   
   ArraySetAsSeries(TrendDown,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,SignUp,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах, как в таймсериях   
   ArraySetAsSeries(SignUp,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//---- символ для индикатора
   PlotIndexSetInteger(2,PLOT_ARROW,108);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,SignDown,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах, как в таймсериях   
   ArraySetAsSeries(SignDown,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0.0);
//---- символ для индикатора
   PlotIndexSetInteger(3,PLOT_ARROW,108);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов цены для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);

//---- объявления локальных переменных 
   double price,value,dK;
   static double price_prev,value_prev;
   int limit,bar,trend;
   static int trend_prev;

//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1;               // стартовый номер для расчета всех баров
      trend_prev=0;
      price_prev=value_prev=close[limit];     
     }
   else
     {
      limit=rates_total-prev_calculated;                 // стартовый номер для расчета новых баров
     }
   trend=trend_prev;
   price=price_prev;
   value=value_prev;

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      TrendUp[bar]=0.0;
      TrendDown[bar]=0.0;
      SignUp[bar]=0.0;
      SignDown[bar]=0.0;

      double AvgRange=0.0;
      for(int iii=0; iii<int(iPeriod); iii++) AvgRange+=MathAbs(high[bar+iii]-low[bar+iii]);
      dK=(AvgRange/iPeriod)/MathPow(10,SymbolInfoInteger("EURUSD",SYMBOL_DIGITS)-_Digits-iDig);

      if(trend>=0)
        {
         price=MathMax(price,high[bar]);
         value=MathMax(value,price*(1.0-dK));
         if(high[bar]<value)
           {
            price = high[bar];
            value = price*(1.0+dK);
            trend = -1;
           }
        }
      else if(trend<=0)
        {
         price=MathMin(price,low[bar]);
         value=MathMin(value,price*(1.0+dK));
         if(low[bar]>value)
           {
            price = low[bar];
            value = price*(1.0-dK);
            trend = +1;
           }
        }

      if(trend>0) TrendUp[bar]=value;
      if(trend<0) TrendDown[bar]=value;

      if(trend_prev<0 && trend>0) SignUp[bar]=TrendUp[bar];
      if(trend_prev>0 && trend<0) SignDown[bar]=TrendDown[bar];

      if(bar)
        {
         trend_prev=trend;
         price_prev=price;
         value_prev=value;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
