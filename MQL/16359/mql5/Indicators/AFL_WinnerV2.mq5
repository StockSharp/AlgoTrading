//+---------------------------------------------------------------------+
//|                                                    AFL_WinnerV2.mq5 | 
//|                                   Copyright © 2016, Andrey Voytenko | 
//|                           https://login.mql5.com/en/users/avoitenko | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2016, Andrey Voytenko"
#property link "https://login.mql5.com/en/users/avoitenko"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов
#property indicator_buffers 3 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//--- нижнее и верхнее ограничения шкалы отдельного окна индикатора
#property indicator_maximum +60
#property indicator_minimum -60
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде многоцветной гистограммы
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- в качестве цветов индикатора использованы
#property indicator_color1  clrRed,clrViolet,clrPaleTurquoise,clrLime
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "AFL_Winner"

//+-----------------------------------+
//|  Описание класса CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//|  объявление перечислений          |
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
input uint iAverage=5; //Период для обработки входных данных
input uint iPeriod=10; //Период поиска экстремумов
input Smooth_Method iMA_Method=MODE_SMA_; //Метод усреднения первого сглаживания 
input uint iLength=5; //Глубина  сглаживания                    
input int iPhase=15; //Параметр сглаживания,
                     //для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
// Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_WEIGHTED_;  // ценовая константа
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  //объём
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input int HighLevel=+40;                          // уровень перекупленности
input int LowLevel=-40;                           // уровень перепроданности
//+-----------------------------------+

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double UpIndBuffer[],DnIndBuffer[],ColorIndBuffer[];
//---- объявление глобальных переменных
int Count1[],Count2[];
double Value[],Price[];
//---- Объявление целых переменных начала отсчета данных
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+
//|  Пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos1(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
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
//|  Пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos2(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
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
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчета данных
   min_rates_1=int(iAverage);
   min_rates_2=min_rates_1+int(iPeriod);
   min_rates_3=min_rates_2+XMA1.GetStartBars(iMA_Method,iLength,iPhase);
   min_rates_total=min_rates_3+XMA1.GetStartBars(iMA_Method,iLength,iPhase);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Length",iLength);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("Phase",iPhase,iMA_Method);

//---- распределение памяти под массивы переменных  
   ArrayResize(Count1,iPeriod);
   ArrayResize(Value,iPeriod);
   ArrayResize(Count2,iAverage);
   ArrayResize(Price,iAverage);
//----
   ArrayInitialize(Count1,0);
   ArrayInitialize(Value,0.0);
   ArrayInitialize(Count2,0);
   ArrayInitialize(Price,0.0);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,UpIndBuffer,INDICATOR_DATA);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DnIndBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(2,ColorIndBuffer,INDICATOR_DATA);

//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- инициализации переменной для короткого имени индикатора
   string shortname="AFL_Winner";
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);

//---- параметры отрисовки уровней индикатора
   IndicatorSetInteger(INDICATOR_LEVELS,5);   
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,+50);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MathMin(+50,HighLevel));
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,0.0);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,3,MathMax(-50,LowLevel));
   IndicatorSetDouble(INDICATOR_LEVELVALUE,4,-50);
//---- в качестве цветов линий горизонтальных уровней использованы 
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,3,clrRed);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,4,clrRed);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_SOLID);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,3,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,4,STYLE_DASHDOT);
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

//---- Объявление переменных с плавающей точкой  
   double rsv,scost5,max,min,x1xma,x2xma;
//---- Объявление целых переменных и получение уже посчитанных баров
   int first,bar,clr;
   long svolume5;

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=0; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров

//---- Основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- Вызов функции PriceSeries для получения входной цены
      Price[Count2[0]]=PriceSeries(IPC,bar,open,low,high,close);
      if(bar<min_rates_1-1)
        {
         if(bar<rates_total-1) Recount_ArrayZeroPos2(Count2,iAverage);
         continue;
        }
      //---- 
      scost5=0;
      svolume5=0;
      for(int kkk=0; kkk<int(iAverage); kkk++)
        {
         long res;
         if(VolumeType==VOLUME_TICK) res=long(tick_volume[bar-kkk]);
         else res=long(volume[bar-kkk]);
         scost5+=res*Price[Count2[kkk]];
         svolume5+=res;
        }
      svolume5=MathMax(svolume5,1);
      Value[Count1[0]]=scost5/svolume5;

      if(bar<min_rates_2-1)
        {
         if(bar<rates_total-1)
           {
            Recount_ArrayZeroPos1(Count1,iPeriod);
            Recount_ArrayZeroPos2(Count2,iAverage);
           }
         continue;
        }

      max=Value[ArrayMaximum(Value,0,iPeriod)];
      min=Value[ArrayMinimum(Value,0,iPeriod)];
      rsv=((Value[Count1[0]]-min)/MathMax(max-min,_Point))*100-50;
      x1xma=XMA1.XMASeries(min_rates_2,prev_calculated,rates_total,iMA_Method,iPhase,iLength,rsv,bar,false);
      x2xma=XMA2.XMASeries(min_rates_3,prev_calculated,rates_total,iMA_Method,iPhase,iLength,x1xma,bar,false);
      //----       
      if(x1xma>x2xma)
        {
         UpIndBuffer[bar]=x1xma;
         DnIndBuffer[bar]=x2xma;
         if((x1xma>HighLevel) || (x1xma>LowLevel && x2xma<=LowLevel)) clr=3;
         else clr=2;
        }
      else
        {
         UpIndBuffer[bar]=x2xma;
         DnIndBuffer[bar]=x1xma;
         if((x1xma<LowLevel) || ((x2xma>HighLevel && x1xma<=HighLevel))) clr=0;
         else clr=1;
        }
      ColorIndBuffer[bar]=clr;

      if(bar<rates_total-1) Recount_ArrayZeroPos1(Count1,iPeriod);
      if(bar<rates_total-1) Recount_ArrayZeroPos2(Count2,iAverage);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
