//+---------------------------------------------------------------------+
//|                                                            XRSX.mq5 | 
//|                                Copyright © 2011,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Relative Strength Index"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов
#property indicator_buffers 4 
//---- использовано всего одно графическое построение
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора 1             |
//+----------------------------------------------+
//---- отрисовка индикатора в виде цветной гистограммы
#property indicator_type1   DRAW_COLOR_HISTOGRAM
//---- в качестве цветов гистограммы использованы
#property indicator_color1 clrGray,clrGreen,clrBlue,clrRed,clrMagenta
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 3
#property indicator_width1  3
//---- отображение метки индикатора
#property indicator_label1  "XRSX"
//+----------------------------------------------+
//| Параметры отрисовки индикатора 2             |
//+----------------------------------------------+
//---- отрисовка индикатора в виде трехцветной линии
#property indicator_type2   DRAW_COLOR_LINE
//---- в качестве цвета линии индикатора использованы
#property indicator_color2 clrGray,clrLime,clrDarkOrange
//---- линия индикатора - штрих
#property indicator_style2  STYLE_DASH
//---- толщина линии индикатора равна 2
#property indicator_width2  2
//---- отображение метки индикатора
#property indicator_label2  "Signal"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1  -50.0
#property indicator_level2  +50.0
#property indicator_levelcolor clrViolet
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA UPXRSX,DNXRSX,XSIGN;
//+----------------------------------------------+
//| Объявление перечислений                      |
//+----------------------------------------------+
enum Applied_price_ //тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_   //TrendFollow_2 Price 
  };
//---
enum IndStyle //стиль отображения индикатора
  {
   COLOR_LINE = DRAW_COLOR_LINE,          //цветная линия
   COLOR_HISTOGRAM=DRAW_COLOR_HISTOGRAM,  //цветная гистограмма
   COLOR_ARROW=DRAW_COLOR_ARROW           //цветные значки
  };
//---
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
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method DSmoothMethod=MODE_JJMA; // Метод усреднения цены
input int DPeriod=15;  // Период скользящей средней
input int DPhase=100;  // Параметр усреднения скользящей средней
                       // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                       // для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method SSmoothMethod=MODE_JurX; // Метод усреднения сигнальной линии
input int SPeriod=7;  // Период сигнальной линии
input int SPhase=100; // Параметр сигнальной линии
                      // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                      // для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE; // Ценовая константа
/* , по которой производится расчет индикатора ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input IndStyle Style=COLOR_HISTOGRAM; // Стиль отображения XRSX
//+----------------------------------------------+
//---- объявление динамического массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double XRSX[],XXRSX[];
double ColorXRSX[],ColorXXRSX[];
//---- объявление целочисленных переменных начала отсчета данных
int StartBars,StartBarsD,StartBarsS;
//+------------------------------------------------------------------+   
//| XRSX indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   StartBarsD=UPXRSX.GetStartBars(DSmoothMethod,DPeriod,DPhase)+1;
   StartBarsS=StartBarsD+UPXRSX.GetStartBars(SSmoothMethod,SPeriod,SPhase);
   StartBars=StartBarsS;
//---- установка алертов на недопустимые значения внешних переменных
   UPXRSX.XMALengthCheck("DPeriod", DPeriod);
   UPXRSX.XMALengthCheck("SPeriod", SPeriod);
//---- установка алертов на недопустимые значения внешних переменных
   UPXRSX.XMAPhaseCheck("DPhase",DPhase,DSmoothMethod);
   UPXRSX.XMAPhaseCheck("SPhase",SPhase,SSmoothMethod);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,XRSX,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"XRSX");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- изменение стиля отображения индикатора   
   PlotIndexSetInteger(0,PLOT_DRAW_TYPE,Style);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorXRSX,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBarsD+1);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,XXRSX,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"Signal line");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(3,ColorXXRSX,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,StartBars+1);
//---- инициализация переменной для короткого имени индикатора
   string shortname,Smooth;
   Smooth=UPXRSX.GetString_MA_Method(DSmoothMethod);
   StringConcatenate(shortname,"Relative Strength Index(",string(DPeriod),",",Smooth,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| XRSX iteration function                                          | 
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
   if(rates_total<StartBars) return(0);
//---- объявление переменных с плавающей точкой  
   double dprice_,absdprice_,up_xrsx,dn_xrsx,xrsx,xxrsx;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first1,first2,first3,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first1=1; // стартовый номер для расчета всех баров
      first2=StartBarsD+1;
      first3=StartBars+1;
     }
   else
     {
      first1=prev_calculated-1; // стартовый номер для расчета новых баров
      first2=first1;
      first3=first1;
     }
//---- основной цикл расчета индикатора
   for(bar=first1; bar<rates_total && !IsStopped(); bar++)
     {
      //---- вызов функции PriceSeries для получения приращения входной цены dprice_
      dprice_=PriceSeries(IPC,bar,open,low,high,close)-PriceSeries(IPC,bar-1,open,low,high,close);
      absdprice_=MathAbs(dprice_);
      //---- два вызова функции XMASeries
      up_xrsx = UPXRSX.XMASeries(1, prev_calculated, rates_total, DSmoothMethod, DPhase, DPeriod,    dprice_, bar, false);
      dn_xrsx = DNXRSX.XMASeries(1, prev_calculated, rates_total, DSmoothMethod, DPhase, DPeriod, absdprice_, bar, false);
      //---- предотвращение деления на ноль на пустых значениях
      if(dn_xrsx==0) xrsx=EMPTY_VALUE;
      else
        {
         xrsx=up_xrsx/dn_xrsx;
         //---- ограничение индикатора сверху и снизу 
         if(xrsx > +1)xrsx = +1;
         if(xrsx < -1)xrsx = -1;
        }
      //---- загрузка полученного значения в индикаторный буфер
      XRSX[bar]=xrsx*100;
      xxrsx=XSIGN.XMASeries(StartBarsD,prev_calculated,rates_total,SSmoothMethod,SPhase,SPeriod,XRSX[bar],bar,false);
      //---- загрузка полученного значения в индикаторный буфер
      XXRSX[bar]=xxrsx;
     }
//---- основной цикл раскраски индикатора
   for(bar=first2; bar<rates_total && !IsStopped(); bar++)
     {
      ColorXRSX[bar]=0;
      //----
      if(XRSX[bar]>0)
        {
         if(XRSX[bar]>XRSX[bar-1]) ColorXRSX[bar]=1;
         if(XRSX[bar]<XRSX[bar-1]) ColorXRSX[bar]=2;
        }
      //----
      if(XRSX[bar]<0)
        {
         if(XRSX[bar]<XRSX[bar-1]) ColorXRSX[bar]=3;
         if(XRSX[bar]>XRSX[bar-1]) ColorXRSX[bar]=4;
        }
     }
//---- основной цикл раскраски сигнальной линии
   for(bar=first3; bar<rates_total && !IsStopped(); bar++)
     {
      ColorXXRSX[bar]=0;
      if(XRSX[bar]>XXRSX[bar-1]) ColorXXRSX[bar]=1;
      if(XRSX[bar]<XXRSX[bar-1]) ColorXXRSX[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
