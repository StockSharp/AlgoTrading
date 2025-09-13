//+---------------------------------------------------------------------+
//|                                               VolatilityQuality.mq5 | 
//|                                    Copyright © 2011, raff1410@o2.pl | 
//|                                                      raff1410@o2.pl | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, raff1410@o2.pl"
#property link "raff1410@o2.pl"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде многоцветной линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цветов трехцветной линии использованы
#property indicator_color1  clrBlue,clrMagenta
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 3
#property indicator_width1  3
//---- отображение метки индикатора
#property indicator_label1  "VolatilityQuality"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XmaMH,XmaML,XmaMO,XmaMC,XmaMC1;
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
enum App_price //тип константы
  {
   PRICE_CLOSE_=1,     //Close
   PRICE_MEDIAN_=5     //Median Price (HL/2)
  };
//+-----------------------------------+
//| Объявление перечислений           |
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
//| Входные параметры индикатора      |
//+-----------------------------------+
input Smooth_Method XMA_Method=MODE_LWMA; // Метод усреднения
input int XLength=5; // Глубина  сглаживания
input int XPhase=15; // Параметр сглаживания
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- для VIDIA это период CMO, для AMA это период медленной скользящей
input uint Smoothing=1; // Глубина  пересчета 
input uint Filter=5; // Фильтрация в пунктах ценового графика
input App_price Price=PRICE_MEDIAN; // Цена
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // Сдвиг индикатора по вертикали в пунктах
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[];
double ColorIndBuffer[];
//---- объявление переменной значения вертикального сдвига мувинга
double dPriceShift,dFilter;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates,Len;
//---- объявление глобальных переменных
int Count[];
double Mc[];
//+------------------------------------------------------------------+
//|  Пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
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
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates=GetStartBars(XMA_Method,XLength,XPhase);
   min_rates_total=int(min_rates+Smoothing+1);
   Len=int(Smoothing+1);
//---- инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
   dFilter=_Point*Filter;
//---- распределение памяти под массивы переменных  
   ArrayResize(Count,Len);
   ArrayResize(Mc,Len);
   ArrayInitialize(Count,0);
   ArrayInitialize(Mc,0.0);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   string Smooth=XmaMC.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"VolatilityQuality(",XLength,", ",Smooth,")");
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
//---- объявление переменных с плавающей точкой  
   double price,MH,ML,MC,MC1,MO,VQ,SumVQ,res1,res2;
   static double SumVQ_prev;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first,bar,clr;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=0; // стартовый номер для расчета всех баров
      SumVQ_prev=PriceSeries(Price,0,open,low,high,close);
      ArrayInitialize(Count,0);
      ArrayInitialize(Mc,SumVQ_prev);
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(Price,bar,open,low,high,close);
      Mc[Count[0]]=XmaMC.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price,bar,false);
      MC=Mc[Count[0]];
      MC1=Mc[Count[Smoothing]];
      if(bar==min_rates_total) SumVQ_prev=PriceSeries(Price,bar-1,open,low,high,close);
      MH=XmaMH.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,high[bar],bar,false);
      ML=XmaML.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,low[bar],bar,false);
      MO=XmaMO.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,open[bar],bar,false);
      res1=MathMax(MH-ML,MathMax(MH-MC1,MC1-ML));
      res2=MH-ML;
      if(res1 && res2) VQ=MathAbs(((MC-MC1)/res1+(MC-MO)/res2)*0.5)*((MC-MC1+(MC-MO))*0.5);
      else VQ=price;
      SumVQ=SumVQ_prev+VQ;
      if(Filter && MathAbs(SumVQ-SumVQ_prev)<dFilter) SumVQ=SumVQ_prev;
      IndBuffer[bar]=SumVQ+dPriceShift;
      //----
      if(bar<rates_total-1)
        {
         Recount_ArrayZeroPos(Count,Len);
         SumVQ_prev=SumVQ;
        }
     }
//---- корректировка значения переменной first
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=min_rates_total; // стартовый номер для расчета всех баров
//---- основной цикл раскраски сигнальной линии
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      if(IndBuffer[bar-1]<IndBuffer[bar]) clr=0;
      else if(IndBuffer[bar-1]>IndBuffer[bar]) clr=1;
      else clr=int(ColorIndBuffer[bar-1]);
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
