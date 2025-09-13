//+------------------------------------------------------------------+ 
//|                                                MFI_Histogram.mq5 | 
//|                               Copyright © 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 3
#property indicator_buffers 3 
//---- использовано одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//|  объявление констант              |
//+-----------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчёт индикатора
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде гистограммы
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- в качестве цветов индикатора использованы
#property indicator_color1  clrMediumTurquoise,clrGray,clrGold
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1  "MFI_Histogram"

//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input uint                 MFIPeriod=14;           // период индикатора
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // объём 
input uint                 HighLevel=70;           // уровень перекупленности
input uint                 LowLevel=30;            // уровень перепроданности
input int                  Shift=0;                // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+

//---- Объявление целых переменных начала отсчёта данных
int  min_rates_total;
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double UpBuffer[],DnBuffer[],ColorBuffer[];
//---- Объявление целых переменных для хендлов индикаторов
int MFI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=int(MFIPeriod);
//---- получение хендла индикатора iMFI
   MFI_Handle=iMFI(NULL,0,MFIPeriod,VolumeType);
   if(MFI_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMFI");
      return(INIT_FAILED);
     }   
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- осуществление сдвига индикатора по горизонтали на InpKijun
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(UpBuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(DnBuffer,true);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(2,ColorBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorBuffer,true);

//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"MFI_Histogram("+string(MFIPeriod)+")");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- количество  горизонтальных уровней индикатора 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,50);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- в качестве цветов линий горизонтальных уровней использованы серый и розовый цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrGreen);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrBrown);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(MFI_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- объявления локальных переменных
   int to_copy,limit,bar;
   
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров

   to_copy=limit+1;

//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(MFI_Handle,0,0,to_copy,UpBuffer)<=0) return(RESET);

//---- основной цикл раскраски индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      DnBuffer[bar]=50.0;
      int clr=1.0;
      if(UpBuffer[bar]>HighLevel) clr=0.0;
      else if(UpBuffer[bar]<LowLevel) clr=2.0;
      ColorBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
