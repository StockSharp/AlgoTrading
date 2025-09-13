//+------------------------------------------------------------------+
//|                                                       KPrmSt.mq5 |
//|                                         Copyright © 2010, LeMan. |
//|                                                 b-market@mail.ru |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2010, LeMan."
#property link      "b-market@mail.ru"
#property description "Стохастик Синтии Кейс"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора KPrmSt        |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета основной линии индикатора использован цвет MediumVioletRed
#property indicator_color1  clrMediumVioletRed
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//---- отображение метки линии индикатора
#property indicator_label1  "KPrmSt"
//+----------------------------------------------+
//| Параметры отрисовки сигнальной линии         |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета сигнальной линии индикатора использован цвет DodgerBlue
#property indicator_color2  clrDodgerBlue
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение метки линии индикатора
#property indicator_label2  "Signal"
//+----------------------------------------------+
//| Параметры размеров окна индикатора           |
//+----------------------------------------------+
#property indicator_minimum 0
#property indicator_maximum 100
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 80.0
#property indicator_level2 50.0
#property indicator_level3 20.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET 0       // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint  Per1=14;
input uint  Per2=3;
input uint  Per3=3;
input uint  Per4=5;
input uint  Shift=0; // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double IndBuffer[];
double SignalBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление глобальных переменных
int Count[];
double MaxArray[],MinArray[];
//+------------------------------------------------------------------+
//| Описание класса Moving_Average                                   |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+
//| Пересчет позиции самого нового элемента в массиве                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// возврат по ссылке номера текущего значения ценового ряда
                          int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;
//----
   Max2=Size;
   Max1=Max2-1;
//----
   count--;
   if(count<0) count=Max1;
//----
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(MathMax(Per1,Per4))+2;
//---- распределение памяти под массивы переменных  
   ArrayResize(Count,Per1);
   ArrayResize(MaxArray,Per1);
   ArrayResize(MinArray,Per1);
//----
   ArrayInitialize(Count,0);
   ArrayInitialize(MaxArray,0.0);
   ArrayInitialize(MinArray,0.0);
//---- превращение динамического массива IndBuffer[] в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);
//---- превращение динамического массива SignalBuffer[] в индикаторный буфер
   SetIndexBuffer(1,SignalBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2 на min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SignalBuffer,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"KPrmSt(",Per1,", ",Per2,", ",Per3,", ",Per4,", ",Shift,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(RESET);
//---- объявления локальных переменных 
   int limit,bar,maxbar,sh,sl,start1,start2;
   double res,Range,ind,sig;
//----
   maxbar=int(rates_total-Per1-1);
   start1=int(maxbar-Per4);
   start2=start1-1;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit=int(rates_total-Per4-1); // стартовый номер для расчета всех баров
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
//---- объявление переменных класса CMoving_Average из файла MASeries_Cls.mqh
   static CMoving_Average IND,SIG;
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      MaxArray[Count[0]]=high[ArrayMaximum(high,bar,Per4)];
      MinArray[Count[0]]=low [ArrayMinimum(low, bar,Per4)];
      //----
      if(bar>maxbar){Recount_ArrayZeroPos(Count,Per1); continue;}
      //----
      sh = ArrayMaximum(MaxArray,0,WHOLE_ARRAY);
      sl = ArrayMinimum(MinArray,0,WHOLE_ARRAY);
      //----
      Range=MaxArray[sh]-MinArray[sl];
      //----
      if(Range) res=NormalizeDouble((close[bar]-MinArray[sl])/(Range)*100,0);
      else res=50;
      //----
      ind=IND.EMASeries(start1,prev_calculated,rates_total,Per2,res,bar,true);
      sig=SIG.EMASeries(start2,prev_calculated,rates_total,Per3,ind,bar,true);
      //----
      IndBuffer[bar]=NormalizeDouble(ind,0);
      SignalBuffer[bar]=NormalizeDouble(sig,0);
      //----
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,Per1);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
