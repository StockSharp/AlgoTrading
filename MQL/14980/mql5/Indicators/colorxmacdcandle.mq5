//+---------------------------------------------------------------------+
//|                                                ColorXMACDCandle.mq5 |
//|                                  Copyright © 2016, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- для расчета и отрисовки индикатора использовано семь буферов
#property indicator_buffers 7
//---- использовано всего два графических построения
#property indicator_plots   2
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- в качестве индикатора использованы цветные свечи
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrMagenta,clrGray,clrBlue
//---- отображение метки индикатора
#property indicator_label1  "MACDCandle Open;MACDCandle High;MACDCandle Low;MACDCandle Close"
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде трёхцветной линии
#property indicator_type2 DRAW_COLOR_LINE
//---- в качестве цветов трёхцветной линии использованы
#property indicator_color2 clrGray,clrLime,clrRed
//---- линия индикатора - сплошная линия
#property indicator_style2 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width2 2
//---- отображение метки сигнальной линии
#property indicator_label2  "Signal Line"
//+-----------------------------------+
//|  Описание классов усреднений      |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5,XMA6,XMA7,XMA8,XMA9;
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_            //Low
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
input Smooth_Method XMA_Method=MODE_T3; //метод усреднения гистограммы
input int Fast_XMA = 12; //период быстрого мувинга
input int Slow_XMA = 26; //период медленного мувинга
input int XPhase = 100;  //параметр усреднения мувингов,
                       //для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
// Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method Signal_Method=MODE_JJMA; //метод усреднения сигнальной линии
input int Signal_XMA=9; //период сигнальной линии 
input int Signal_Phase=100; // параметр сигнальной линии,
                            //изменяющийся в пределах -100 ... +100,
//влияет на качество переходного процесса;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//ценовая константа сигнальной линии
//+-----------------------------------+
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total,min_rates_1;
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double ExtOpenBuffer[],ExtHighBuffer[],ExtLowBuffer[],ExtCloseBuffer[],ExtColorBuffer[];
double SignBuffer[],ColorSignBuffer[];
//+------------------------------------------------------------------+    
//| XMACD indicator initialization function                          | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_1=MathMax(GetStartBars(XMA_Method,Fast_XMA,XPhase),GetStartBars(XMA_Method,Slow_XMA,XPhase));
   min_rates_total=min_rates_1+GetStartBars(Signal_Method,Signal_XMA,Signal_Phase)+2;

//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
   
//---- превращение динамического массива SignBuffer в индикаторный буфер
   SetIndexBuffer(5,SignBuffer,INDICATOR_DATA);
   
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(6,ColorSignBuffer,INDICATOR_COLOR_INDEX);   
   
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);


//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Fast_XMA", Fast_XMA);
   XMA1.XMALengthCheck("Slow_XMA", Slow_XMA);
   XMA1.XMALengthCheck("Signal_XMA", Signal_XMA);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("XPhase", XPhase, XMA_Method);
   XMA1.XMAPhaseCheck("Signal_Phase", Signal_Phase, Signal_Method);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(XMA_Method);
   string Smooth2=XMA1.GetString_MA_Method(Signal_Method);
   StringConcatenate(shortname,
                     "XMACD( ",Fast_XMA,", ",Slow_XMA,", ",Signal_XMA,", ",Smooth1,", ",Smooth2," )");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| XMACD iteration function                                         | 
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
//---- Проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- Объявление целых переменных
   int first1,first2,bar;
//---- Объявление переменных с плавающей точкой  
   double fast_xma,slow_xma,sign_xma=0.0,oxmacd,cxmacd,hxmacd,lxmacd,Max,Min;

//---- Инициализация индикатора в блоке OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      first1=0; // стартовый номер для расчёта всех баров первого цикла
      first2=min_rates_total+1; // стартовый номер для расчёта всех баров второго цикла
     }
   else // стартовый номер для расчёта новых баров
     {
      first1=prev_calculated-1;
      first2=first1;
     }

//---- Основной цикл расчёта индикатора
   for(bar=first1; bar<rates_total; bar++)
     {
      fast_xma=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,open[bar],bar,false);
      slow_xma=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,open[bar],bar,false);
      oxmacd=(fast_xma-slow_xma)/_Point;
      //----
      fast_xma=XMA3.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,close[bar],bar,false);
      slow_xma=XMA4.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,close[bar],bar,false);
      cxmacd=(fast_xma-slow_xma)/_Point;
      //----
      fast_xma=XMA5.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,high[bar],bar,false);
      slow_xma=XMA6.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,high[bar],bar,false);
      hxmacd=(fast_xma-slow_xma)/_Point;
      //----
      fast_xma=XMA7.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,low[bar],bar,false);
      slow_xma=XMA8.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,low[bar],bar,false);
      lxmacd=(fast_xma-slow_xma)/_Point;
      //---- исправление и окрашивания свечей
      Max=MathMax(oxmacd,cxmacd);
      Min=MathMin(oxmacd,cxmacd);     
      ExtCloseBuffer[bar]=cxmacd;
      ExtOpenBuffer[bar]=oxmacd;
      ExtHighBuffer[bar]=MathMax(Max,hxmacd);
      ExtLowBuffer[bar]=MathMin(Min,lxmacd);
      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=2.0;
      else if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else ExtColorBuffer[bar]=1.0;
      //---- 
      switch(AppliedPrice)
        {
         case PRICE_OPEN_ :
           {
            sign_xma=XMA9.XMASeries(min_rates_1,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,oxmacd,bar,false);
            break;
           }
         case PRICE_CLOSE_ :
           {
            sign_xma=XMA9.XMASeries(min_rates_1,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,cxmacd,bar,false);
            break;
           }
         case PRICE_HIGH_ :
           {
            sign_xma=XMA9.XMASeries(min_rates_1,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,hxmacd,bar,false);
            break;
           }
         case PRICE_LOW_ :
           {
            sign_xma=XMA9.XMASeries(min_rates_1,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,lxmacd,bar,false);
           }
        }
      SignBuffer[bar] = sign_xma;
     }

//---- Основной цикл раскраски сигнальной линии
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorSignBuffer[bar]=0;
      if(SignBuffer[bar]>SignBuffer[bar-1]) ColorSignBuffer[bar]=1;
      if(SignBuffer[bar]<SignBuffer[bar-1]) ColorSignBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
