//+------------------------------------------------------------------+
//|                                                   BykovTrend.mq5 |
//|                                        Ramdass - Conversion only |
//|                                                                  |
//+------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Ramdass - Conversion only"
//--- ссылка на сайт автора
#property link      ""
//--- номер версии индикатора
#property version   "1.02"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован розовый цвет
#property indicator_color1  clrMagenta
//--- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//--- отображение бычей метки индикатора
#property indicator_label1  "BykovTrend Sell"
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
#property indicator_label2 "BykovTrend Buy"
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int RISK=3;
input int SSP=9;
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//---
bool uptrend_,old;
int K,WPR_Handle,ATR_Handle,min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- инициализация глобальных переменных 
   K=33-RISK;
   int ATR_Period=15;
   min_rates_total=int(MathMax(SSP,ATR_Period))+1;
//--- получение хендла индикатора ATR
   WPR_Handle=iWPR(NULL,0,SSP);
   if(WPR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iWPR");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//--- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- имя для окон данных и метка для подокон 
   string short_name="BykovTrend";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---   
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
//--- проверка количества баров на достаточность для расчета
   if(BarsCalculated(WPR_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//--- объявления локальных переменных 
   int to_copy,limit,bar;
   double range,wpr,WPR[],ATR[];
   bool uptrend;

//--- расчеты необходимого количества копируемых данных и
//стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      to_copy=rates_total; // расчетное количество всех баров
      limit=rates_total-min_rates_total; // стартовый номер для расчета всех баров
      uptrend_=false;
      old=false;
     }
   else
     {
      to_copy=rates_total-prev_calculated+1; // расчетное количество только новых баров
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }
//--- копируем вновь появившиеся данные в массивы WPR[] и ATR[]
   if(CopyBuffer(WPR_Handle,0,0,to_copy,WPR)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(WPR,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- восстанавливаем значения переменных
   uptrend=uptrend_;
//--- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //--- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0)
        {
         uptrend_=uptrend;
        }
      //---  
      wpr=WPR[bar];
      range=ATR[bar]*3/8;
      //---
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //---
      if(wpr<-100+K) uptrend=false;
      if(wpr>-K)     uptrend=true;
      //---
      if(!old &&  uptrend) BuyBuffer [bar]=low[bar]-range;
      if( old && !uptrend) SellBuffer[bar]=high[bar]+range;
      //---
      if(bar) old=uptrend;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
