//+---------------------------------------------------------------------+
//|                                                       ForexLine.mq5 | 
//|                                             Copyright © 2015, 3rjfx | 
//|                                 https://www.mql5.com/en/users/3rjfx | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2015, 3rjfx"
#property link "https://www.mql5.com/en/users/3rjfx"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде многоцветной линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цветов трехцветной линии использованы
#property indicator_color1  clrLimeGreen,clrGray,clrMagenta
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 3
#property indicator_width1  3
//---- отображение метки индикатора
#property indicator_label1  "IndBuffer"

//+-----------------------------------+
//|  Описание класса CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4;
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
enum Applied_price_ //Тип константы
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
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
/*enum Smooth_Method - перечисление объявлено в файле SmoothAlgorithms.mqh
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
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input Smooth_Method MA_Method11=MODE_LWMA; //Метод усреднения первого сглаживания мувинга 1
input int Length11=5; //Глубина  первого сглаживания  мувинга 1                   
input int Phase11=15; //Параметр первого сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method12=MODE_LWMA; //Метод усреднения второго сглаживания 
input int Length12=10; //Глубина  второго сглаживания  мувинга 1
input int Phase12=15;  //Параметр второго сглаживания  мувинга 1,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC1=PRICE_CLOSE;//Ценовая константа мувинга 1
//----
input Smooth_Method MA_Method21=MODE_LWMA; //Метод усреднения первого сглаживания мувинга 2
input int Length21=20; //Глубина  первого сглаживания мувинга 2                   
input int Phase21=15; //Параметр первого сглаживания мувинга 2,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method22=MODE_LWMA; //Метод усреднения второго сглаживания мувинга 2
input int Length22=20; //Глубина  второго сглаживания мувинга 2
input int Phase22=15;  //Параметр второго сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC2=PRICE_CLOSE;//Ценовая константа мувинга 2
//----
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // Сдвиг индикатора по вертикали в пунктах
//+-----------------------------------+

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[];
double ColorIndBuffer[];

//---- Объявление переменной значения вертикального сдвига мувинга
double dPriceShift;
//---- Объявление целых переменных начала отсчета данных
int min_rates_total,min_rates_1,min_rates_2;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчета данных
   min_rates_1=GetStartBars(MA_Method11,Length11,Phase11);
   min_rates_2=GetStartBars(MA_Method21,Length21,Phase21);
   
   int min_rates_12=GetStartBars(MA_Method12,Length12,Phase12);
   int min_rates_22=GetStartBars(MA_Method22,Length22,Phase22);
   
   min_rates_total=MathMax(min_rates_1+min_rates_12,min_rates_2+min_rates_22);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Length11",Length11);
   XMA2.XMALengthCheck("Length12",Length12);
   XMA3.XMALengthCheck("Length21",Length21);
   XMA4.XMALengthCheck("Length22",Length22);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("Phase11", Phase11,MA_Method11);
   XMA2.XMAPhaseCheck("Phase12", Phase12,MA_Method12);
   XMA3.XMAPhaseCheck("Phase21", Phase21,MA_Method21);
   XMA4.XMAPhaseCheck("Phase22", Phase22,MA_Method22);

//---- Инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);

//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"ForexLine(",Length11,", ",Length12,", ",Length21,", ",Length21,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| Custom indicator iteration function                              | 
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
   if(rates_total<min_rates_total) return(0);

//---- Объявление переменных с плавающей точкой  
   double price,x1xma,x2xma,x3xma,x4xma;
//---- Объявление целых переменных и получение уже посчитанных баров
   int first,bar,clr;

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=0; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров

//---- Основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC1,bar,open,low,high,close);
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method11,Phase11,Length11,price,bar,false);
      x2xma=XMA2.XMASeries(min_rates_1,prev_calculated,rates_total,MA_Method12,Phase12,Length12,x1xma,bar,false);
      //----
      price=PriceSeries(IPC2,bar,open,low,high,close);
      x3xma=XMA3.XMASeries(0,prev_calculated,rates_total,MA_Method21,Phase21,Length21,price,bar,false);
      x4xma=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,MA_Method22,Phase22,Length22,x3xma,bar,false);
      //----      
      IndBuffer[bar]=x4xma+dPriceShift;
      //---- раскрашиваем индикатор
      clr=1;
      if(x2xma>x4xma) clr=0;
      if(x2xma<x4xma) clr=2;
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
