//+---------------------------------------------------------------------+
//|                                   Chaikin_Volatility_Stochastic.mq5 |
//|                                            Copyright © 2007, Giaras |
//|                                       giampiero.raschetti@gmail.com |
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2007, Giaras"
#property link      "giampiero.raschetti@gmail.com"
//---- номер версии индикатора
#property version   "1.11"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветjd индикатора использованы
#property indicator_color1  clrBlue,clrRed
//---- отображение метки индикатора
#property indicator_label1  "Chaikin_Volatility_Stochastic"
//+-----------------------------------+
//|  Описание класса CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменных класса CXMA из файла xrangeAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
/*enum Smooth_Method - перечисление объявлено в файле xrangeAlgorithms.mqh
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
   MODE_AMA    //AMA
  }; */
//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input Smooth_Method XMA_Method=MODE_EMA_; //метод усреднения
input int XLength=10; //глубина сглаживания                    
input int XPhase=15; //параметр сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input uint StocLength= 5;
input uint WMALength = 5;
input int Shift=0; // сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double IndBuffer[],TriggerBuffer[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total,min_rates_1,min_rates_2;
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве кольцевых буферов
int Count[];
double xrange[];
//+------------------------------------------------------------------+
//|  Пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
                          int Size)
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
//| Chaikin_Volatility indicator initialization function             | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_1=XMA1.GetStartBars(XMA_Method,XLength,XPhase);
   min_rates_2=min_rates_1+int(StocLength);
   min_rates_total=min_rates_2+int(WMALength+1);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);

//---- Распределение памяти под массивы переменных  
   ArrayResize(Count,StocLength);
   ArrayResize(xrange,StocLength);

//---- Инициализация массивов переменных
   ArrayInitialize(Count,0.0);
   ArrayInitialize(xrange,0.0);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора

//---- превращение динамического массива TriggerBuffer[] в индикаторный буфер
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);

//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"Chaikin_Volatility_Stochastic");

//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| Chaikin_Volatility iteration function                            | 
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

//---- Объявление целых переменных и получение уже посчитанных баров
   int first,bar;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=0; // стартовый номер для расчёта всех баров
      ArrayInitialize(Count,0.0);
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- Основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double range=high[bar]-low[bar];
      xrange[Count[0]]=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,range,bar,false);
      
      double _chakin=xrange[Count[0]];
      double hh=_chakin,ll=_chakin;
      for(int iii=0; iii<int(StocLength); iii++)
        {
         double tmp=xrange[Count[iii]];
         hh=MathMax(hh,tmp);
         ll=MathMin(ll,tmp);
        }
      double Value1=_chakin-ll;
      double Value2=hh-ll;
      double Value3=NULL;
      if(Value2) Value3=Value1/Value2;
      IndBuffer[bar]=100*XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,MODE_LWMA_,0,WMALength,Value3,bar,false);
      TriggerBuffer[bar]=IndBuffer[MathMax(bar-1,0)];
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,StocLength);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
