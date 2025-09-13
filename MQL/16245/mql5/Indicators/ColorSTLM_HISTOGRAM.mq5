//+---------------------------------------------------------------------+ 
//|                                             ColorSTLM_HISTOGRAM.mq5 | 
//|                                  Copyright © 2016, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2016, Nikolay Kositsin"
//---- ссылка на сайт автора
#property link      "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов
#property indicator_buffers 3 
//---- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Параметры отрисовки индикатора STLM         |
//+----------------------------------------------+
//---- отрисовка индикатора STLM в виде цветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов пятицветной гистограммы использованы
#property indicator_color1 clrRed,clrMagenta,clrGray,clrTeal,clrSpringGreen
//---- линия индикатора-непрерывная кривая
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "STLM HISTOGRAM"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора STLM         |
//+----------------------------------------------+
//---- отрисовка индикатора FTLM в виде линии
#property indicator_type2 DRAW_LINE
//---- в качестве цвета сигнальной линии использован серый цвет
#property indicator_color2 clrGray
//---- линия индикатора-штрихпунктирная кривая
#property indicator_style2 STYLE_DASHDOTDOT
//---- толщина линии индикатора равна 1
#property indicator_width2 2
//---- отображение метки сигнальной линии
#property indicator_label2  "STLM"

//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrBlueViolet
#property indicator_levelstyle STYLE_SOLID
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
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
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА                |
//+----------------------------------------------+
input int JLength=3; // глубина сглаживания                   
input int JPhase=100; // параметр сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_WEIGHTED_;  // ценовая константа
input int STLMShift=0; // сдвиг STLM по горизонтали в барах
//+----------------------------------------------+

//---- объявление и инициализация переменной для хранения количества расчётных баров
int STLMPeriod=91;

//---- объявление динамического массивoв, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[],IndBuffer_[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=STLMPeriod+1+30;

//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
   SetIndexBuffer(2,IndBuffer_,INDICATOR_DATA);

//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,STLMPeriod);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,STLMPeriod);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- инициализации переменной для короткого имени индикатора
   string shortname="ColorSTLM_HISTOGRAM";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//----   
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- объявления локальных переменных 
   int first,bar;
   double STLM,JSTLM,value1,value2,clr;
   static int minbar;
//---- объявление переменной класса JJMA из файла JJMASeries_Cls.mqh
   static CJJMA JMA;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=STLMPeriod-1;  // стартовый номер для расчёта всех баров
      minbar=first;
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- основной цикл расчёта индикатора STLM
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      value1=
             +0.0982862174*PriceSeries(IPC,bar-0,open,low,high,close)
             +0.0975682269*PriceSeries(IPC,bar-1,open,low,high,close)
             +0.0961401078*PriceSeries(IPC,bar-2,open,low,high,close)
             +0.0940230544*PriceSeries(IPC,bar-3,open,low,high,close)
             +0.0912437090*PriceSeries(IPC,bar-4,open,low,high,close)
             +0.0878391006*PriceSeries(IPC,bar-5,open,low,high,close)
             +0.0838544303*PriceSeries(IPC,bar-6,open,low,high,close)
             +0.0793406350*PriceSeries(IPC,bar-7,open,low,high,close)
             +0.0743569346*PriceSeries(IPC,bar-8,open,low,high,close)
             +0.0689666682*PriceSeries(IPC,bar-9,open,low,high,close)
             +0.0632381578*PriceSeries(IPC,bar-10,open,low,high,close)
             +0.0572428925*PriceSeries(IPC,bar-11,open,low,high,close)
             +0.0510534242*PriceSeries(IPC,bar-12,open,low,high,close)
             +0.0447468229*PriceSeries(IPC,bar-13,open,low,high,close)
             +0.0383959950*PriceSeries(IPC,bar-14,open,low,high,close)
             +0.0320735368*PriceSeries(IPC,bar-15,open,low,high,close)
             +0.0258537721*PriceSeries(IPC,bar-16,open,low,high,close)
             +0.0198005183*PriceSeries(IPC,bar-17,open,low,high,close)
             +0.0139807863*PriceSeries(IPC,bar-18,open,low,high,close)
             +0.0084512448*PriceSeries(IPC,bar-19,open,low,high,close)
             +0.0032639979*PriceSeries(IPC,bar-20,open,low,high,close)
             -0.0015350359*PriceSeries(IPC,bar-21,open,low,high,close)
             -0.0059060082*PriceSeries(IPC,bar-22,open,low,high,close)
             -0.0098190256*PriceSeries(IPC,bar-23,open,low,high,close)
             -0.0132507215*PriceSeries(IPC,bar-24,open,low,high,close)
             -0.0161875265*PriceSeries(IPC,bar-25,open,low,high,close)
             -0.0186164872*PriceSeries(IPC,bar-26,open,low,high,close)
             -0.0205446727*PriceSeries(IPC,bar-27,open,low,high,close)
             -0.0219739146*PriceSeries(IPC,bar-28,open,low,high,close)
             -0.0229204861*PriceSeries(IPC,bar-29,open,low,high,close)
             -0.0234080863*PriceSeries(IPC,bar-30,open,low,high,close)
             -0.0234566315*PriceSeries(IPC,bar-31,open,low,high,close)
             -0.0231017777*PriceSeries(IPC,bar-32,open,low,high,close)
             -0.0223796900*PriceSeries(IPC,bar-33,open,low,high,close)
             -0.0213300463*PriceSeries(IPC,bar-34,open,low,high,close)
             -0.0199924534*PriceSeries(IPC,bar-35,open,low,high,close)
             -0.0184126992*PriceSeries(IPC,bar-36,open,low,high,close)
             -0.0166377699*PriceSeries(IPC,bar-37,open,low,high,close)
             -0.0147139428*PriceSeries(IPC,bar-38,open,low,high,close)
             -0.0126796776*PriceSeries(IPC,bar-39,open,low,high,close)
             -0.0105938331*PriceSeries(IPC,bar-40,open,low,high,close)
             -0.0084736770*PriceSeries(IPC,bar-41,open,low,high,close)
             -0.0063841850*PriceSeries(IPC,bar-42,open,low,high,close)
             -0.0043466731*PriceSeries(IPC,bar-43,open,low,high,close)
             -0.0023956944*PriceSeries(IPC,bar-44,open,low,high,close)
             -0.0005535180*PriceSeries(IPC,bar-45,open,low,high,close)
             +0.0011421469*PriceSeries(IPC,bar-46,open,low,high,close)
             +0.0026845693*PriceSeries(IPC,bar-47,open,low,high,close)
             +0.0040471369*PriceSeries(IPC,bar-48,open,low,high,close)
             +0.0052380201*PriceSeries(IPC,bar-49,open,low,high,close)
             +0.0062194591*PriceSeries(IPC,bar-50,open,low,high,close)
             +0.0070340085*PriceSeries(IPC,bar-51,open,low,high,close)
             +0.0076266453*PriceSeries(IPC,bar-52,open,low,high,close)
             +0.0080376628*PriceSeries(IPC,bar-53,open,low,high,close)
             +0.0083037666*PriceSeries(IPC,bar-54,open,low,high,close)
             +0.0083694798*PriceSeries(IPC,bar-55,open,low,high,close)
             +0.0082901022*PriceSeries(IPC,bar-56,open,low,high,close)
             +0.0080741359*PriceSeries(IPC,bar-57,open,low,high,close)
             +0.0077543820*PriceSeries(IPC,bar-58,open,low,high,close)
             +0.0073260526*PriceSeries(IPC,bar-59,open,low,high,close)
             +0.0068163569*PriceSeries(IPC,bar-60,open,low,high,close)
             +0.0062325477*PriceSeries(IPC,bar-61,open,low,high,close)
             +0.0056078229*PriceSeries(IPC,bar-62,open,low,high,close)
             +0.0049516078*PriceSeries(IPC,bar-63,open,low,high,close)
             +0.0161380976*PriceSeries(IPC,bar-64,open,low,high,close);
      //----
      value2=
             -0.0074151919*PriceSeries(IPC,bar-0,open,low,high,close)
             -0.0060698985*PriceSeries(IPC,bar-1,open,low,high,close)
             -0.0044979052*PriceSeries(IPC,bar-2,open,low,high,close)
             -0.0027054278*PriceSeries(IPC,bar-3,open,low,high,close)
             -0.0007031702*PriceSeries(IPC,bar-4,open,low,high,close)
             +0.0014951741*PriceSeries(IPC,bar-5,open,low,high,close)
             +0.0038713513*PriceSeries(IPC,bar-6,open,low,high,close)
             +0.0064043271*PriceSeries(IPC,bar-7,open,low,high,close)
             +0.0090702334*PriceSeries(IPC,bar-8,open,low,high,close)
             +0.0118431116*PriceSeries(IPC,bar-9,open,low,high,close)
             +0.0146922652*PriceSeries(IPC,bar-10,open,low,high,close)
             +0.0175884606*PriceSeries(IPC,bar-11,open,low,high,close)
             +0.0204976517*PriceSeries(IPC,bar-12,open,low,high,close)
             +0.0233865835*PriceSeries(IPC,bar-13,open,low,high,close)
             +0.0262218588*PriceSeries(IPC,bar-14,open,low,high,close)
             +0.0289681736*PriceSeries(IPC,bar-15,open,low,high,close)
             +0.0315922931*PriceSeries(IPC,bar-16,open,low,high,close)
             +0.0340614696*PriceSeries(IPC,bar-17,open,low,high,close)
             +0.0363444061*PriceSeries(IPC,bar-18,open,low,high,close)
             +0.0384120882*PriceSeries(IPC,bar-19,open,low,high,close)
             +0.0402373884*PriceSeries(IPC,bar-20,open,low,high,close)
             +0.0417969735*PriceSeries(IPC,bar-21,open,low,high,close)
             +0.0430701377*PriceSeries(IPC,bar-22,open,low,high,close)
             +0.0440399188*PriceSeries(IPC,bar-23,open,low,high,close)
             +0.0446941124*PriceSeries(IPC,bar-24,open,low,high,close)
             +0.0450230100*PriceSeries(IPC,bar-25,open,low,high,close)
             +0.0450230100*PriceSeries(IPC,bar-26,open,low,high,close)
             +0.0446941124*PriceSeries(IPC,bar-27,open,low,high,close)
             +0.0440399188*PriceSeries(IPC,bar-28,open,low,high,close)
             +0.0430701377*PriceSeries(IPC,bar-29,open,low,high,close)
             +0.0417969735*PriceSeries(IPC,bar-30,open,low,high,close)
             +0.0402373884*PriceSeries(IPC,bar-31,open,low,high,close)
             +0.0384120882*PriceSeries(IPC,bar-32,open,low,high,close)
             +0.0363444061*PriceSeries(IPC,bar-33,open,low,high,close)
             +0.0340614696*PriceSeries(IPC,bar-34,open,low,high,close)
             +0.0315922931*PriceSeries(IPC,bar-35,open,low,high,close)
             +0.0289681736*PriceSeries(IPC,bar-36,open,low,high,close)
             +0.0262218588*PriceSeries(IPC,bar-37,open,low,high,close)
             +0.0233865835*PriceSeries(IPC,bar-38,open,low,high,close)
             +0.0204976517*PriceSeries(IPC,bar-39,open,low,high,close)
             +0.0175884606*PriceSeries(IPC,bar-40,open,low,high,close)
             +0.0146922652*PriceSeries(IPC,bar-41,open,low,high,close)
             +0.0118431116*PriceSeries(IPC,bar-42,open,low,high,close)
             +0.0090702334*PriceSeries(IPC,bar-43,open,low,high,close)
             +0.0064043271*PriceSeries(IPC,bar-44,open,low,high,close)
             +0.0038713513*PriceSeries(IPC,bar-45,open,low,high,close)
             +0.0014951741*PriceSeries(IPC,bar-46,open,low,high,close)
             -0.0007031702*PriceSeries(IPC,bar-47,open,low,high,close)
             -0.0027054278*PriceSeries(IPC,bar-48,open,low,high,close)
             -0.0044979052*PriceSeries(IPC,bar-49,open,low,high,close)
             -0.0060698985*PriceSeries(IPC,bar-50,open,low,high,close)
             -0.0074151919*PriceSeries(IPC,bar-51,open,low,high,close)
             -0.0085278517*PriceSeries(IPC,bar-52,open,low,high,close)
             -0.0094111161*PriceSeries(IPC,bar-53,open,low,high,close)
             -0.0100658241*PriceSeries(IPC,bar-54,open,low,high,close)
             -0.0104994302*PriceSeries(IPC,bar-55,open,low,high,close)
             -0.0107227904*PriceSeries(IPC,bar-56,open,low,high,close)
             -0.0107450280*PriceSeries(IPC,bar-57,open,low,high,close)
             -0.0105824763*PriceSeries(IPC,bar-58,open,low,high,close)
             -0.0102517019*PriceSeries(IPC,bar-59,open,low,high,close)
             -0.0097708805*PriceSeries(IPC,bar-60,open,low,high,close)
             -0.0091581551*PriceSeries(IPC,bar-61,open,low,high,close)
             -0.0084345004*PriceSeries(IPC,bar-62,open,low,high,close)
             -0.0076214397*PriceSeries(IPC,bar-63,open,low,high,close)
             -0.0067401718*PriceSeries(IPC,bar-64,open,low,high,close)
             -0.0058083144*PriceSeries(IPC,bar-65,open,low,high,close)
             -0.0048528295*PriceSeries(IPC,bar-66,open,low,high,close)
             -0.0038816271*PriceSeries(IPC,bar-67,open,low,high,close)
             -0.0029244713*PriceSeries(IPC,bar-68,open,low,high,close)
             -0.0019911267*PriceSeries(IPC,bar-69,open,low,high,close)
             -0.0010974211*PriceSeries(IPC,bar-70,open,low,high,close)
             -0.0002535559*PriceSeries(IPC,bar-71,open,low,high,close)
             +0.0005231953*PriceSeries(IPC,bar-72,open,low,high,close)
             +0.0012297491*PriceSeries(IPC,bar-73,open,low,high,close)
             +0.0018539149*PriceSeries(IPC,bar-74,open,low,high,close)
             +0.0023994354*PriceSeries(IPC,bar-75,open,low,high,close)
             +0.0028490136*PriceSeries(IPC,bar-76,open,low,high,close)
             +0.0032221429*PriceSeries(IPC,bar-77,open,low,high,close)
             +0.0034936183*PriceSeries(IPC,bar-78,open,low,high,close)
             +0.0036818974*PriceSeries(IPC,bar-79,open,low,high,close)
             +0.0038037944*PriceSeries(IPC,bar-80,open,low,high,close)
             +0.0038338964*PriceSeries(IPC,bar-81,open,low,high,close)
             +0.0037975350*PriceSeries(IPC,bar-82,open,low,high,close)
             +0.0036986051*PriceSeries(IPC,bar-83,open,low,high,close)
             +0.0035521320*PriceSeries(IPC,bar-84,open,low,high,close)
             +0.0033559226*PriceSeries(IPC,bar-85,open,low,high,close)
             +0.0031224409*PriceSeries(IPC,bar-86,open,low,high,close)
             +0.0028550092*PriceSeries(IPC,bar-87,open,low,high,close)
             +0.0025688349*PriceSeries(IPC,bar-88,open,low,high,close)
             +0.0022682355*PriceSeries(IPC,bar-89,open,low,high,close)
             +0.0073925495*PriceSeries(IPC,bar-90,open,low,high,close);

      STLM=value1-value2;
      JSTLM=JMA.JJMASeries(minbar,prev_calculated,rates_total,0,JPhase,JLength,STLM,bar,false);
      //---- Инициализация ячейки индикаторного буфера полученным значением FTLM
      IndBuffer[bar]=JSTLM;
      IndBuffer_[bar]=JSTLM;
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first++;

//---- Основной цикл раскраски
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      clr=2;

      if(IndBuffer[bar]>0) if(IndBuffer[bar-1]>IndBuffer[bar]) clr=3; else clr=4;
      if(IndBuffer[bar]<0) if(IndBuffer[bar-1]<IndBuffer[bar]) clr=1; else clr=0;

      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
