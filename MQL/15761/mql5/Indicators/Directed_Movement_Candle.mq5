//+------------------------------------------------------------------+
//|                                     Directed_Movement_Candle.mq5 |
//|                               Copyright © 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Directed_MovementCandle"
//---- номер версии индикатора
#property version   "1.00"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано пять буферов
#property indicator_buffers 5
//---- использовано всего одно графическое построение
#property indicator_plots   1
//---- в качестве индикатора использованы цветные свечи
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrDeepPink,clrGray,clrDodgerBlue
//---- отображение метки индикатора
#property indicator_label1  "Directed_Movement_Candle Open;High;Low;Close"
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
/*enum Smooth_Method - объявлено в файле SmoothAlgorithms.mqh
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
   MODE_AMA    //AMA
  }; */
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
enum Smooth
  {
   MODE_FIRST=0, //первое
   MODE_SECOND   //второе
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth Mode=MODE_SECOND;                   // усреднение RSI
input uint                 RSIPeriod=14;         // период индикатора
input Smooth_Method MA_Method1=MODE_SMA_;        // метод усреднения первого сглаживания 
input int Length1=12;                            // глубина  первого сглаживания                    
input int Phase1=15;                             // параметр первого сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method2=MODE_JJMA;        // метод усреднения второго сглаживания 
input int Length2 = 5;                           // глубина  второго сглаживания 
input int Phase2=15;                             // параметр второго сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input int Shift=0;                               // сдвиг индикатора по горизонтали в барах
input int HighLevel=70;                          // верхний уровень срабатывания
input int MiddleLevel=50;                        // середина диапазона
input int LowLevel=30;                           // нижний уровень срабатывания
//+----------------------------------------------+

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];

//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//---- Объявление целых переменных для хендлов индикаторов
int Ind_Handle[4];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//----
   min_rates_total=int(RSIPeriod);
   min_rates_total+=GetStartBars(MA_Method1,Length1,Phase1);
   min_rates_total+=GetStartBars(MA_Method2,Length2,Phase2);
   min_rates_total+=2;

//---- получение хендла индикатора Directed_Movement
   Ind_Handle[0]=iCustom(NULL,0,"Directed_Movement",RSIPeriod,PRICE_OPEN,MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,0);
   if(Ind_Handle[0]==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Directed_Movement["+string(0)+"]!");
      return(INIT_FAILED);
     }

   Ind_Handle[1]=iCustom(NULL,0,"Directed_Movement",RSIPeriod,PRICE_HIGH,MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,0);
   if(Ind_Handle[1]==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Directed_Movement["+string(1)+"]!");
      return(INIT_FAILED);
     }

   Ind_Handle[2]=iCustom(NULL,0,"Directed_Movement",RSIPeriod,PRICE_LOW,MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,0);
   if(Ind_Handle[2]==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Directed_Movement["+string(2)+"]!");
      return(INIT_FAILED);
     }

   Ind_Handle[3]=iCustom(NULL,0,"Directed_Movement",RSIPeriod,PRICE_CLOSE,MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,0);
   if(Ind_Handle[3]==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Directed_Movement["+string(3)+"]!");
      return(INIT_FAILED);
     }

//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);

//---- индексация элементов в буферах как в таймсериях
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);
   ArraySetAsSeries(ExtColorBuffer,true);

//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);

//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- имя для окон данных и метка для субъокон 
   string short_name="Directed_Movement_Candl";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---- количество  горизонтальных уровней индикатора 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,70);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,50);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,30);
//---- в качестве цветов линий горизонтальных уровней использованы цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrMagenta);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASH);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
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
//---- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(Ind_Handle[0])<rates_total
      || BarsCalculated(Ind_Handle[1])<rates_total
      || BarsCalculated(Ind_Handle[2])<rates_total
      || BarsCalculated(Ind_Handle[3])<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- объявления локальных переменных 
   int to_copy,limit,bar;

//---- расчёты необходимого количества копируемых данных и стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-1; // стартовый номер для расчёта всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
     }

   to_copy=limit+1;

//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(Ind_Handle[0],Mode,0,to_copy,ExtOpenBuffer)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle[1],Mode,0,to_copy,ExtHighBuffer)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle[2],Mode,0,to_copy,ExtLowBuffer)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle[3],Mode,0,to_copy,ExtCloseBuffer)<=0) return(RESET);


//---- Основной цикл исправления и окрашивания свечей
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double Max=MathMin(MathMax(ExtOpenBuffer[bar],ExtCloseBuffer[bar]),100);
      double Min=MathMax(MathMin(ExtOpenBuffer[bar],ExtCloseBuffer[bar]),0);

      ExtHighBuffer[bar]=MathMin(MathMax(Max,ExtHighBuffer[bar]),100);
      ExtLowBuffer[bar]=MathMax(MathMin(Min,ExtLowBuffer[bar]),0);

      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=2.0;
      else if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else ExtColorBuffer[bar]=1.0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
