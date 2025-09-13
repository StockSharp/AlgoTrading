//+---------------------------------------------------------------------+ 
//|                                                    ColorRsiMACD.mq5 | 
//|                                           Copyright © 2016, Maury74 | 
//|                                         molinari.maurizio@gmail.com | 
//+---------------------------------------------------------------------+
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2016, Maury74"
#property link "molinari.maurizio@gmail.com" 
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 4
#property indicator_buffers 4 
//---- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в виде четырёхцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов четырёхцветной гистограммы использованы
#property indicator_color1 clrGray,clrTeal,clrBlueViolet,clrIndianRed,clrMagenta
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "ColorRsiMACD"

//---- отрисовка индикатора в виде трёхцветной линии
#property indicator_type2 DRAW_COLOR_LINE
//---- в качестве цветов трёхцветной линии использованы
#property indicator_color2 clrGray,clrDodgerBlue,clrMagenta
//---- линия индикатора - штрихпунктирная кривая
#property indicator_style2 STYLE_SOLID
//---- толщина линии индикатора равна 3
#property indicator_width2 3
//---- отображение метки сигнальной линии
#property indicator_label2  "Signal Line"
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET 0                // Константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//|  Описание классов усреднений                 |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3;
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
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
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
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
//+----------------------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА                |
//+----------------------------------------------+
input uint    RSIPeriod=14;
input ENUM_APPLIED_PRICE   RSIPrice=PRICE_CLOSE;
input Smooth_Method XMA_Method=MODE_T3; //метод усреднения гистограммы
input uint Fast_XMA = 12; //период быстрого мувинга
input uint Slow_XMA = 26; //период медленного мувинга
input int XPhase=100;  //параметр усреднения мувингов,
                       //для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
// Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method Signal_Method=MODE_JJMA; //метод усреднения сигнальной линии
input int Signal_XMA=9; //период сигнальной линии 
input int Signal_Phase=100; // параметр сигнальной линии,
                            //изменяющийся в пределах -100 ... +100,
//влияет на качество переходного процесса;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//ценовая константа
//+----------------------------------------------+
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double XMACDBuffer[],SignBuffer[],ColorXMACDBuffer[],ColorSignBuffer[];
//--- объявление целочисленных переменных для хендлов индикаторов
int Ind_Handle;
//+------------------------------------------------------------------+    
//| XMACD indicator initialization function                          | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_1=int(RSIPeriod);
   min_rates_2=min_rates_1+GetStartBars(XMA_Method,Fast_XMA,XPhase);
   min_rates_3=min_rates_2+GetStartBars(XMA_Method,Slow_XMA,XPhase);
   min_rates_total=min_rates_3+GetStartBars(Signal_Method,Signal_XMA,Signal_Phase)+2;

//--- получение хендла индикатора iRSI
   Ind_Handle=iRSI(Symbol(),NULL,RSIPeriod,RSIPrice);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iRSI");
      return(INIT_FAILED);
     }

//---- превращение динамического массива XMACDBuffer в индикаторный буфер
   SetIndexBuffer(0,XMACDBuffer,INDICATOR_DATA);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorXMACDBuffer,INDICATOR_COLOR_INDEX);

//---- превращение динамического массива SignBuffer в индикаторный буфер
   SetIndexBuffer(2,SignBuffer,INDICATOR_DATA);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(3,ColorSignBuffer,INDICATOR_COLOR_INDEX);

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
   string shortname="ColorRsiMACD";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
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
   if(rates_total<min_rates_total) return(RESET);
   if(BarsCalculated(Ind_Handle)<rates_total) return(prev_calculated);

//---- Объявление целых переменных
   int first1,first2,first3,bar;
//---- Объявление переменных с плавающей точкой  
   double RSI[1],fast_rsi,slow_rsi,rsi_macd,sign_rsi;

//---- Инициализация индикатора в блоке OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      first1=min_rates_1; // стартовый номер для расчёта всех баров первого цикла
      first2=min_rates_1+1; // стартовый номер для расчёта всех баров второго цикла
      first3=min_rates_total+1; // стартовый номер для расчёта всех баров третьего цикла
     }
   else // стартовый номер для расчёта новых баров
     {
      first1=prev_calculated-1;
      first2=first1;
      first3=first1;
     }

//---- Основной цикл расчёта индикатора
   for(bar=first1; bar<rates_total; bar++)
     {
      if(CopyBuffer(Ind_Handle,0,rates_total-1-bar,1,RSI)<=0) return(RESET);
      fast_rsi=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,RSI[0],bar,false);
      slow_rsi=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,fast_rsi,bar,false);
      rsi_macd=fast_rsi-slow_rsi;
      sign_rsi=XMA3.XMASeries(min_rates_3,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,rsi_macd,bar,false);
      //---- Загрузка полученных значений в индикаторные буферы      
      XMACDBuffer[bar]=rsi_macd;
      SignBuffer[bar]=sign_rsi;
     }

//---- Основной цикл раскраски индикатора XMACD
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorXMACDBuffer[bar]=0;

      if(XMACDBuffer[bar]>0)
        {
         if(XMACDBuffer[bar]>XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=1;
         if(XMACDBuffer[bar]<XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=2;
        }

      if(XMACDBuffer[bar]<0)
        {
         if(XMACDBuffer[bar]<XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=3;
         if(XMACDBuffer[bar]>XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=4;
        }
     }

//---- Основной цикл раскраски сигнальной линии
   for(bar=first3; bar<rates_total; bar++)
     {
      ColorSignBuffer[bar]=0;
      if(XMACDBuffer[bar]>SignBuffer[bar-1]) ColorSignBuffer[bar]=1;
      if(XMACDBuffer[bar]<SignBuffer[bar-1]) ColorSignBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
