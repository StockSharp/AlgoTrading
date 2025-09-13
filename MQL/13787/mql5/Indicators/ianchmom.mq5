//+------------------------------------------------------------------+ 
//|                                                     iAnchMom.mq5 | 
//|                                            Copyright © 2007, NNN | 
//|                                                                  | 
//+------------------------------------------------------------------+  
#property copyright "Copyright © 2007, NNN"
#property link ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Объявление констант               |
//+-----------------------------------+
#define RESET 0 // константа для возврата терминалу команды на пересчет индикатора
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде цветной гистограммы
#property indicator_type1   DRAW_COLOR_HISTOGRAM
#property indicator_color1  clrRed,clrMagenta,clrGray,clrBlue,clrGreen
#property indicator_width1  2
#property indicator_label1  "iAnchMom"
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint SMAPeriod=34;                  // Период SMA
input uint EMAPeriod=20;                  // Период EMA
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // Ценовая константа, по которой производится расчет индикатора
input int Shift=0;                        // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- индикаторные буферы
double IndBuffer[];
double ColorIndBuffer[];
//---- объявление целочисленных переменных для хендлов индикаторов
int SMA_Handle,EMA_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+    
//| Momentum indicator initialization function                       | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- получение хендла индикатора SMA
   SMA_Handle=iMA(NULL,0,SMAPeriod,0,MODE_SMA,IPC);
   if(SMA_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора SMA");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора SMA
   EMA_Handle=iMA(NULL,0,EMAPeriod,0,MODE_EMA,IPC);
   if(EMA_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора EMA");
      return(INIT_FAILED);
     }
//---- инициализация переменных начала отсчета данных   
   min_rates_total=int(SMAPeriod+1);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorIndBuffer,true);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"iAnchMom(",SMAPeriod,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,4);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Momentum iteration function                                      | 
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
   if(BarsCalculated(SMA_Handle)<rates_total
      || BarsCalculated(EMA_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);
//---- объявление переменных с плавающей точкой  
   double SMA[],EMA[];
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int to_copy,limit,bar;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-1-min_rates_total+1; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }
//----
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(SMA_Handle,0,0,to_copy,SMA)<=0) return(RESET);
   if(CopyBuffer(EMA_Handle,0,0,to_copy,EMA)<=0) return(RESET);
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(SMA,true);
   ArraySetAsSeries(EMA,true);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      if(SMA[bar]) IndBuffer[bar]=100*((EMA[bar]/SMA[bar])-1.0);
      else IndBuffer[bar]=EMPTY_VALUE;
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) limit--;
//---- основной цикл раскраски индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      int clr=2;
      //----
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar+1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar+1]) clr=3;
        }
      //----
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar+1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar+1]) clr=1;
        }
      //----
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+ 
