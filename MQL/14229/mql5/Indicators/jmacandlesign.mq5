//+------------------------------------------------------------------+
//|                                                JMACandleSign.mq5 |
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Семафорный сигнальный индикатор с использованием двух индикаторов JMA, построенных на Open и Close значениях ценового ряда"
//---- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован розовый цвет
#property indicator_color1  clrDeepPink
//--- толщина линии индикатора 1 равна 2
#property indicator_width1  2
//--- отображение медвежьей метки индикатора
#property indicator_label1  "JMACandle Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьего индикатора       |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычьей линии индикатора использован голубой цвет
#property indicator_color2  clrDodgerBlue
//--- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//--- отображение бычьей метки индикатора
#property indicator_label2 "JMACandle Buy"
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET  0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int JLength=7;  // Глубина сглаживания
input int JPhase=100; // Параметр сглаживания
//---- изменяющийся в пределах -100 ... +100,
//---- влияет на качество переходного процесса;
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление целочисленных переменных для хендлов индикаторов
int O_Handle,C_Handle,ATR_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- инициализация глобальных переменных 
   min_rates_total=30+1;
   int ATR_Period=15;
   min_rates_total=int(MathMax(min_rates_total,ATR_Period))+1;
//--- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }
//---- объявление перечислений 
   enum Applied_price_ //Тип константы
     {
      PRICE_CLOSE_ = 1,     //PRICE_CLOSE
      PRICE_OPEN_ = 2       //PRICE_OPEN
     };
//---- получение хендлов индикатора JMA
   O_Handle=iCustom(NULL,0,"JMA",JLength,JPhase,PRICE_OPEN_,0,0);
   if(O_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iJMA[OPEN]!");
      return(INIT_FAILED);
     }
//----
   C_Handle=iCustom(NULL,0,"JMA",JLength,JPhase,PRICE_CLOSE_,0,0);
   if(C_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iJMA[CLOSE]!");
      return(INIT_FAILED);
     }
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,172);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,172);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//--- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для субъокон 
   string short_name="JMACandleSign";
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
//---- проверка количества баров на достаточность для расчета
   if(BarsCalculated(O_Handle)<rates_total
      || BarsCalculated(C_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- объявление локальных переменных 
   int to_copy,limit,bar;
   double ATR[],FOpen[],FClose[];
//---- расчеты необходимого количества копируемых данных и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-2; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }
//----
   to_copy=limit+2;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(O_Handle,0,0,to_copy,FOpen)<=0) return(RESET);
   if(CopyBuffer(C_Handle,0,0,to_copy,FClose)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(FOpen,true);
   ArraySetAsSeries(FClose,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- основной цикл исправления и окрашивания свечей
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      if(FOpen[bar+1]>=FClose[bar+1] && FOpen[bar]<FClose[bar]) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
      if(FOpen[bar+1]<=FClose[bar+1] && FOpen[bar]>FClose[bar]) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
