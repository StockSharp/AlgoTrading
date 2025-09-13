//+------------------------------------------------------------------+
//|                                            XMA_Range_Channel.mq5 |
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//| Для работы  индикатора файл SmoothAlgorithms.mqh                 |
//| следует положить в папку: каталог_данных_терминала\MQL5\Include  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, ellizii"
#property link ""
#property description "XMA Ichimoku Channel"
//---- номер версии индикатора
#property version   "1.02"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов 9
#property indicator_buffers 9 
//---- использовано 4 графических построения
#property indicator_plots   4
//+--------------------------------------------+
//| Параметры отрисовки облака                 |
//+--------------------------------------------+
//---- отрисовка индикатора в виде облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цвета линии индикатора использован цвет Lavender
#property indicator_color1 clrLavender
//---- отображение метки индикатора
#property indicator_label1  "XMA Range Channel"
//+--------------------------------------------+
//| Параметры отрисовки уровней                |
//+--------------------------------------------+
//---- отрисовка уровней в виде линий
#property indicator_type2   DRAW_LINE
#property indicator_type3   DRAW_LINE
//---- ввыбор цветов уровней
#property indicator_color2  clrMediumSeaGreen
#property indicator_color3  clrRed
//---- уровни - штрихпунктирные кривые
#property indicator_style2 STYLE_SOLID
#property indicator_style3 STYLE_SOLID
//---- толщина уровней равна 1
#property indicator_width2  1
#property indicator_width3  1
//---- отображение меток уровней
#property indicator_label2  "Up Line"
#property indicator_label3  "Down Line"
//+--------------------------------------------+
//| Параметры отрисовки свечей                 |
//+--------------------------------------------+
//---- в качестве индикатора использованы цветные свечи
#property indicator_type4   DRAW_COLOR_CANDLES
#property indicator_color4   clrMagenta,clrPurple,clrGreen,clrLime
//---- отображение метки индикатора
#property indicator_label4  "Open;High;Low;Close"
//+--------------------------------------------+
//| Описание классов усреднений                |
//+--------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+--------------------------------------------+
//---- объявление переменных классов CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+--------------------------------------------+
//| Объявление перечислений                    |
//+--------------------------------------------+
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
//+--------------------------------------------+
//| Объявление перечислений                    |
//+--------------------------------------------+
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
//+--------------------------------------------+
//| Входные параметры индикатора               |
//+--------------------------------------------+ 
input Smooth_Method XMA_Method=MODE_SMA; // Метод усреднения
input int XLength=7;  // Глубина сглаживания
input int XPhase=100; // Параметр усреднения
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- для VIDIA это период CMO, для AMA это период медленной скользящей 
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // Сдвиг индикатора по вертикали в пунктах
//+--------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ExtOpenBuffer[],ExtHighBuffer[],ExtLowBuffer[],ExtCloseBuffer[],ExtColorBuffer[];
double UpIndBuffer[],DnIndBuffer[],UpLineBuffer[],DnLineBuffer[];
//---- объявление переменной значения вертикального сдвига средней
double dPriceShift;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+   
//| XMA_Range_Channel indicator initialization function              | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=GetStartBars(XMA_Method,XLength,XPhase);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("XLength",XLength);
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);
//---- инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(0,UpIndBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,DnIndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(2,UpLineBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,DnLineBuffer,INDICATOR_DATA);
//---- установка позиции, с которой начинается линий канала
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- осуществление сдвига индикатора по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(4,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(5,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(6,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(7,ExtCloseBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(8,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- установка позиции, с которой начинается линий канала
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- осуществление сдвига индикатора по горизонтали
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"XMA_Range_Channel",XLength,", ",XPhase,", ",Smooth,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| XMA_Range_Channel iteration function                             | 
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
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=0; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      UpLineBuffer[bar]=UpIndBuffer[bar]=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,high[bar],bar,false)+dPriceShift;
      DnLineBuffer[bar]=DnIndBuffer[bar]=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,low[bar],bar,false)+dPriceShift;
     }
//---- основной цикл исправления и окрашивания свечей
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      ExtOpenBuffer[bar]=ExtCloseBuffer[bar]=ExtHighBuffer[bar]=ExtLowBuffer[bar]=ExtColorBuffer[bar]=EMPTY_VALUE;
      //----
      if(close[bar]>UpLineBuffer[bar])
        {
         ExtOpenBuffer[bar]=open[bar];
         ExtCloseBuffer[bar]=close[bar];
         ExtHighBuffer[bar]=high[bar];
         ExtLowBuffer[bar]=low[bar];
         if(close[bar]>=open[bar]) ExtColorBuffer[bar]=3;
         else ExtColorBuffer[bar]=2;
        }
      //----
      if(close[bar]<DnLineBuffer[bar])
        {
         ExtOpenBuffer[bar]=open[bar];
         ExtCloseBuffer[bar]=close[bar];
         ExtHighBuffer[bar]=high[bar];
         ExtLowBuffer[bar]=low[bar];
         if(close[bar]<=open[bar]) ExtColorBuffer[bar]=0;
         else ExtColorBuffer[bar]=1;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
