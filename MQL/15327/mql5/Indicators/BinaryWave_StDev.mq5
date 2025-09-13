//+---------------------------------------------------------------------+
//|                                                BinaryWave_StDev.mq5 | 
//|                                             Copyright © 2009, LeMan |
//|                                                    b-market@mail.ru |
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2009, LeMan"
#property link      "b-market@mail.ru"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- для расчёта и отрисовки индикатора использовано шесть буферов
#property indicator_buffers 6
//---- использовано всего пять графических построений
#property indicator_plots   5
//+----------------------------------------------+
//|  Параметры отрисовки линии индикатора        |
//+----------------------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цветов трёхцветной линии использованы
#property indicator_color1  clrOrange,clrGray,clrDodgerBlue
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "BinaryWave"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован красный цвет
#property indicator_color2  clrRed
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение медвежьей метки индикатора
#property indicator_label2  "Dn_Signal 1"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде символа
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован аквамариновый цвет
#property indicator_color3  clrAqua
//---- толщина линии индикатора 3 равна 2
#property indicator_width3  2
//---- отображение бычей метки индикатора
#property indicator_label3  "Up_Signal 1"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде символа
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован красный цвет
#property indicator_color4  clrRed
//---- толщина линии индикатора 4 равна 4
#property indicator_width4  4
//---- отображение медвежьей метки индикатора
#property indicator_label4  "Dn_Signal 2"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 5 в виде символа
#property indicator_type5   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован аквамариновый цвет
#property indicator_color5  clrAqua
//---- толщина линии индикатора 5 равна 4
#property indicator_width5  4
//---- отображение бычей метки индикатора
#property indicator_label5  "Up_Signal 2"
//+-----------------------------------------------+
//| Параметры отображения горизонтальных уровней  |
//+-----------------------------------------------+
#property indicator_level1  0
#property indicator_levelcolor clrRed
#property indicator_levelstyle STYLE_SOLID
//+-----------------------------------------------+
//|  объявление констант                          |
//+-----------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчёт индикатора
//+-----------------------------------------------+
//|  Описание класса CXMA                         |
//+-----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------------------+

//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1;
//+-----------------------------------------------+
//|  объявление перечислений                      |
//+-----------------------------------------------+
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
//+-----------------------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА                 |
//+-----------------------------------------------+
//--- Вес индикаторов. Если ноль, индикатор не участвует в расчете волны
input double WeightMA    = 1.0;
input double WeightMACD  = 1.0;
input double WeightOsMA  = 1.0;
input double WeightCCI   = 1.0;
input double WeightMOM   = 1.0;
input double WeightRSI   = 1.0;
input double WeightADX   = 1.0;
//---- Параметры скользящего среднего
input int   MAPeriod=13;
input  ENUM_MA_METHOD   MAType=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
//---- Параметры MACD
input int   FastMACD     = 12;
input int   SlowMACD     = 26;
input int   SignalMACD   = 9;
input ENUM_APPLIED_PRICE   PriceMACD=PRICE_CLOSE;
//---- Параметры OsMA
input int   FastPeriod   = 12;
input int   SlowPeriod   = 26;
input int   SignalPeriod = 9;
input ENUM_APPLIED_PRICE   OsMAPrice=PRICE_CLOSE;
//---- Параметры CCI
input int   CCIPeriod=14;
input ENUM_APPLIED_PRICE   CCIPrice=PRICE_MEDIAN;
//---- Параметры Момента
input int   MOMPeriod=14;
input ENUM_APPLIED_PRICE   MOMPrice=PRICE_CLOSE;
//---- Параметры RSI
input int   RSIPeriod=14;
input ENUM_APPLIED_PRICE   RSIPrice=PRICE_CLOSE;
//---- Параметры ADX
input int   ADXPeriod=14;
//---- Включение сглаживания волны
input Smooth_Method bMA_Method=MODE_JJMA; //метод усреднения
input int bLength=5; //глубина сглаживания                    
input int bPhase=100; //параметр сглаживания,
                      //для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
// Для VIDIA это период CMO, для AMA это период медленной скользящей
input double dK1=1.5;  //коэффициент 1 для квадратичного фильтра
input double dK2=2.5;  //коэффициент 2 для квадратичного фильтра
input uint std_period=9; //период квадратичного фильтра
input int Shift=0; //сдвиг индикатора по горизонтали в барах
//+-----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double ExtLineBuffer[],ColorExtLineBuffer[];
double BearsBuffer1[],BullsBuffer1[];
double BearsBuffer2[],BullsBuffer2[];
//----
double dWave[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total,min_rates_total_1;
//---- Объявление целых переменных для хендлов индикаторов
int MA_Handle,MACD_Handle,OsMA_Handle,CCI_Handle,MOM_Handle,RSI_Handle,ADX_Handle;
//+------------------------------------------------------------------+
//| Определяем положение цены закрытия относительно среднего         |
//+------------------------------------------------------------------+    
double MAClose(int bar,double &MaArray[],const double &Close[])
  {
//----
   if(WeightMA>0)
     {
      if(Close[bar]-MaArray[bar]>0) return(+WeightMA);
      if(Close[bar]-MaArray[bar]<0) return(-WeightMA);
      //if(Close[bar]-MaArray[bar]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Определяем наклон MACD                                           |
//+------------------------------------------------------------------+    
double MACD(int bar,double &MacdArray[])
  {
//----
   if(WeightMACD>0)
     {
      if(MacdArray[bar]-MacdArray[bar+1]>0) return(+WeightMACD);
      if(MacdArray[bar]-MacdArray[bar+1]<0) return(-WeightMACD);
      //if(MacdArray[bar]-MacdArray[bar+1]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Определяем положение OsMa относительно нуля                      |
//+------------------------------------------------------------------+    
double OsMA(int bar,double &OsMAArray[])
  {
//----
   if(WeightOsMA>0)
     {
      if(OsMAArray[bar]>0) return(+WeightOsMA);
      if(OsMAArray[bar]<0) return(-WeightOsMA);
      //if(OsMAArray[bar]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Определяем положение CCI относительно нуля                       |
//+------------------------------------------------------------------+    
double CCI(int bar,double &CCIArray[])
  {
//----
   if(WeightCCI>0)
     {
      if(CCIArray[bar]>0) return(+WeightCCI);
      if(CCIArray[bar]<0) return(-WeightCCI);
      //if(CCIArray[bar]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Определяем положение Momentum относительно 100                   |
//+------------------------------------------------------------------+    
double MOM(int bar,double &MOMArray[])
  {
//----
   if(WeightMOM>0)
     {
      if(MOMArray[bar]>100) return(+WeightMOM);
      if(MOMArray[bar]<100) return(-WeightMOM);
      //if(MOMArray[bar]==100) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Определяем положение RSI относительно 50                         |
//+------------------------------------------------------------------+    
double RSI(int bar,double &RSIArray[])
  {
//----
   if(WeightRSI>0)
     {
      if(RSIArray[bar]>50) return(+WeightRSI);
      if(RSIArray[bar]<50) return(-WeightRSI);
      //if(RSIArray[bar]==100) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Определяем положение DMI                                         |
//+------------------------------------------------------------------+    
double ADX(int bar,double &DMIPArray[],double &DMIMArray[])
  {
//----
   if(WeightADX>0)
     {
      if(DMIPArray[bar]>DMIMArray[bar]) return(+WeightADX);
      if(DMIPArray[bar]<DMIMArray[bar]) return(-WeightADX);
      //if(DMIPArray[bar]==DMIMArray[bar]) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+   
//| BinaryWave indicator initialization function                     | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total_1=MathMax(MAPeriod,MathMax(SlowPeriod,MathMax(CCIPeriod,MathMax(SlowMACD,MOMPeriod))))+1;
   min_rates_total=min_rates_total_1+XMA1.GetStartBars(bMA_Method,bLength,bPhase);
   min_rates_total+=int(std_period);
//---- Распределение памяти под массивы переменных  
   ArrayResize(dWave,std_period);
   
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("bLength", bLength);
   XMA1.XMAPhaseCheck("bPhase", bPhase, bMA_Method);

//---- получение хендла индикатора iMA
   MA_Handle=iMA(NULL,0,MAPeriod,0,MAType,MAPrice);
   if(MA_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMA");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iMACD
   MACD_Handle=iMACD(NULL,0,FastMACD,SlowMACD,SignalMACD,PriceMACD);
   if(MACD_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMACD");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iOsMA
   OsMA_Handle=iOsMA(NULL,0,FastPeriod,SlowPeriod,SignalPeriod,OsMAPrice);
   if(OsMA_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iOsMA");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iCCI
   CCI_Handle=iCCI(NULL,0,CCIPeriod,CCIPrice);
   if(CCI_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iCCI");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iMomentum
   MOM_Handle=iMomentum(NULL,0,MOMPeriod,MOMPrice);
   if(MOM_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMomentum");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iRSI
   RSI_Handle=iRSI(NULL,0,RSIPeriod,RSIPrice);
   if(RSI_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iRSI");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iADX
   ADX_Handle=iADX(NULL,0,ADXPeriod);
   if(ADX_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iADX");
      return(INIT_FAILED);
     }

//---- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtLineBuffer,true);
   
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorExtLineBuffer,true);

//---- превращение динамического массива BearsBuffer в индикаторный буфер
   SetIndexBuffer(2,BearsBuffer1,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BearsBuffer1,true);

//---- превращение динамического массива BullsBuffer в индикаторный буфер
   SetIndexBuffer(3,BullsBuffer1,INDICATOR_DATA);
//---- осуществление сдвига индикатора 3 по горизонтали
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BullsBuffer1,true);

//---- превращение динамического массива BearsBuffer в индикаторный буфер
   SetIndexBuffer(4,BearsBuffer2,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BearsBuffer2,true);

//---- превращение динамического массива BullsBuffer в индикаторный буфер
   SetIndexBuffer(5,BullsBuffer2,INDICATOR_DATA);
//---- осуществление сдвига индикатора 3 по горизонтали
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(4,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BullsBuffer2,true);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(bMA_Method);
   StringConcatenate(shortname,"BinaryWave(",bLength,", ",Smooth1,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| BinaryWave iteration function                                    | 
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
   if(BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(MACD_Handle)<rates_total
      || BarsCalculated(OsMA_Handle)<rates_total
      || BarsCalculated(CCI_Handle)<rates_total
      || BarsCalculated(MOM_Handle)<rates_total
      || BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(ADX_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- объявления локальных переменных 
   int to_copy,limit,bar,maxbar;
   double tmp,MA_[],MACD_[],OsMA_[],CCI_[],MOM_[],RSI_[],DMIP_[],DMIM_[];
   double SMAdif,Sum,StDev,dstd,BEARS1,BULLS1,BEARS2,BULLS2,Filter1,Filter2,wave;

//---- расчёты необходимого количества копируемых данных и
//стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      to_copy=rates_total; // расчётное количество всех баров
      limit=rates_total-2; // стартовый номер для расчёта всех баров
     }
   else
     {
      to_copy=rates_total-prev_calculated+1; // расчётное количество только новых баров
      limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
     }


//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA_)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle,0,0,to_copy+1,MACD_)<=0) return(RESET);
   if(CopyBuffer(OsMA_Handle,0,0,to_copy,OsMA_)<=0) return(RESET);
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCI_)<=0) return(RESET);
   if(CopyBuffer(MOM_Handle,0,0,to_copy,MOM_)<=0) return(RESET);
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI_)<=0) return(RESET);
   if(CopyBuffer(ADX_Handle,1,0,to_copy,DMIP_)<=0) return(RESET);
   if(CopyBuffer(ADX_Handle,2,0,to_copy,DMIM_)<=0) return(RESET);

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(MA_,true);
   ArraySetAsSeries(MACD_,true);
   ArraySetAsSeries(OsMA_,true);
   ArraySetAsSeries(CCI_,true);
   ArraySetAsSeries(MOM_,true);
   ArraySetAsSeries(RSI_,true);
   ArraySetAsSeries(DMIP_,true);
   ArraySetAsSeries(DMIM_,true);
   ArraySetAsSeries(close,true);

//----   
   maxbar=rates_total-min_rates_total_1-1;

//---- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      tmp=MAClose(bar,MA_,close)+MACD(bar,MACD_)+OsMA(bar,OsMA_)+CCI(bar,CCI_)+MOM(bar,MOM_)+RSI(bar,RSI_)+ADX(bar,DMIP_,DMIM_);
      ExtLineBuffer[bar]=XMA1.XMASeries(maxbar,prev_calculated,rates_total,bMA_Method,bPhase,bLength,tmp,bar,true);
     }
     
//---- пересчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      limit--;

//---- Основной цикл раскраски сигнальной линии
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      int clr=1;
      if(ExtLineBuffer[bar+1]<ExtLineBuffer[bar]) clr=2;
      if(ExtLineBuffer[bar+1]>ExtLineBuffer[bar]) clr=0;
      ColorExtLineBuffer[bar]=clr;
     }
     
//---- пересчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      limit=rates_total-min_rates_total+1;
//---- основной цикл расчёта индикатора стандартных отклонений
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- загружаем приращения индикатора в массив для промежуточных вычислений
      for(int iii=0; iii<int(std_period); iii++) dWave[iii]=ExtLineBuffer[bar+iii]-ExtLineBuffer[bar+iii+1];

      //---- находим простое среднее приращений индикатора
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dWave[iii];
      SMAdif=Sum/std_period;

      //---- находим сумму квадратов разностей приращений и среднего
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dWave[iii]-SMAdif,2);

      //---- определяем итоговое значение среднеквадратичного отклонения StDev от приращения индикатора
      StDev=MathSqrt(Sum/std_period);

      //---- инициализация переменных
      dstd=NormalizeDouble(dWave[0],_Digits+2);
      Filter1=NormalizeDouble(dK1*StDev,_Digits+2);
      Filter2=NormalizeDouble(dK2*StDev,_Digits+2);
      BEARS1=EMPTY_VALUE;
      BULLS1=EMPTY_VALUE;
      BEARS2=EMPTY_VALUE;
      BULLS2=EMPTY_VALUE;
      wave=ExtLineBuffer[bar];

      //---- вычисление индикаторных значений
      if(dstd<-Filter1 && dstd>=-Filter2) BEARS1=wave; //есть нисходящий тренд
      if(dstd<-Filter2) BEARS2=wave; //есть нисходящий тренд
      if(dstd>+Filter1 && dstd<=+Filter2) BULLS1=wave; //есть восходящий тренд
      if(dstd>+Filter2) BULLS2=wave; //есть восходящий тренд

      //---- инициализация ячеек индикаторных буферов полученными значениями 
      BullsBuffer1[bar]=BULLS1;
      BearsBuffer1[bar]=BEARS1;
      BullsBuffer2[bar]=BULLS2;
      BearsBuffer2[bar]=BEARS2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
