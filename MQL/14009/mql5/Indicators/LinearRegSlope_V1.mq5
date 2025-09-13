//+------------------------------------------------------------------+
//|                                            LinearRegSlope_V1.mq5 | 
//|                                Copyright © 2006, TrendLaboratory |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                   E-mail: igorad2003@yahoo.co.uk |
//|                                                                  |
//|                         Modified from LinearRegSlope_v1 by Toshi |
//|                                  http://toshi52583.blogspot.com/ |
//+------------------------------------------------------------------+
//| Для работы индикатора файл SmoothAlgorithms.mqh                  |
//| следует положить в папку: каталог_данных_терминала\MQL5\Include  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, TrendLaboratory"
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- номер версии индикатора
#property version   "1.11"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано всего два графических построения
#property indicator_plots   2
//+-----------------------------------+
//|  Параметры отрисовки индикатора 1 |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован DodgerBlue цвет
#property indicator_color1 clrDodgerBlue
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "Linear Reg Slope line"

//+-----------------------------------+
//|  Параметры отрисовки индикатора 2 |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета линии индикатора использован Coral цвет
#property indicator_color2 clrCoral
//---- линия индикатора - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width2  1
//---- отображение метки индикатора
#property indicator_label2  "Trigger line"

//+-----------------------------------+
//|  Описание класса CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1;
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
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price
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
input Smooth_Method SlMethod=MODE_SMA; //метод усреднения
input int SlLength=12; //глубина сглаживания                    
input int SlPhase=15; //параметр сглаживания,
                      //для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
// Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE;//ценовая константа
/* , по которой производится расчёт индикатора ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // сдвиг индикатора по горизонтали в барах
input uint TriggerShift=1; // cдвиг бара для тригера 
//+-----------------------------------+

//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double RegSlopeBuffer[],TriggerBuffer[];
//---- Объявление глобальных переменных
int TriggerShift_;
double Num2,SumBars;
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве кольцевых буферов
int Count[];
double Smooth[];
//+------------------------------------------------------------------+
//|  пересчёт позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos
(
 int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
 int Size // количество элементов в кольцевом буфере
 )
// Recount_ArrayZeroPos(count, SlLength)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=Size;
   Max1=Max2-1;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+   
//| XMA indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=XMA1.GetStartBars(SlMethod,SlLength,SlPhase);

//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("SlLength", SlLength);
   XMA1.XMAPhaseCheck("SlPhase", SlPhase, SlMethod);

//---- Инициализация переменных   
   SumBars=SlLength *(SlLength-1)*0.5;
   double SumSqrBars=(SlLength-1.0)*SlLength *(2.0*SlLength-1.0)/6.0;
   Num2=SumBars*SumBars-SlLength*SumSqrBars;
   TriggerShift_=int(min_rates_total+TriggerShift-1);

//---- Распределение памяти под массивы переменных  
   ArrayResize(Count,SlLength);
   ArrayResize(Smooth,SlLength);

//---- Инициализация массивов переменных
   ArrayInitialize(Count,0);
   ArrayInitialize(Smooth,0.0);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,RegSlopeBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(SlMethod);
   StringConcatenate(shortname,"Linear Reg Slope(",SlLength,", ",Smooth1,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- определение точности отображения значений индикатора
// IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| XMA iteration function                                           | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- Объявление переменных с плавающей точкой  
   double price_,Sum1,Sum2,SumY,Num1;
//---- Объявление целых переменных и получение уже посчитанных баров
   int first,bar,iii;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first=0; // стартовый номер для расчёта всех баров
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- Основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- Вызов функции PriceSeries для получения входной цены price_
      price_=PriceSeries(IPC,bar,open,low,high,close);
      Smooth[Count[0]]=XMA1.XMASeries(0,prev_calculated,rates_total,SlMethod,SlPhase,SlLength,price_,bar,false);

      Sum1=0;
      SumY=0;

      if(bar>SlLength)
         for(iii=0; iii<SlLength; iii++)
           {
            SumY+=Smooth[Count[iii]];
            Sum1+=iii*Smooth[Count[iii]];
           }

      Sum2=SumBars*SumY;
      Num1=SlLength*Sum1-Sum2;

      if(Num2!=0.0) RegSlopeBuffer[bar]=100*Num1/Num2;
      else          RegSlopeBuffer[bar]=EMPTY_VALUE;

      if(bar>TriggerShift_) TriggerBuffer[bar]=RegSlopeBuffer[bar-TriggerShift];
      else                 TriggerBuffer[bar]=EMPTY_VALUE;

      //---- пересчёт позиций элементов в кольцевом буфере Smooth[]
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,SlLength);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
