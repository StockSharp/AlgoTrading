//+---------------------------------------------------------------------+
//|                                             Digital_CCI_Woodies.mq5 | 
//|                                           Copyright © 2015, Ramdass | 
//|                                                                     | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2015, Ramdass"
#property link ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветов индикатора использованы
#property indicator_color1  clrHotPink,clrMediumAquamarine
//---- отображение метки индикатора
#property indicator_label1  "Digital_CCI_Woodies"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4;
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
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
input Smooth_Method MA_Method1=MODE_SMA; // Метод усреднения первого сглаживания
input int Length1=14; // Глубина  первого сглаживания
input int Phase1=15;  // Параметр первого сглаживания
//--- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//--- для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method2=MODE_SMA; // Метод усреднения второго сглаживания
input int Length2=6; // Глубина  второго сглаживания
input int Phase2=15; // Параметр второго сглаживания
                     //--- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//--- для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE; // Ценовая константа
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ExtABuffer[],ExtBBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_,min_rates_1,min_rates_2;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_=26;
   min_rates_1=min_rates_+GetStartBars(MA_Method1,Length1,Phase1);
   min_rates_2=min_rates_+GetStartBars(MA_Method2,Length2,Phase2);
   min_rates_total=2*(MathMax(min_rates_1,min_rates_2)-min_rates_)+min_rates_;
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Length1", Length1);
   XMA2.XMALengthCheck("Length2", Length2);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("Phase1", Phase1, MA_Method1);
   XMA2.XMAPhaseCheck("Phase2", Phase2, MA_Method2);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,ExtABuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtBBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(MA_Method1);
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   StringConcatenate(shortname,"Digital_CCI_Woodies(",Length1,", ",Length2,", ",Smooth1,", ",Smooth2,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
   double res,xres1,xres2,rel1,rel2,dev1,dev2;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=min_rates_; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      res=Get_Filter(IPC,open,low,high,close,bar);
      xres1=XMA1.XMASeries(min_rates_,prev_calculated,rates_total,MA_Method1,Phase1,Length1,res,bar,false);
      xres2=XMA2.XMASeries(min_rates_,prev_calculated,rates_total,MA_Method2,Phase2,Length2,res,bar,false);
      rel1=res-xres1;
      rel2=res-xres2;
      dev1=0.015*XMA3.XMASeries(min_rates_1,prev_calculated,rates_total,MA_Method1,Phase1,Length1,MathAbs(rel1),bar,false);
      dev2=0.015*XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,MA_Method2,Phase2,Length2,MathAbs(rel2),bar,false);
      if(dev1) ExtABuffer[bar]=rel1/dev1; else ExtABuffer[bar]=0.0;
      if(dev2) ExtBBuffer[bar]=rel2/dev2; else ExtBBuffer[bar]=0.0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| Получение значений цифрового фильтра                             |
//+------------------------------------------------------------------+   
double Get_Filter(Applied_price_ Price,const double  &Open[],const double  &Low[],const double  &High[],const double  &Close[],int index)
  {
//---- 
   double sum=0.225654509516691000*PriceSeries(Price,index-0,Open,Low,High,Close)
              +0.21924126458513900*PriceSeries(Price,index-1,Open,Low,High,Close)
              +0.20068847968989900*PriceSeries(Price,index-2,Open,Low,High,Close)
              +0.17199251376592300*PriceSeries(Price,index-3,Open,Low,High,Close)
              +0.13627040812928600*PriceSeries(Price,index-4,Open,Low,High,Close)
              +0.09716444691033020*PriceSeries(Price,index-5,Open,Low,High,Close)
              +0.05850647966034450*PriceSeries(Price,index-6,Open,Low,High,Close)
              +0.02374481402976710*PriceSeries(Price,index-7,Open,Low,High,Close)
              -0.00442869436477854*PriceSeries(Price,index-8,Open,Low,High,Close)
              -0.02436367832290450*PriceSeries(Price,index-9,Open,Low,High,Close)
              -0.03556173658348070*PriceSeries(Price,index-10,Open,Low,High,Close)
              -0.03863068174342280*PriceSeries(Price,index-11,Open,Low,High,Close)
              -0.03505645644492430*PriceSeries(Price,index-12,Open,Low,High,Close)
              -0.02689634908050020*PriceSeries(Price,index-13,Open,Low,High,Close)
              -0.01641578870330520*PriceSeries(Price,index-14,Open,Low,High,Close)
              -0.00573862260748731*PriceSeries(Price,index-15,Open,Low,High,Close)
              +0.00342620684434000*PriceSeries(Price,index-16,Open,Low,High,Close)
              +0.00996574022237624*PriceSeries(Price,index-17,Open,Low,High,Close)
              +0.01342232808872480*PriceSeries(Price,index-18,Open,Low,High,Close)
              +0.01393940042379780*PriceSeries(Price,index-19,Open,Low,High,Close)
              +0.01211499384832860*PriceSeries(Price,index-20,Open,Low,High,Close)
              +0.00883315067608292*PriceSeries(Price,index-21,Open,Low,High,Close)
              +0.00502356811075590*PriceSeries(Price,index-22,Open,Low,High,Close)
              +0.00151954406404245*PriceSeries(Price,index-23,Open,Low,High,Close)
              -0.00108567173532015*PriceSeries(Price,index-24,Open,Low,High,Close)
              -0.01333016897970480*PriceSeries(Price,index-25,Open,Low,High,Close);
//----
   return(sum);
  }
//+------------------------------------------------------------------+
