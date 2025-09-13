//+---------------------------------------------------------------------+
//|                                                  ColorHMA_StDev.mq5 |
//|                                  Copyright © 2010, Nikolay Kositsin |
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "2010,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"

//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- для расчёта и отрисовки индикатора использовано шесть буферов
#property indicator_buffers 6
//---- использовано всего пять графических построений
#property indicator_plots   5
//+----------------------------------------------+
//|  Параметры отрисовки линии  индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цветов трёхцветной линии использованы
#property indicator_color1  clrGray,clrMediumPurple,clrRed
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  2
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован розовый цвет
#property indicator_color2  clrMagenta
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение медвежьей метки индикатора
#property indicator_label2  "Dn_Signal 1"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде символа
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован синий цвет
#property indicator_color3  clrBlue
//---- толщина линии индикатора 3 равна 2
#property indicator_width3  2
//---- отображение бычей метки индикатора
#property indicator_label3  "Up_Signal 1"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде символа
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован розовый цвет
#property indicator_color4  clrMagenta
//---- толщина линии индикатора 4 равна 4
#property indicator_width4  4
//---- отображение медвежьей метки индикатора
#property indicator_label4  "Dn_Signal 2"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 5 в виде символа
#property indicator_type5   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован синий цвет
#property indicator_color5  clrBlue
//---- толщина линии индикатора 5 равна 4
#property indicator_width5  4
//---- отображение бычей метки индикатора
#property indicator_label5  "Up_Signal 2"
//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input uint HMA_Period=13; // Период мувинга
input double dK1=1.5;  //коэффициент 1 для квадратичного фильтра
input double dK2=2.5;  //коэффициент 2 для квадратичного фильтра
input uint std_period=9; //период квадратичного фильтра
input int PriceShift=0; // cдвиг Фатла по вертикали в пунктах
input int Shift=0; // Сдвиг мувинга по горизонтали в барах
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double ExtLineBuffer[],ColorExtLineBuffer[];
double BearsBuffer1[],BullsBuffer1[];
double BearsBuffer2[],BullsBuffer2[];
//---- Объявление целых переменных
int Hma2_Period,Sqrt_Period;
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total,min_rates;
double dPriceShift,dHMA[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//----  
  Hma2_Period=int(MathFloor(HMA_Period/2));
  Sqrt_Period=int(MathFloor(MathSqrt(HMA_Period)));
//---- Инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
  
//---- Инициализация переменных начала отсчёта данных
   min_rates=int(HMA_Period)+Sqrt_Period;
   min_rates_total=min_rates+int(std_period);
//---- Распределение памяти под массивы переменных  
   ArrayResize(dHMA,std_period);
   
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига скользящей средней по горизонтали на HMAShift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора HMAPeriod
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- превращение динамического массива BearsBuffer в индикаторный буфер
   SetIndexBuffer(2,BearsBuffer1,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива BullsBuffer в индикаторный буфер
   SetIndexBuffer(3,BullsBuffer1,INDICATOR_DATA);
//---- осуществление сдвига индикатора 3 по горизонтали
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива BearsBuffer в индикаторный буфер
   SetIndexBuffer(4,BearsBuffer2,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива BullsBuffer в индикаторный буфер
   SetIndexBuffer(5,BullsBuffer2,INDICATOR_DATA);
//---- осуществление сдвига индикатора 3 по горизонтали
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(4,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,EMPTY_VALUE);
      
//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- имя для окон данных и лэйба для субъокон 
   string short_name="ColorHMA_StDev";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name+"("+string(HMA_Period)+")");
//----
  }
//+------------------------------------------------------------------+
// Описание класса CMoving_Average                                   | 
//+------------------------------------------------------------------+  
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+ 
//|  Moving Average                                                  |
//+------------------------------------------------------------------+
int OnCalculate
(
 const int rates_total,// количество истории в барах на текущем тике
 const int prev_calculated,// количество истории в барах на предыдущем тике
 const int begin,// номер начала достоверного отсчёта баров
 const double &price[]// ценовой массив для расчёта индикатора
 )
  { 
   int begin0=min_rates_total+begin; 
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<begin0) return(0);

//---- объявления локальных переменных 
   int first,bar,begin1;
   double lwma1,lwma2,dma,hma;
   double SMAdif,Sum,StDev,dstd,BEARS1,BULLS1,BEARS2,BULLS2,Filter1,Filter2;  
   begin1=int(HMA_Period)+begin;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated==0) // проверка на первый старт расчёта индикатора
     {
      first=begin; // стартовый номер для расчёта всех баров      
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,begin0+1);
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- объявление переменной класса CMoving_Average из файла HMASeries_Cls.mqh
   static CMoving_Average MA1,MA2,MA3;

//---- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      lwma1=MA1.LWMASeries(begin,prev_calculated,rates_total,Hma2_Period,price[bar],bar,false);
      lwma2=MA2.LWMASeries(begin,prev_calculated,rates_total,HMA_Period, price[bar],bar,false);
      dma=2*lwma1-lwma2;      
      ExtLineBuffer[bar]=MA3.LWMASeries(begin1,prev_calculated,rates_total,Sqrt_Period,dma,bar,false);
     }
     
//---- пересчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first=begin0;

//---- Основной цикл раскраски сигнальной линии
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      ColorExtLineBuffer[bar]=0;
      if(ExtLineBuffer[bar-1]<ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=1;
      if(ExtLineBuffer[bar-1]>ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=2;
     }
     
//---- пересчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first= begin0;
//---- основной цикл расчёта индикатора стандартных отклонений
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- загружаем приращения индикатора в массив для промежуточных вычислений
      for(int iii=0; iii<int(std_period); iii++) dHMA[iii]=ExtLineBuffer[bar-iii]-ExtLineBuffer[bar-iii-1];

      //---- находим простое среднее приращений индикатора
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dHMA[iii];
      SMAdif=Sum/std_period;

      //---- находим сумму квадратов разностей приращений и среднего
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dHMA[iii]-SMAdif,2);

      //---- определяем итоговое значение среднеквадратичного отклонения StDev от приращения индикатора
      StDev=MathSqrt(Sum/std_period);

      int dig=_Digits+2;
      //---- инициализация переменных
      dstd=NormalizeDouble(dHMA[0],dig);
      Filter1=NormalizeDouble(dK1*StDev,dig);
      Filter2=NormalizeDouble(dK2*StDev,dig);
      BEARS1=EMPTY_VALUE;
      BULLS1=EMPTY_VALUE;
      BEARS2=EMPTY_VALUE;
      BULLS2=EMPTY_VALUE;
      hma=ExtLineBuffer[bar];

      //---- вычисление индикаторных значений
      if(dstd<-Filter1 && dstd>=-Filter2) BEARS1=hma; //есть нисходящий тренд
      if(dstd<-Filter2) BEARS2=hma; //есть нисходящий тренд
      if(dstd>+Filter1 && dstd<=+Filter2) BULLS1=hma; //есть восходящий тренд
      if(dstd>+Filter2) BULLS2=hma; //есть восходящий тренд

      //---- инициализация ячеек индикаторных буферов полученными значениями 
      BullsBuffer1[bar]=BULLS1;
      BearsBuffer1[bar]=BEARS1;
      BullsBuffer2[bar]=BULLS2;
      BearsBuffer2[bar]=BEARS2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
