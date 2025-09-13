//+------------------------------------------------------------------+
//|                                           ColorMaRsi-Trigger.mq5 | 
//|                              Copyright © 2010, fx-system@mail.ru |
//|                                                fx-system@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, fx-system@mail.ru"
#property link      "fx-system@mail.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветов облака индикатора использованы
#property indicator_color1 clrMagenta,clrRoyalBlue
//---- отображение метки индикатора
#property indicator_label1  "MaRsi-Trigger"
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET  0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint nPeriodRsi=3;
input ENUM_APPLIED_PRICE nRSIPrice=PRICE_WEIGHTED;
input uint nPeriodRsiLong=13;
input ENUM_APPLIED_PRICE nRSIPriceLong=PRICE_MEDIAN;
input uint nPeriodMa=5;
input  ENUM_MA_METHOD nMAType=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPrice=PRICE_CLOSE;
input uint nPeriodMaLong=10;
input  ENUM_MA_METHOD nMATypeLong=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPriceLong=PRICE_CLOSE;
input int  Shift=0; // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- индикаторные буферы
double ExtMapBuffer[];
double ExtZerBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление целочисленных переменных для хендлов индикаторов
int MA_Handle,RSI_Handle,MAl_Handle,RSIl_Handle;
//+------------------------------------------------------------------+   
//| MaRsi-Trigger indicator initialization function                  | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- получение хендла индикатора iRSI
   RSI_Handle=iRSI(NULL,0,nPeriodRsi,nRSIPrice);
   if(RSI_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iRSI");
//---- получение хендла индикатора iRSIl
   RSIl_Handle=iRSI(NULL,0,nPeriodRsiLong,nRSIPriceLong);
   if(RSIl_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iRSIl");
//---- получение хендла индикатора iMA
   MA_Handle=iMA(NULL,0,nPeriodMa,0,nMAType,nMAPrice);
   if(MA_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iMA");
//---- получение хендла индикатора iMAl
   MAl_Handle=iMA(NULL,0,nPeriodMaLong,0,nMATypeLong,nMAPriceLong);
   if(MAl_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iMAl");
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(MathMax(MathMax(MathMax(nPeriodRsi,nPeriodRsiLong),nPeriodMa),nPeriodMaLong));
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ExtZerBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtZerBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,ExtMapBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtMapBuffer,true);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname="MaRsi-Trigger()";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| MaRsi-Trigger iteration function                                 | 
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
   if(BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(MAl_Handle)<rates_total
      || BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(RSIl_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- объявления локальных переменных 
   int to_copy,limit,bar;
   double res,MA_[],MAl_[],RSI_[],RSIl_[];
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
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA_)<=0) return(RESET);
   if(CopyBuffer(MAl_Handle,0,0,to_copy,MAl_)<=0) return(RESET);
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI_)<=0) return(RESET);
   if(CopyBuffer(RSIl_Handle,0,0,to_copy,RSIl_)<=0) return(RESET);
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(MA_,true);
   ArraySetAsSeries(MAl_,true);
   ArraySetAsSeries(RSI_,true);
   ArraySetAsSeries(RSIl_,true);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      res=0;
      if(MA_[bar] > MAl_[bar]) res = +1;
      if(MA_[bar] < MAl_[bar]) res = -1;
      //----
      if(RSI_[bar] > RSIl_[bar]) res += 1;
      if(RSI_[bar] < RSIl_[bar]) res -= 1;
      //----
      ExtMapBuffer[bar]=MathMax(MathMin(1,res),-1);
      ExtZerBuffer[bar]=0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
