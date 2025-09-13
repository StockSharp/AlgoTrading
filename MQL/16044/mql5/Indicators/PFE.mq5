//+---------------------------------------------------------------------+
//|                                                             PFE.mq5 | 
//|                     Copyright © 2008, giampiero.raschetti@gmail.com | 
//|                                              http://www.fxtrade.it/ | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2008, giampiero.raschetti@gmail.com"
#property link "http://www.fxtrade.it/"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в виде четырёхцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов четырёхцветной гистограммы использованы
#property indicator_color1 clrTeal,clrBlueViolet,clrIndianRed,clrMagenta
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1  "PFE"
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1;
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
enum Applied_price_      //Тип константы
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
/*enum SmoothMethod - перечисление объявлено в файле SmoothAlgorithms.mqh
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
input uint PfePeriod=5;
input Smooth_Method XMA_Method=MODE_JJMA;//метод усреднения
input uint XLength=5;                    //глубина сглаживания                    
input int XPhase=100;                    //параметр сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE_;   //ценовая константа
input int Shift=0;                       //сдвиг индикатора по горизонтали в барах
input double UpLevel=+0.5;               //уровень перекупленности
input double DnLevel=-0.5;               //уровень перепроданности
//+----------------------------------------------+
//---- объявление динамического массива, который будет в 
// дальнейшем использован в качестве индикаторного буфера
double IndBuffer[],ColorIndBuffer[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total,min_rates_1;
//+------------------------------------------------------------------+   
//| PFE indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_1=int(PfePeriod)+1;
   min_rates_total=min_rates_1+GetStartBars(XMA_Method,XLength,XPhase);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой индексный буфер
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"PFE(",PfePeriod,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);  
//---- количество  горизонтальных уровней индикатора 2  
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,UpLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,DnLevel);
//---- в качестве цветов линий горизонтальных уровней использованы серый и розовый цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrRed);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| PFE iteration function                                           | 
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
   double noise,pfe;
//---- Объявление целых переменных и получение уже посчитанных баров
   int first,bar;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=min_rates_1; // стартовый номер для расчёта всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- Основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      noise=_Point;
      for(int kkk=0; kkk<int(PfePeriod); kkk++) noise+=MathAbs(PriceSeries(IPC,bar-kkk,open,low,high,close)-PriceSeries(IPC,bar-kkk-1,open,low,high,close));      
      pfe=(PriceSeries(IPC,bar,open,low,high,close)-PriceSeries(IPC,bar-PfePeriod,open,low,high,close))/noise;
      IndBuffer[bar]=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,pfe,bar,false);
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;
//---- Основной цикл раскраски индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int bar1=bar-1;
      int clr=0;

      if(IndBuffer[bar]>=0)
        {
         if(IndBuffer[bar]>IndBuffer[bar1]) clr=0;
         if(IndBuffer[bar]<IndBuffer[bar1]) clr=1;
         if(IndBuffer[bar]==IndBuffer[bar1]) clr=int(ColorIndBuffer[bar1]);
        }

      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar1]) clr=2;
         if(IndBuffer[bar]>IndBuffer[bar1]) clr=3;
         if(IndBuffer[bar]==IndBuffer[bar1]) clr=int(ColorIndBuffer[bar1]);
        }
      ColorIndBuffer[bar]=clr;
     }
//----         
   return(rates_total);
  }
//+------------------------------------------------------------------+
