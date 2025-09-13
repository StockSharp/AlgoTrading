//+---------------------------------------------------------------------+
//|                                                    Ichimoku_Osc.mq5 | 
//|                                               Copyright © 2010, MDM | 
//|                                                                     | 
//+---------------------------------------------------------------------+
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): terminal_data_folder\MQL5\Include             |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2010, MDM"
#property link ""
#property description "Ichimoku_Osc"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов
#property indicator_buffers 4 
//---- использовано всего два графических построения
#property indicator_plots   2
//+-----------------------------------+
//| Объявление констант               |
//+-----------------------------------+
#define RESET 0 // константа для возврата терминалу команды на пересчет индикатора
//+-----------------------------------+
//| Параметры отрисовки индикатора 1  |
//+-----------------------------------+
//---- отрисовка индикатора в виде трехцветной линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цвета линии индикатора использованы
#property indicator_color1 clrGray,clrDeepSkyBlue,clrDeepPink
//---- линия индикатора - сплошная
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 4
#property indicator_width1  4
//---- отображение метки индикатора
#property indicator_label1  "Signal"
//+-----------------------------------+
//| Параметры отрисовки индикатора 2  |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
//#property indicator_type2   DRAW_LINE
//---- в качестве цветов гистограммы использованы
#property indicator_color2 clrGray
//---- линия индикатора - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width2  2
//---- отображение метки индикатора
#property indicator_label2  "Ichimoku Oscillator"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XSIGN;
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
enum IndStyle //стиль отображения индикатора
  {
   LINE = DRAW_LINE,          //линия
   ARROW=DRAW_ARROW,          //значки
   HISTOGRAM=DRAW_HISTOGRAM   //гистограмма
  };
//+-----------------------------------+
/*enum Smooth_Method - объявлено в файле SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input int Tenkan=9;      // Tenkan-sen
input int Kijun=26;      // Kijun-sen
input int Senkou=52;     // Senkou Span B
//---
input Smooth_Method SSmoothMethod=MODE_JJMA; // Метод усреднения сигнальной линии
input int SPeriod=7;  // Период сигнальной линии
input int SPhase=100; // Параметр сигнальной линии
                      // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
// для VIDIA это период CMO, для AMA это период медленной скользящей
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input IndStyle Style=DRAW_ARROW; // Стиль отображения осциллятора
//+-----------------------------------+
//---- объявление динамического массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double Osc[],XOsc[];
double ColorXOsc[];
//---- объявление целочисленных переменных для хендлов индикаторов
int Ich_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_;
//+------------------------------------------------------------------+   
//| Osc indicator initialization function                            | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//---- инициализация переменных начала отсчета данных   
   min_rates_=int(MathMax(MathMax(Tenkan,Kijun),Senkou));
   min_rates_total=min_rates_+GetStartBars(SSmoothMethod,SPeriod,SPhase);
//---- получение хендла индикатора Ichimoku_Calc
   Ich_Handle=iCustom(NULL,PERIOD_CURRENT,"Ichimoku_Calc",Tenkan,Kijun,Senkou,0);
   if(Ich_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Ichimoku_Calc");
      return(INIT_FAILED);
     }
//---- установка алертов на недопустимые значения внешних переменных
   XSIGN.XMALengthCheck("SPeriod",SPeriod);
//---- установка алертов на недопустимые значения внешних переменных
   XSIGN.XMAPhaseCheck("SPhase",SPhase,SSmoothMethod);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,XOsc,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(XOsc,true);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorXOsc,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorXOsc,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,Osc,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- изменение стиля отображения индикатора   
   PlotIndexSetInteger(1,PLOT_DRAW_TYPE,Style);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(Osc,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname,Smooth;
   Smooth=XSIGN.GetString_MA_Method(SSmoothMethod);
   StringConcatenate(shortname,"Ichimoku Oscillator(",string(Tenkan),",",Smooth,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| Osc iteration function                                           | 
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
   if(BarsCalculated(Ich_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//---- объявление переменных с плавающей точкой  
   double markt,trend,TS[],KS[],SA[],CS[];
//---- объявление целочисленных переменных
   int bar,limit,maxbar,to_copy;
//----
   maxbar=rates_total-1-min_rates_total;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit=rates_total-min_rates_-1; // стартовый номер для расчета всех баров
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров 
//----
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(Ich_Handle,TENKANSEN_LINE,0,to_copy,TS)<=0) return(RESET);
   if(CopyBuffer(Ich_Handle,KIJUNSEN_LINE,0,to_copy,KS)<=0) return(RESET);
   if(CopyBuffer(Ich_Handle,SENKOUSPANB_LINE,0,to_copy,SA)<=0) return(RESET);
   if(CopyBuffer(Ich_Handle,CHINKOUSPAN_LINE,0,to_copy,CS)<=0) return(RESET);
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(TS,true);
   ArraySetAsSeries(KS,true);
   ArraySetAsSeries(SA,true);
   ArraySetAsSeries(CS,true);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      markt=(CS[bar]-SA[bar]);
      trend=(TS[bar]-KS[bar]);
      //----
      Osc[bar]=(markt-trend)/_Point;
      //---- загрузка полученного значения в индикаторный буфер
      XOsc[bar]=XSIGN.XMASeries(maxbar,prev_calculated,rates_total,SSmoothMethod,SPhase,SPeriod,Osc[bar],bar,true);
     }
//---- основной цикл раскраски сигнальной линии
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ColorXOsc[bar]=0;
      if(XOsc[bar]>XOsc[bar+1]) ColorXOsc[bar]=1;
      if(XOsc[bar]<XOsc[bar+1]) ColorXOsc[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
