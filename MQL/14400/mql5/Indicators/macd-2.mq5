//+------------------------------------------------------------------+ 
//|                                                       MACD-2.mq5 | 
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---- номер версии индикатора
#property version   "1.00"
#property description "MACD-2"
//---- номер версии индикатора
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 4
#property indicator_buffers 4 
//---- использовано два графических построения
#property indicator_plots   2
//+-----------------------------------+
//| Объявление констант               |
//+-----------------------------------+
#define RESET  0 // константа для возврата терминалу команды на пересчет индикатора
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветов индикатора использованы
#property indicator_color1  clrLime,clrDeepPink
//---- отображение метки индикатора
#property indicator_label1  "MACD_Cloud"
//+----------------------------------------------+
//| Параметры отрисовки индикатора 2             |
//+----------------------------------------------+
//---- отрисовка индикатора в виде четырехцветной гистограммы
#property indicator_type2 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов пятицветной гистограммы использованы
#property indicator_color2 clrBrown,clrViolet,clrGray,clrDeepSkyBlue,clrBlue
//---- линия индикатора - сплошная
#property indicator_style2 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width2 2
//---- отображение метки индикатора
#property indicator_label2  "MACD"
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint FastMACD     = 12;
input uint SlowMACD     = 26;
input uint SignalMACD   = 9;
input ENUM_APPLIED_PRICE   PriceMACD=PRICE_CLOSE;
//+-----------------------------------+
//---- объявление целочисленных переменных начала отсчета данных
int  min_rates_total;
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ExtABuffer[],ExtBBuffer[];
double IndBuffer[],ColorIndBuffer[];
//---- объявление целочисленных переменных для хендлов индикаторов
int MACD_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(SignalMACD+MathMax(FastMACD,SlowMACD));
//---- получение хендла индикатора iMACD
   MACD_Handle=iMACD(NULL,0,FastMACD,SlowMACD,SignalMACD,PriceMACD);
   if(MACD_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMACD");
      return(INIT_FAILED);
     }
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ExtABuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtABuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,ExtBBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtBBuffer,true);

//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(2,IndBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(3,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorIndBuffer,true);

//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);

//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"MACD-2");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[])
  {
//---- проверка количества баров на достаточность для расчета
   if(BarsCalculated(MACD_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//---- объявления локальных переменных 
   int to_copy,limit;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
   else limit=rates_total-prev_calculated;  // стартовый номер для расчета только новых баров
//----
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(MACD_Handle,MAIN_LINE,0,to_copy,ExtABuffer)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle,SIGNAL_LINE,0,to_copy,ExtBBuffer)<=0) return(RESET);
//---- основной цикл расчета индикатора
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ExtABuffer[bar]/=_Point;
      ExtBBuffer[bar]/=_Point;
      IndBuffer[bar]=3*(ExtABuffer[bar]-ExtBBuffer[bar]);
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) limit--;
//---- основной цикл раскраски индикатора Ind
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
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
