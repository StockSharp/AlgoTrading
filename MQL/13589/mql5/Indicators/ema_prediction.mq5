//+------------------------------------------------------------------+
//|                                               EMA_Prediction.mq5 |
//|                                     Copyright © 2008, Codersguru |
//|                                         http://www.forex-tsd.com |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2008, Codersguru"
//---- ссылка на сайт автора
#property link      "http://www.forex-tsd.com"
//---- номер версии индикатора
#property version   "1.01"
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
//---- в качестве цвета медвежьей линии индикатора использован розовый цвет
#property indicator_color1  clrMagenta
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение медвежьей метки индикатора
#property indicator_label1  "EMA_Prediction Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьего индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычьей линии индикатора использован зеленый цвет
#property indicator_color2  clrLime
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение бычьей метки индикатора
#property indicator_label2 "EMA_Prediction Buy"
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET  0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint               FastMAPeriod=1;
input  ENUM_MA_METHOD    FastMAType=MODE_EMA;
input ENUM_APPLIED_PRICE FastMAPrice=PRICE_CLOSE;
input uint               SlowMAPeriod=2;
input  ENUM_MA_METHOD    SlowMAType=MODE_EMA;
input ENUM_APPLIED_PRICE SlowMAPrice=PRICE_CLOSE;
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление целочисленных переменных для хендлов индикаторов
int ATR_Handle,FsMA_Handle,SlMA_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- инициализация глобальных переменных
   int ATR_Period=12;
   min_rates_total=int(MathMax(MathMax(FastMAPeriod,SlowMAPeriod),ATR_Period))+1;
//---- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iATR!");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора Fast iMA
   FsMA_Handle=iMA(NULL,0,FastMAPeriod,0,FastMAType,FastMAPrice);
   if(FsMA_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Fast iMA");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора Slow iMA
   SlMA_Handle=iMA(NULL,0,SlowMAPeriod,0,SlowMAType,SlowMAPrice);
   if(SlMA_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Slow iMA");
      return(INIT_FAILED);
     }
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"EMA_Prediction Sell");
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"EMA_Prediction Buy");
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//---- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и лэйба для субъокон 
   string short_name="EMA_Prediction";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
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
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- объявления локальных переменных 
   int to_copy,limit,bar;
   double ATR[],FsMA[],SlMA[];
//---- расчеты необходимого количества копируемых данных и
//---- стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }
//----
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   to_copy++;
   if(CopyBuffer(FsMA_Handle,0,0,to_copy,FsMA)<=0) return(RESET);
   if(CopyBuffer(SlMA_Handle,0,0,to_copy,SlMA)<=0) return(RESET);
//---- индексация элементов в массивах как в таймсериях
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(FsMA,true);
   ArraySetAsSeries(SlMA,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //----
      if(FsMA[bar+1]<SlMA[bar+1] && FsMA[bar]>SlMA[bar] && open[bar]<close[bar]) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
      if(FsMA[bar+1]>SlMA[bar+1] && FsMA[bar]<SlMA[bar] && open[bar]>close[bar]) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
