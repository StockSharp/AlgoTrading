//+------------------------------------------------------------------+ 
//|                                         ColorZerolagTriXOSMA.mq5 | 
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//---- авторство индикатора
#property copyright "Copyright © 2015, Nikolay Kositsin"
//---- ссылка на сайт автора
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде четырехцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов четырехцветной гистограммы использованы
#property indicator_color1 clrOrange,clrYellow,clrGray,clrYellowGreen,clrTeal
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "ColorZerolagTriXOSMA"
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint    smoothing1=15;
input uint    smoothing2=7;
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // Ценовая константа
//----
input double Factor1=0.05;
input uint    TriX_period1=8;
//----
input double Factor2=0.10;
input uint    TriX_period2=21;
//----
input double Factor3=0.16;
input uint    TriX_period3=34;
//----
input double Factor4=0.26;
input int    TriX_period4=55;
//----
input double Factor5=0.43;
input uint    TriX_period5=89;
//+-----------------------------------+
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление переменных с плавающей точкой
double smoothConst1,smoothConst2;
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double ExtBuffer[];
double ColorExtBuffer[];
//---- объявление переменных для хранения хендлов индикаторов
int TriX1_Handle,TriX2_Handle,TriX3_Handle,TriX4_Handle,TriX5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagTriX indicator initialization function                    | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация констант
   smoothConst1=(smoothing1-1.0)/smoothing1;
   smoothConst2=(smoothing2-1.0)/smoothing2;
//---- 
   uint PeriodBuffer[5];
//---- расчет стартового бара
   PeriodBuffer[0] = TriX_period1;
   PeriodBuffer[1] = TriX_period2;
   PeriodBuffer[2] = TriX_period3;
   PeriodBuffer[3] = TriX_period4;
   PeriodBuffer[4] = TriX_period5;
//----
   min_rates_total=int(3*PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)])+2;
//---- получение хендла индикатора iTriX1
   TriX1_Handle=iTriX(NULL,0,TriX_period1,IPC);
   if(TriX1_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX1");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iTriX2
   TriX2_Handle=iTriX(NULL,0,TriX_period2,IPC);
   if(TriX2_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX2");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iTriX3
   TriX3_Handle=iTriX(NULL,0,TriX_period3,IPC);
   if(TriX3_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX3");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iTriX4
   TriX4_Handle=iTriX(NULL,0,TriX_period4,IPC);
   if(TriX4_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX4");
      return(INIT_FAILED);
     }
//---- получение хендла индикатора iTriX5
   TriX5_Handle=iTriX(NULL,0,TriX_period5,IPC);
   if(TriX5_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iTriX5");
      return(INIT_FAILED);
     }
//---- превращение динамического массива MAMABuffer в индикаторный буфер
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtBuffer,true);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorExtBuffer,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname="ColorZerolagTriXOSMA";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,6);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagTriX iteration function                                   | 
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
   if(BarsCalculated(TriX1_Handle)<rates_total
      || BarsCalculated(TriX2_Handle)<rates_total
      || BarsCalculated(TriX3_Handle)<rates_total
      || BarsCalculated(TriX4_Handle)<rates_total
      || BarsCalculated(TriX5_Handle)<rates_total
      || rates_total<min_rates_total)
      return(0);
//---- объявление переменных с плавающей точкой  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend,OSMA,diff;
   double TriX1[],TriX2[],TriX3[],TriX4[],TriX5[];
//---- объявление целых переменных
   int limit,to_copy,bar,clr;
   static double SlowTrend1,OSMA1;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-2; // стартовый номер для расчета всех баров
      to_copy=limit+2;
     }
   else // стартовый номер для расчета новых баров
     {
      limit=rates_total-prev_calculated;  // стартовый номер для расчета только новых баров
      to_copy=limit+1;
     }
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(TriX1,true);
   ArraySetAsSeries(TriX2,true);
   ArraySetAsSeries(TriX3,true);
   ArraySetAsSeries(TriX4,true);
   ArraySetAsSeries(TriX5,true);
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(TriX1_Handle,0,0,to_copy,TriX1)<=0) return(0);
   if(CopyBuffer(TriX2_Handle,0,0,to_copy,TriX2)<=0) return(0);
   if(CopyBuffer(TriX3_Handle,0,0,to_copy,TriX3)<=0) return(0);
   if(CopyBuffer(TriX4_Handle,0,0,to_copy,TriX4)<=0) return(0);
   if(CopyBuffer(TriX5_Handle,0,0,to_copy,TriX5)<=0) return(0);
//---- расчет стартового номера limit для цикла пересчета баров и стартовая инициализация переменных
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      bar=limit+1;
      Osc1 = Factor1 * TriX1[bar];
      Osc2 = Factor2 * TriX2[bar];
      Osc3 = Factor2 * TriX3[bar];
      Osc4 = Factor4 * TriX4[bar];
      Osc5 = Factor5 * TriX5[bar];
      //----
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      SlowTrend1=FastTrend/smoothing1;
      OSMA1=(FastTrend-SlowTrend1)/smoothing2;
     }
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * TriX1[bar];
      Osc2 = Factor2 * TriX2[bar];
      Osc3 = Factor2 * TriX3[bar];
      Osc4 = Factor4 * TriX4[bar];
      Osc5 = Factor5 * TriX5[bar];
      //----
      FastTrend = Osc1 + Osc2 + Osc3 + Osc4 + Osc5;
      SlowTrend = FastTrend / smoothing1 + SlowTrend1 * smoothConst1;
      //----
      OSMA=(FastTrend-SlowTrend)/smoothing2+OSMA1*smoothConst2;
      ExtBuffer[bar]=OSMA;
      if(bar)
        {
         SlowTrend1=SlowTrend;
         OSMA1=OSMA;
        }
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit--;
//---- основной цикл раскраски индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      clr=2;
      diff=ExtBuffer[bar]-ExtBuffer[bar+1];
      //----
      if(ExtBuffer[bar]>0)
        {
         if(diff>0) clr=4;
         if(diff<0) clr=3;
        }
      //----
      if(ExtBuffer[bar]<0)
        {
         if(diff<0) clr=0;
         if(diff>0) clr=1;
        }
      //----
      ColorExtBuffer[bar]=clr;
     }
//----
   return(rates_total);
  }
//+------------------------------------------------------------------+
