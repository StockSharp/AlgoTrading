//+------------------------------------------------------------------+ 
//|                                                    NonLagDot.mq5 | 
//|                                Copyright © 2006, TrendLaboratory |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                   E-mail: igorad2003@yahoo.co.uk |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, TrendLaboratory"
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов 2
#property indicator_buffers 2
//+-----------------------------------+
//|  объявление констант              |
//+-----------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчет индикатора
#define PI     3.1415926535 // Число пи
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- использовано всего одно графическое построение
#property indicator_plots   1
//---- отрисовка индикатора в виде цветных значков
#property indicator_type1   DRAW_COLOR_ARROW
#property indicator_color1  clrGray,clrMagenta,clrGreen
#property indicator_width1  2
#property indicator_label1  "NonLagDot"
//+-----------------------------------+
//|  Входные параметры индикатора     |
//+-----------------------------------+
input ENUM_APPLIED_PRICE Price=PRICE_CLOSE;       // Ценовая константа
input ENUM_MA_METHOD     Type=MODE_SMA;           // Метод усреднения
input int                Length=10;               // Период расчета индикатора
input int                Filter= 0;
input double             Deviation=0;             // Девиация
input int                Shift=0;                 // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double MABuffer[];
double ColorMABuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление целочисленных переменных для хендлов индикаторов
int MA_Handle;
//---- объявление глобальных переменных
int Phase;
double Coeff,Len,Cycle,dT1,dT2,Kd,Fi;
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация констант
   Coeff= 3*PI;
   Phase=Length-1;
   Cycle= 4;
   Len=Length*Cycle + Phase;
   dT1=(2*Cycle-1)/(Cycle*Length-1);
   dT2=1.0/(Phase-1);
   Kd=1.0+Deviation/100;
   Fi=Filter*_Point;

//---- инициализация переменных начала отсчета данных 
   min_rates_total=int(Length+Len+1);

//---- получение хендла индикатора iMA
   MA_Handle=iMA(NULL,0,Length,0,Type,Price);
   if(MA_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iMA");

//---- превращение динамического массива MABuffer[] в индикаторный буфер
   SetIndexBuffer(0,MABuffer,INDICATOR_CALCULATIONS);
//---- осуществление сдвига индикатора по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(MABuffer,true);

//---- превращение динамического массива ColorMABuffer[] в индикаторный буфер
   SetIndexBuffer(1,ColorMABuffer,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига индикатора по горизонтали  
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора   
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorMABuffer,true);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"NonLagDot( Length = ",Length,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| Custom iteration function                                        | 
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
   if(BarsCalculated(MA_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- объявления локальных переменных 
   int to_copy,limit,bar,trend0;
   double MA[],alfa,beta,t,Sum,Weight,g;
   static int trend1;

//---- расчеты необходимого количества копируемых данных
//---- и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      to_copy=rates_total;                 // расчетное количество всех баров
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
     }
   else
     {
      to_copy=rates_total-prev_calculated+int(Len); // расчетное количество только новых баров
      limit=rates_total-prev_calculated;            // стартовый номер для расчета новых баров
     }

//---- копируем вновь появившиеся данные в массив
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(MA,true);

   trend0=trend1;

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Weight=0;
      Sum=0;
      t=0;

      for(int iii=0; iii<=Len-1; iii++)
        {
         g=1.0/(Coeff*t+1);
         if(t<=0.5) g=1;
         beta=MathCos(PI*t);
         alfa=g*beta;
         Sum+=alfa*MA[bar+iii];
         Weight+=alfa;
         if(t<1) t+=dT2;
         else if(t<Len-1) t+=dT1;
        }

      if(Weight>0) MABuffer[bar]=Kd*Sum/Weight;

      if(Filter>0) if(MathAbs(MABuffer[bar]-MABuffer[bar-1])<Fi) MABuffer[bar]=MABuffer[bar-1];

      if(MABuffer[bar]-MABuffer[bar+1]>Fi) trend0=+1;
      if(MABuffer[bar+1]-MABuffer[bar]>Fi) trend0=-1;

      ColorMABuffer[bar]=0;

      if(trend0>0) ColorMABuffer[bar]=2;
      if(trend0<0) ColorMABuffer[bar]=1;
      if(bar) trend1=trend0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+ 
