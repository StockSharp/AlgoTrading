//+------------------------------------------------------------------+
//|                                                  CHOWithFlat.mq5 |
//|                                 Copyright © 2014, Powered byStep | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property description "Money Flow Index With Flat"
//---- авторство индикатора
#property copyright "Copyright © 2014, Powered byStep"
//---- авторство индикатора
#property link      ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано три буфера
#property indicator_buffers 3
//---- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора 1             |
//+----------------------------------------------+
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrDodgerBlue,clrOrange
//---- отображение метки индикатора
#property indicator_label1  "CHO Oscillator"
//+----------------------------------------------+
//| Параметры отрисовки индикатора 2             |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета медвежьей линии индикатора использован серый цвет
#property indicator_color2  clrSlateGray
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 5
#property indicator_width2  5
//---- отображение медвежьей метки индикатора
#property indicator_label2  "Flat"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Объявление перечислений                      |
//+----------------------------------------------+
enum Smooth_Method
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
  };
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint                BBPeriod=20;                 // Период для расчета Боллинджера
input double              StdDeviation=2.0;            // Девиация Боллинджера
input ENUM_APPLIED_PRICE  applied_price=PRICE_CLOSE;   // Тип цены Боллинджера
input Smooth_Method XMA_Method=MODE_SMA;               // Метод усреднения
input uint FastPeriod=3;                               // Период быстрого усреднения
input uint SlowPeriod=10;                              // Метод медленного усреднения
input int XPhase=15;                                   // Параметр сглаживания
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;      // Объем 
input uint                MAPeriod=13;                 // Период усреднения сигнальной линии
input  ENUM_MA_METHOD     MAType=MODE_SMA;             // Тип усреднения сигнальной линии
input uint                flat=100;                    // Величина флэта в пунктах
input int                 Shift=0;                     // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[];
double SignalBuffer[];
double IndBuffer1[];
//---- объявление целочисленных переменных для хендлов индикаторов
int BB_Handle,CHO_Handle,MA_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(MathMax(MathMax(BBPeriod,FastPeriod),SlowPeriod));
//---- получение хендла индикатора iBands
   BB_Handle=iBands(Symbol(),PERIOD_CURRENT,BBPeriod,0,StdDeviation,applied_price);
   if(BB_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iBands");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iCHO
   CHO_Handle=iCustom(Symbol(),PERIOD_CURRENT,"CHO",XMA_Method,FastPeriod,SlowPeriod,XPhase,VolumeType);
   if(CHO_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iCHO");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iMA
   MA_Handle=iMA(Symbol(),PERIOD_CURRENT,MAPeriod,0,MAType,CHO_Handle);
   if(MA_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMA");
      return(INIT_FAILED);
     }
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,SignalBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SignalBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,IndBuffer1,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer1,true);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//----
   IndicatorSetString(INDICATOR_SHORTNAME,"CHOWithFlat");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов цены для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- проверка количества баров на достаточность для расчета
   if(BarsCalculated(BB_Handle)<rates_total
      || BarsCalculated(CHO_Handle)<rates_total
      || BarsCalculated(MA_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);
//---- объявления локальных переменных 
   int to_copy,limit,bar;
   double MainCHO[],SignCHO[],UpBB[],MainBB[];
//---- расчеты необходимого количества копируемых данных и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(BB_Handle,UPPER_BAND,0,to_copy,UpBB)<=0) return(RESET);
   if(CopyBuffer(BB_Handle,BASE_LINE,0,to_copy,MainBB)<=0) return(RESET);
   if(CopyBuffer(CHO_Handle,MAIN_LINE,0,to_copy,MainCHO)<=0) return(RESET);
   if(CopyBuffer(MA_Handle,MAIN_LINE,0,to_copy,SignCHO)<=0) return(RESET);
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(UpBB,true);
   ArraySetAsSeries(MainBB,true);
   ArraySetAsSeries(MainCHO,true);
   ArraySetAsSeries(SignCHO,true);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double res=(UpBB[bar]-MainBB[bar])/_Point;
      if(res<flat)
        {
         if(MainCHO[bar]>SignCHO[bar])
           {
            IndBuffer[bar]=0.00000001;
            SignalBuffer[bar]=0.00000001;
            IndBuffer1[bar]=0;
           }
         //----
         if(MainCHO[bar]<SignCHO[bar])
           {
            IndBuffer[bar]=0.00000001;
            SignalBuffer[bar]=0.00000001;
            IndBuffer1[bar]=0;
           }
        }
      else
        {
         IndBuffer1[bar]=EMPTY_VALUE;
         IndBuffer[bar]=MainCHO[bar];
         SignalBuffer[bar]=SignCHO[bar];
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
