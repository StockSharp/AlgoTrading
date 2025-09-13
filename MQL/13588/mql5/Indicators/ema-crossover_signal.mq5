//+------------------------------------------------------------------+
//|                                         EMA-Crossover_Signal.mq5 |
//|         Copyright © 2005, Jason Robinson (jnrtrading)            |
//|                   http://www.jnrtading.co.uk                     |
//+------------------------------------------------------------------+
/*
  +------------------------------------------------------------------+
  | Allows you to enter two ema periods and it will then show you at |
  | Which point they crossed over. It is more usful on the shorter   |
  | periods that get obscured by the bars / candlesticks and when    |
  | the zoom level is out. Also allows you then to remove the emas   |
  | from the chart. (emas are initially set at 5 and 6)              |
  +------------------------------------------------------------------+
*/
#property copyright "Copyright © 2005, Jason Robinson (jnrtrading)"
#property link "http://www.jnrtading.co.uk"
#property description "EMA-Crossover_Signal"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован розовый цвет
#property indicator_color1  clrMagenta
//---- толщина индикатора 1 равна 1
#property indicator_width1  1
//---- отображение медвежьей метки индикатора
#property indicator_label1  "EMA-Crossover_Signal Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьего индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован синий цвет
#property indicator_color2  clrBlue
//---- толщина индикатора 2 равна 1
#property indicator_width2  1
//---- отображение бычьей метки индикатора
#property indicator_label2 "EMA-Crossover_Signal Buy"
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET  0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint FasterMA=5;
input uint SlowerMA=6;
input  ENUM_MA_METHOD   MAType=MODE_LWMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление для хендлов индикаторов
int FsMA_Handle,SlMA_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация глобальных переменных   
   min_rates_total=int(MathMax(FasterMA,SlowerMA)+3);
//---- получение хендла индикатора iMA
   FsMA_Handle=iMA(NULL,0,FasterMA,0,MAType,MAPrice);
   if(FsMA_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iMA");
//---- получение хендла индикатора iMA
   SlMA_Handle=iMA(NULL,0,SlowerMA,0,MAType,MAPrice);
   if(SlMA_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iMA");
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,119);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,119);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);
//---- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="EMA-Crossover_Signal";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
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
   if(BarsCalculated(FsMA_Handle)<rates_total
      || BarsCalculated(SlMA_Handle)<rates_total
      || rates_total<min_rates_total)return(RESET);
//---- объявления локальных переменных 
   int to_copy,limit,bar;
   double AvgRange,Range,FsMA[],SlMA[];
//---- расчеты необходимого количества копируемых данных и
//---- стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(FsMA,true);
   ArraySetAsSeries(SlMA,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//----
   to_copy=limit+3;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(FsMA_Handle,0,0,to_copy,FsMA)<=0) return(RESET);
   if(CopyBuffer(SlMA_Handle,0,0,to_copy,SlMA)<=0) return(RESET);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      AvgRange=0.0;
      for(int count=bar+9; count>=bar; count--) AvgRange+=MathAbs(high[count]-low[count]);
      Range=AvgRange/10;
      //----
      SellBuffer[bar]=0.0;
      BuyBuffer[bar]=0.0;
      //----
      if(FsMA[bar+1]>SlMA[bar+1] && FsMA[bar+2]<SlMA[bar+2] && FsMA[bar]>SlMA[bar]) BuyBuffer[bar]=low[bar]-Range*0.5;
      else if(FsMA[bar+1]<SlMA[bar+1] && FsMA[bar+2]>SlMA[bar+2] && FsMA[bar]<SlMA[bar]) SellBuffer[bar]=high[bar]+Range*0.5;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
