//+------------------------------------------------------------------+
//|                                                   MACDCandle.mq5 |
//|                               Copyright © 2013, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "MACDCandle Smoothed"
//---- номер версии индикатора
#property version   "1.00"
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано пять буферов
#property indicator_buffers 5
//---- использовано всего одно графическое построение
#property indicator_plots   1
//---- в качестве индикатора использованы цветные свечи
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrDeepPink,clrBlue,clrTeal
//---- отображение метки индикатора
#property indicator_label1  "MACDCandle Open;MACDCandle High;MACDCandle Low;MACDCandle Close"
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET  0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Объявление перечислений                      |
//+----------------------------------------------+
enum MODE
  {
   MODE_HISTOGRAM=0,    // Гистограмма
   MODE_SIGNAL_LINE=1   // Сигнальная линия
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint  fast_ema_period=12;             // Период быстрой средней 
input uint  slow_ema_period=26;             // Период медленной средней 
input uint  signal_period=9;                // Период усреднения разности 
input MODE  mode=MODE_SIGNAL_LINE;          // Источник даных для расчета
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление целочисленных переменных для хендлов индикаторов
int MACD_Handle[4];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- инициализация глобальных переменных 
   min_rates_total=int(MathMax(fast_ema_period,slow_ema_period));
   if(mode==MODE_SIGNAL_LINE) min_rates_total+=int(signal_period);
//---- получение хендла индикатора iMACD
   MACD_Handle[0]=iMACD(NULL,0,fast_ema_period,slow_ema_period,signal_period,PRICE_OPEN);
   if(MACD_Handle[0]==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMACD["+string(0)+"]!");
      return(INIT_FAILED);
     }
   MACD_Handle[1]=iMACD(NULL,0,fast_ema_period,slow_ema_period,signal_period,PRICE_HIGH);
   if(MACD_Handle[1]==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMACD["+string(1)+"]!");
      return(INIT_FAILED);
     }
   MACD_Handle[2]=iMACD(NULL,0,fast_ema_period,slow_ema_period,signal_period,PRICE_LOW);
   if(MACD_Handle[2]==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMACD["+string(2)+"]!");
      return(INIT_FAILED);
     }
   MACD_Handle[3]=iMACD(NULL,0,fast_ema_period,slow_ema_period,signal_period,PRICE_CLOSE);
   if(MACD_Handle[3]==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMACD["+string(3)+"]!");
      return(INIT_FAILED);
     }
//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буферах как в таймсериях
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);
   ArraySetAsSeries(ExtColorBuffer,true);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- имя для окон данных и метка для подокон 
   string short_name="MACDCandl";
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
   if(BarsCalculated(MACD_Handle[0])<rates_total
      || BarsCalculated(MACD_Handle[1])<rates_total
      || BarsCalculated(MACD_Handle[2])<rates_total
      || BarsCalculated(MACD_Handle[3])<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- объявление локальных переменных 
   int to_copy,limit,bar;
//---- расчеты необходимого количества копируемых данных и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-1; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }
//---
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(MACD_Handle[0],int(mode),0,to_copy,ExtOpenBuffer)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle[1],int(mode),0,to_copy,ExtHighBuffer)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle[2],int(mode),0,to_copy,ExtLowBuffer)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle[3],int(mode),0,to_copy,ExtCloseBuffer)<=0) return(RESET);
//---- основной цикл исправления и окрашивания свечей
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double Max=MathMax(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);
      double Min=MathMin(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);
      //---
      ExtHighBuffer[bar]=MathMax(Max,ExtHighBuffer[bar])/_Point;
      ExtLowBuffer[bar]=MathMin(Min,ExtLowBuffer[bar])/_Point;
      //---
      ExtOpenBuffer[bar]/=_Point;
      ExtCloseBuffer[bar]/=_Point;
      //---
      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=2.0;
      else if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else ExtColorBuffer[bar]=1.0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
