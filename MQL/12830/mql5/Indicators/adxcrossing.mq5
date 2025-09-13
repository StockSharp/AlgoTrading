//+------------------------------------------------------------------+
//|                                                  ADXCrossing.mq5 |
//|                                           Copyright © 2005, Amir |
//|                                                                  |
//+------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2005, Amir"
//--- ссылка на сайт автора
#property link      ""
//--- номер версии индикатора
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
#property indicator_color1  clrMagenta
//--- толщина линии индикатора 1 равна 2
#property indicator_width1  2
//--- отображение бычей метки индикатора
#property indicator_label1  "ADXCrossing Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьго индикатора        |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычей линии индикатора использован зеленый цвет
#property indicator_color2  clrLime
//--- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//--- отображение медвежьей метки индикатора
#property indicator_label2 "ADXCrossing Buy"
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET 0  // Константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint ADXPeriod=50;
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//--- объявление целочисленных переменных для хендлов индикаторов
int ATR_Handle,ADX_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- инициализация глобальных переменных 
   int AtrPeriod=14;
   min_rates_total=int(ADXPeriod)+1;
   min_rates_total=int(MathMax(min_rates_total,AtrPeriod));
//--- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,AtrPeriod);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора iADX
   ADX_Handle=iADX(NULL,0,ADXPeriod);
   if(ADX_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iADX");
      return(INIT_FAILED);
     }
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//--- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- имя для окон данных и метка для подокон 
   string short_name="ADXCrossing";
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
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(ADX_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);
//--- объявления локальных переменных 
   int to_copy,limit,bar;
   double ATR[],Up[],Dn[];
//--- расчеты необходимого количества копируемых данных
//--- и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }
   to_copy=limit+1;
//--- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   to_copy++;
   if(CopyBuffer(ADX_Handle,1,0,to_copy,Up)<=0) return(RESET);
   if(CopyBuffer(ADX_Handle,2,0,to_copy,Dn)<=0) return(RESET);
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(Up,true);
   ArraySetAsSeries(Dn,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //---
      if(Up[bar]>Dn[bar] &&  Up[bar+1]<=Dn[bar+1]) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
      if(Up[bar]<Dn[bar] &&  Up[bar+1]>=Dn[bar+1]) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
