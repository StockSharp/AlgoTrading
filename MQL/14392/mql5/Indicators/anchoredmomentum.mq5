//+------------------------------------------------------------------+
//|                                             AnchoredMomentum.mq5 | 
//|                              Copyright © 2010, Umnyashkin Victor | 
//|                                       http://www.metaquotes.net/ | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Umnyashkin Victor"
#property link "http://www.metaquotes.net/"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 4
#property indicator_buffers 4 
//---- использовано всего четыре графических построения
#property indicator_plots   4
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован серый цвет
#property indicator_color1 clrGray
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "Momentum"
//+----------------------------------------------+
//| Параметры отрисовки бычьего индикатора        |
//+----------------------------------------------+
//---- отрисовка индикатора в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован салатовый цвет
#property indicator_color2 clrSpringGreen
//---- толщина линии индикатора равна 3
#property indicator_width2 3
//---- отображение бычьей метки индикатора
#property indicator_label2 "Up_Signal"
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//---- отрисовка индикатора в виде символа
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован темно-розовый цвет
#property indicator_color3  clrDeepPink
//---- толщина линии индикатора равна 3
#property indicator_width3 3
//---- отображение медвежьей метки индикатора
#property indicator_label3 "Dn_Signal"
//+----------------------------------------------+
//| Параметры отрисовки безтрендового индикатора |
//+----------------------------------------------+
//---- отрисовка индикатора в виде символа
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета безтрендового индикатора использован серый
#property indicator_color4  clrGray
//---- толщина линии индикатора равна 3
#property indicator_width4 3
//---- отображение безтрендовой метки индикатора
#property indicator_label4 "No_Signal"
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint MomPeriod=8;    // Период SMA 
input uint SmoothPeriod=6; // Период EMA
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // Ценовая константа, по которой производится расчет индикатора
input double UpLevel=+0.025; // Верхний пробойный уровень
input double DnLevel=-0.025; // Нижний пробойный уровень
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//----  дальнейшем использованы в качестве индикаторных буферов
double MomBuffer[];
double UpBuffer[];
double DnBuffer[];
double FlBuffer[];
//---- объявление целочисленных переменных для хендлов индикаторов
int SMA_Handle,EMA_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+   
//| Momentum indicator initialization function                       | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- получение хендла индикатора SMA
   SMA_Handle=iMA(NULL,0,MomPeriod,0,MODE_SMA,IPC);
   if(SMA_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора SMA");
//---- получение хендла индикатора SMA
   EMA_Handle=iMA(NULL,0,MomPeriod,0,MODE_EMA,IPC);
   if(EMA_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора EMA");
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(MomPeriod);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,MomBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(MomBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,UpBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- выбор символа для отрисовки
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(UpBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,DnBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- выбор символа для отрисовки
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(DnBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,FlBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 3
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"No Signal");
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- выбор символа для отрисовки
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(FlBuffer,true);
//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Momentum(",MomPeriod,", ",SmoothPeriod,", ",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,3);
//---- количество горизонтальных уровней индикатора 2   
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,UpLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,DnLevel);
//---- в качестве цветов линий горизонтальных уровней использованы синий и розовый цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrMagenta);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//|  Momentum iteration function                                     | 
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
   if(BarsCalculated(SMA_Handle)<rates_total
      || BarsCalculated(EMA_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);
//---- объявление переменных с плавающей точкой  
   double res,Momentum,SMA[],EMA[];
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int to_copy,limit,bar;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-1-min_rates_total; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }
//---
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(SMA_Handle,0,0,to_copy,SMA)<=0) return(RESET);
   if(CopyBuffer(EMA_Handle,0,0,to_copy,EMA)<=0) return(RESET);
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(SMA,true);
   ArraySetAsSeries(EMA,true);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      res=SMA[bar];
      if(res) Momentum=100*(EMA[bar]/SMA[bar]-1);
      else Momentum=EMPTY_VALUE;
      //---
      MomBuffer[bar]=Momentum;
      //---- инициализация ячеек индикаторных буферов нулями
      UpBuffer[bar]=EMPTY_VALUE;
      DnBuffer[bar]=EMPTY_VALUE;
      FlBuffer[bar]=EMPTY_VALUE;
      //---
      if(Momentum==EMPTY_VALUE) continue;
      //---- инициализация ячеек индикаторных буферов полученными значениями 
      if(Momentum>UpLevel) UpBuffer[bar]=Momentum; //есть восходящий тренд
      else if(Momentum<DnLevel) DnBuffer[bar]=Momentum; //есть нисходящий тренд
      else FlBuffer[bar]=Momentum; //нет тренда
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
