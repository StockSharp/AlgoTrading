//+------------------------------------------------------------------+
//|                                        simple_trading_system.mq5 |
//|                               Copyright 2014, Vitalie Postolache |
//|                                             http://www.mql4.com/ |
//+------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright 2014, Vitalie Postolache"
//--- ссылка на сайт автора
#property link      "http://www.mql4.com/"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован розовый цвет
#property indicator_color1  clrDeepPink
//--- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//--- отображение бычей метки индикатора
#property indicator_label1  "simple_trading_system Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьго индикатора        |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычей линии индикатора использован зеленый цвет
#property indicator_color2  clrLightSeaGreen
//--- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//--- отображение медвежьей метки индикатора
#property indicator_label2 "simple_trading_system Buy"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint   MAShift=4;
//--- Параметры скользящего среднего
input uint   MAPeriod=2;
input  ENUM_MA_METHOD   MAType=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//--- объявление целочисленных переменных для хендлов индикаторов
int MA_Handle,ATR_Handle;
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//--- объявление глобальных переменных
int Count[],MAShift_,SumPeriod;
double Ma[];
//+------------------------------------------------------------------+
//|  Пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
                          int Size)
  {
//---
   int numb,Max1,Max2;
   static int count=1;
//---
   Max2=Size;
   Max1=Max2-1;
//---
   count--;
   if(count<0) count=Max1;
//---
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- инициализация глобальных переменных 
   int ATR_Period=15;
   min_rates_total=int(MathMax(ATR_Period,MAPeriod+MAShift));
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
//--- распределение памяти под массивы переменных  
   MAShift_=int(MAShift+1);
   SumPeriod=int(MAPeriod+MAShift);
   ArrayResize(Count,MAShift_);
   ArrayResize(Ma,MAShift_);
//---
   ArrayInitialize(Count,0);
   ArrayInitialize(Ma,0.0);
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
//--- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- имя для окон данных и лэйба для субъокон 
   string short_name="simple_trading_system";
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
//--- проверка количества баров на достаточность для расчета
   if(BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- объявления локальных переменных 
   int to_copy,limit,bar,sign0;
   double MA[],ATR[],ma0,ma1;
   static int sign1;
//--- расчеты необходимого количества копируемых данных
//--- и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total; // стартовый номер для расчета всех баров
      sign1=0;
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }
   to_copy=limit+1;
//--- копируем вновь появившиеся данные в массивы MA[] и ATR[]
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(MA,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
//---   
   sign0=sign1;
//--- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      Ma[Count[0]]=MA[bar];
      //---
      ma0=MA[bar];
      ma1=Ma[Count[MAShift]];
      //---
      if(sign0<+1 && ma0<=ma1 && close[bar]>=close[bar+MAShift] && close[bar]<=close[bar+SumPeriod] && close[bar]<open[bar])
        {
         BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
         sign0=+1;
        }
      //---
      if(sign0>-1 && ma0>=ma1 && close[bar]<=close[bar+MAShift] && close[bar]>=close[bar+SumPeriod] && close[bar]>open[bar])
        {
         SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
         sign0=-1;
        }
      //---
      if(bar)
        {
         Recount_ArrayZeroPos(Count,MAShift_);
         sign1=sign0;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
