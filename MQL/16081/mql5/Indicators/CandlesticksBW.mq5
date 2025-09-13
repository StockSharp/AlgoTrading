//+------------------------------------------------------------------+
//|                                               CandlesticksBW.mq5 |
//|                                       Copyright © 2008, Vladimir | 
//|                                         finance@allmotion.com.ua | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Vladimir"
#property link "finance@allmotion.com.ua"
#property description "CandlesticksBW"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window
//---- для расчета и отрисовки индикатора использовано пять буферов
#property indicator_buffers 5
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- в качестве индикатора использованы цветные свечи
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrAqua,clrBlue,clrGreen,clrRed,clrPurple,clrMagenta
//---- отображение метки индикатора
#property indicator_label1  "CandlesticksBW/Open;High;Low;Close"
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET  0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+

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
int AC_Handle,AO_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- инициализация глобальных переменных 
   min_rates_total=34+2;
//---- получение хендла индикатора   Awesome oscillator 
   AO_Handle=iAO(Symbol(),PERIOD_CURRENT);
   if(AO_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора   Awesome oscillator");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора  Accelerator Oscillator 
   AC_Handle=iAC(Symbol(),PERIOD_CURRENT);
   if(AC_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора  Accelerator Oscillator");
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
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для субъокон 
   string short_name="CandlesticksBW";
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
   if(BarsCalculated(AO_Handle)<rates_total
      || BarsCalculated(AC_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- объявления локальных переменных 
   int to_copy,limit,bar;
   double AO[],AC[];
//---- расчеты необходимого количества копируемых данных и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }
//---
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyOpen(Symbol(),PERIOD_CURRENT,0,to_copy,ExtOpenBuffer)<=0) return(RESET);
   if(CopyHigh(Symbol(),PERIOD_CURRENT,0,to_copy,ExtHighBuffer)<=0) return(RESET);
   if(CopyLow(Symbol(),PERIOD_CURRENT,0,to_copy,ExtLowBuffer)<=0) return(RESET);
   if(CopyClose(Symbol(),PERIOD_CURRENT,0,to_copy,ExtCloseBuffer)<=0) return(RESET);
   to_copy++;
   if(CopyBuffer(AO_Handle,0,0,to_copy,AO)<=0) return(RESET);
   if(CopyBuffer(AC_Handle,0,0,to_copy,AC)<=0) return(RESET);

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(AO,true);
   ArraySetAsSeries(AC,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);

//---- основной цикл исправления и окрашивания свечей
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      int clr;
      
      if(AO[bar]>=AO[bar+1] && AC[bar]>=AC[bar+1])
        {
         if(open[bar]<=close[bar]) clr=0;
         else clr=1;
        }
      else
      if(AO[bar]<=AO[bar+1] && AC[bar]<=AC[bar+1])
        {
         if(open[bar]>=close[bar]) clr=5;
         else clr=4;
        }
      else
        {
         if(open[bar]<=close[bar]) clr=2;
         else clr=3;        
        }
      ExtColorBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
