//+------------------------------------------------------------------+
//|                                                 KaufWMAcross.mq5 |
//|                                     Copyright © 2007, John Smith |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, John Smith"
#property link "http://www.metaquotes.net"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован бежевый цвет
#property indicator_color1  clrSalmon
//--- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//--- отображение бычей метки индикатора
#property indicator_label1  "KaufWMAcross Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычей линии индикатора использован зеленый цвет
#property indicator_color2  clrLime
//--- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//--- отображение медвежьей метки индикатора
#property indicator_label2 "KaufWMAcross Buy"
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET 0          // Константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
//--- Параметры AMA
input uint ama_period=9;                            // Период AMA
input uint fast_ma_period=2;                        // Период быстрой скользящей
input uint slow_ma_period=30;                       // Период медленной скользящей
input ENUM_APPLIED_PRICE  AMAPrice=PRICE_CLOSE;     // Цена AMA
//--- Параметры скользящего среднего
input uint  MAPeriod=13;                            // Период MA
input  ENUM_MA_METHOD   MAType=MODE_LWMA;           // Метод усреднения MA
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;     // Цена MA
//+----------------------------------------------+
//--- объявление динамических массивов, которые будут
//--- в дальнейшем использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//--- объявление целочисленных переменных для хендлов индикаторов
int ATR_Handle,AMA_Handle,MA_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- инициализация глобальных переменных 
   int ATR_Period=15;
   min_rates_total=int(MathMax(MathMax(ATR_Period,ama_period+1),MAPeriod+1));
//--- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора iMA
   MA_Handle=iMA(NULL,0,MAPeriod,0,MAType,MAPrice);
   if(MA_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMA");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора iAMA
   AMA_Handle=iAMA(NULL,0,ama_period,fast_ma_period,slow_ma_period,0,AMAPrice);
   if(AMA_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iAMA");
      return(INIT_FAILED);
     }
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//--- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- имя для окон данных и метка для субъокон 
   string short_name="KaufWMAcross";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//--- завершение инициализации
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
//--- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(AMA_Handle)<rates_total
      || BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- объявления локальных переменных 
   int to_copy,limit,bar;
   double AMA[],MA[],ATR[];
//--- расчёты необходимого количества копируемых данных и
//--- стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-min_rates_total; // стартовый номер для расчёта всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
   to_copy=limit+2;
//--- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);
   if(CopyBuffer(AMA_Handle,0,0,to_copy,AMA)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(MA,true);
   ArraySetAsSeries(AMA,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      if(AMA[bar+1]>MA[bar+1] && AMA[bar]<MA[bar]) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
      if(AMA[bar+1]<MA[bar+1] && AMA[bar]>MA[bar]) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+