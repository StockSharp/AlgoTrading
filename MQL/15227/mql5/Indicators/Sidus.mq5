//+------------------------------------------------------------------+
//|                                                        Sidus.mq5 | 
//|                                  Copyright © 2006, GwadaTradeBoy |
//|                                            racooni_1975@yahoo.fr |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, GwadaTradeBoy"
#property link      "racooni_1975@yahoo.fr"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов 6
#property indicator_buffers 6 
//---- использовано 4 графических построения
#property indicator_plots   4
//+-----------------------------------+
//|  объявление констант              |
//+-----------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчет индикатора
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//---- в качестве цвета индикатора использован цвет Teal
#property indicator_color1  Teal
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение метки индикатора
#property indicator_label1 "Sidus Buy"
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета индикатора использован MediumVioletRed
#property indicator_color2  MediumVioletRed
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение метки индикатора
#property indicator_label2  "Sidus Sell"
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде облака
#property indicator_type3 DRAW_FILLING
//---- в качестве цветов индикатора использованы BlueViolet и Magenta
#property indicator_color3 BlueViolet,Magenta
//---- отображение метки индикатора
#property indicator_label3  "Sidus Fast EMA"
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде облака
#property indicator_type4 DRAW_FILLING
//---- в качестве цветов индикатора использованы цвета Lime и Red
#property indicator_color4 Lime,Red
//---- отображение метки индикатора
#property indicator_label4  "Sidus Fast LWMA"

//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint FastEMA=18;                    // Период быстрой EMA
input uint SlowEMA=28;                    // Период медленной EMA
input uint FastLWMA=5;                    // Период быстрой LWMA
input uint SlowLWMA=8;                    // Период медленной LWMA
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // Ценовая константа
extern uint digit=0;                      // Размах в пунктах
//+-----------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double FstEmaBuffer[],SlwEmaBuffer[],FstLwmaBuffer[],SlwLwmaBuffer[];
double SellBuffer[],BuyBuffer[];
double DIGIT;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление целочисленных переменных для хендлов индикаторов
int FstEma_Handle,SlwEma_Handle,FstLwma_Handle,SlwLwma_Handle,ATR_Handle;
//+------------------------------------------------------------------+   
//| Sidus indicator initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчета данных
   min_rates_total=int(MathMax(FastLWMA,SlowLWMA)+3);

//---- Инициализация переменных  
   DIGIT=digit*_Point;

//---- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,15);
   if(ATR_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора ATR");

//---- получение хендла индикатора FastEMA
   FstEma_Handle=iMA(NULL,0,FastEMA,0,MODE_EMA,IPC);
   if(FstEma_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора FastEMA");

//---- получение хендла индикатора SlowEma
   SlwEma_Handle=iMA(NULL,0,SlowEMA,0,MODE_EMA,IPC);
   if(SlwEma_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора SlowEma");

//---- получение хендла индикатора FastLWMA
   FstLwma_Handle=iMA(NULL,0,FastLWMA,0,MODE_LWMA,IPC);
   if(FstLwma_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора FastLWMA");

//---- получение хендла индикатора SlowLWMA
   SlwLwma_Handle=iMA(NULL,0,SlowLWMA,0,MODE_LWMA,IPC);
   if(SlwLwma_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора SlowLWMA");

//---- превращение динамического массива BuyBuffer[] в индикаторный буфер
   SetIndexBuffer(0,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,233);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);

//---- превращение динамического массива SellBuffer[] в индикаторный буфер
   SetIndexBuffer(1,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,234);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);

//---- превращение динамического массива FstEmaBuffer[] в индикаторный буфер
   SetIndexBuffer(2,FstEmaBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(FstEmaBuffer,true);

//---- превращение динамического массива SlwEmaBuffer[] в индикаторный буфер
   SetIndexBuffer(3,SlwEmaBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SlwEmaBuffer,true);

//---- превращение динамического массива FstLwmaBuffer[] в индикаторный буфер
   SetIndexBuffer(4,FstLwmaBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,0);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(FstLwmaBuffer,true);

//---- превращение динамического массива SlwLwmaBuffer[] в индикаторный буфер
   SetIndexBuffer(5,SlwLwmaBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(5,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(5,PLOT_EMPTY_VALUE,0);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SlwLwmaBuffer,true);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Sidus(",FastEMA,", ",SlowEMA,", ",FastLWMA,", ",SlowLWMA,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| Sidus iteration function                                          | 
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
//---- проверка количества баров на достаточность для расчета
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(FstEma_Handle)<rates_total
      || BarsCalculated(SlwEma_Handle)<rates_total
      || BarsCalculated(FstLwma_Handle)<rates_total
      || BarsCalculated(SlwLwma_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- объявления локальных переменных 
   int to_copy,limit,bar;
   double range,ATR[];

//---- расчеты необходимого количества копируемых данных
//---- и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
      to_copy=rates_total;
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров     
      to_copy=limit+1;
     }

//---- копируем вновь появившиеся данные в массивы ATR[] и индикаторные буферы
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   if(CopyBuffer(FstEma_Handle,0,0,to_copy,FstEmaBuffer)<=0) return(RESET);
   if(CopyBuffer(SlwEma_Handle,0,0,to_copy,SlwEmaBuffer)<=0) return(RESET);
   if(CopyBuffer(FstLwma_Handle,0,0,to_copy,FstLwmaBuffer)<=0) return(RESET);
   if(CopyBuffer(SlwLwma_Handle,0,0,to_copy,SlwLwmaBuffer)<=0) return(RESET);

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      range=ATR[bar]*3/8;
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(FstLwmaBuffer[bar]>SlwLwmaBuffer[bar]+DIGIT && FstLwmaBuffer[bar+1]<=SlwLwmaBuffer[bar+1]) BuyBuffer[bar]=low[bar]-range;
      if(SlwLwmaBuffer[bar]>SlwEmaBuffer[bar]+DIGIT && SlwLwmaBuffer[bar+1]<=SlwEmaBuffer[bar]) BuyBuffer[bar]=low[bar]-range;

      if(FstLwmaBuffer[bar]<SlwLwmaBuffer[bar]-DIGIT && FstLwmaBuffer[bar+1]>=SlwLwmaBuffer[bar+1]) SellBuffer[bar]=high[bar]+range;
      if(SlwLwmaBuffer[bar]<SlwEmaBuffer[bar]-DIGIT && SlwLwmaBuffer[bar+1]>=SlwEmaBuffer[bar]) SellBuffer[bar]=high[bar]+range;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
