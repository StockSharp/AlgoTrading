//+---------------------------------------------------------------------+
//|                                                      NRatioSign.mq5 | 
//|                                              Copyright © 2006, Rosh | 
//|                                     http://konkop.narod.ru/nrma.htm | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2006, Rosh"
#property link "http://konkop.narod.ru/nrma.htm"
//--- номер версии индикатора
#property version   "1.01"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован DeepPink цвет
#property indicator_color1  clrDeepPink
//--- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//--- отображение бычей метки индикатора
#property indicator_label1  "NRatioSign Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьго индикатора        |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычей линии индикатора использован DodgerBlue цвет
#property indicator_color2  clrDodgerBlue
//--- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//--- отображение медвежьей метки индикатора
#property indicator_label2 "NRatioSign Buy"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 80.0
#property indicator_level2 50.0
#property indicator_level3 20.0
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1;
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| объявление перечислений                      |
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
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| объявление перечислений                      |
//+----------------------------------------------+
enum Alg_Method
  {
   MODE_IN,  //Алгоритм на входе в зоны ПЗ и ПП
   MODE_OUT  //Алгоритм на выходе в зоны ПЗ и ПП
  };
//+----------------------------------------------+
//| объявление перечислений                      |
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
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_SMA; // Метод усреднения
input int XLength=3;                     // Глубина сглаживания                    
input int XPhase=15;                     // Параметр сглаживания
//--- XPhase: для JJMA изменяется в пределах -100 ... +100, влияет на качество переходного процесса;
//--- XPhase: для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE;    // Ценовая константа
input double Kf=1;
input double Fast=2;
input double Sharp=2;
input Alg_Method Mode=MODE_OUT;          // Пробойный алгоритм
input uint NRatio_UpLevel=80;            // Уровень перекупленности
input uint NRatio_DnLevel=20;            // Уровень перепроданности
input int    Shift=0;                    // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//--- объявление переменной значения вертикального сдвига скользящей средней
double dF;
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
int ATR_Handle;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- Инициализация переменных начала отсчета данных
   min_rates_total=XMA1.GetStartBars(XMA_Method,XLength,XPhase)+1;
   int ATR_Period=10;
   min_rates_total=int(MathMax(min_rates_total+1,ATR_Period));
//--- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("XLength",XLength);
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);
//--- Инициализация констант
   dF=2.0/(1.0+Fast);

//--- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }

   if(NRatio_UpLevel<=NRatio_DnLevel)
     {
      Print("Уровень перекупленности всегда должен быть больше уровня перепроданности!!!");
      return(INIT_FAILED);
     }

//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,175);
//--- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);

//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,175);
//--- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);

//--- инициализации переменной для короткого имени индикатора
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"NRatioSign(",XLength,", ",Smooth1,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| Custom indicator iteration function                              | 
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
//--- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);

//--- Объявление переменных с плавающей точкой  
   double price,NRTR0,LPrice,HPrice,Oscil,xOscil,ATR[1],NRatio0;
   static double NRTR1,NRatio1;
//--- Объявление целых переменных и получение уже посчитанных баров
   int first,bar,Trend0;
   static int Trend1;

//--- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=1; // стартовый номер для расчета всех баров
      bar=first-1;
      price=PriceSeries(IPC,bar,open,low,high,close);
      NRatio1=50;
      if(close[first]>open[first])
        {
         Trend1=+1;
         NRTR1=NormalizeDouble(price*(1.0-Kf*0.01),_Digits);
        }
      else
        {
         Trend1=-1;
         NRTR1=NormalizeDouble(price*(1.0+Kf*0.01),_Digits);
        }
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров

//--- Основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //--- Вызов функции PriceSeries для получения входной цены price
      price=PriceSeries(IPC,bar,open,low,high,close);
      NRTR0=NRTR1;
      Trend0=Trend1;

      if(Trend1>=0)
        {
         if(price<NRTR1)
           {
            Trend0=-1;
            NRTR0=NormalizeDouble(price*(1.0+Kf*0.01),_Digits);
           }
         else
           {
            Trend0=+1;
            LPrice=NormalizeDouble(price*(1.0-Kf*0.01),_Digits);
            if(LPrice>NRTR1) NRTR0=LPrice;
            else NRTR0=NRTR1;
           }
        }

      if(Trend1<=0)
        {
         if(price>NRTR1)
           {
            Trend0=+1;
            NRTR0=NormalizeDouble(price*(1.0-Kf*0.01),_Digits);
           }
         else
           {
            Trend0=-1;
            HPrice=NormalizeDouble(price*(1.0+Kf*0.01),_Digits);
            if(HPrice<NRTR1) NRTR0=HPrice;
            else NRTR0=NRTR1;
           }
        }

      Oscil=(100.0*MathAbs(price-NRTR0)/price)/Kf;
      xOscil=XMA1.XMASeries(1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,Oscil,bar,false);
      NRatio0=100*MathPow(xOscil,Sharp);

      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      
      if(Mode==MODE_IN)
        {
         if(NRatio0>NRatio_UpLevel && NRatio1<=NRatio_UpLevel)
           {
            //--- копируем вновь появившиеся данные в массив
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
           }
         if(NRatio0<NRatio_DnLevel && NRatio1>=NRatio_DnLevel)
           {
            //--- копируем вновь появившиеся данные в массив
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            SellBuffer[bar]=high[bar]+ATR[0]*3/8;
           }
        }
      else
        {
         if(NRatio0<NRatio_UpLevel && NRatio1>=NRatio_UpLevel)
           {
            //--- копируем вновь появившиеся данные в массив
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            SellBuffer[bar]=high[bar]+ATR[0]*3/8;
           }
         if(NRatio0>NRatio_DnLevel && NRatio1<=NRatio_DnLevel)
           {
            //--- копируем вновь появившиеся данные в массив
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
           }
        }

      if(bar<rates_total-1)
        {
         Trend1=Trend0;
         NRTR1=NRTR0;
         NRatio1=NRatio0;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
