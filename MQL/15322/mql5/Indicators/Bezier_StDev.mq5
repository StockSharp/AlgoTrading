//+------------------------------------------------------------------+ 
//|                                                 Bezier_StDev.mq5 | 
//|                                     Copyright © 2007, Lizhniyk E |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, Lizhniyk E"
#property link      "Lizhniyk E"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчёта и отрисовки индикатора использовано четыре буфера
#property indicator_buffers 4
//---- использовано три графических построения
#property indicator_plots   3
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цвета линии индикатора использованы
#property indicator_color1 clrGray,clrDodgerBlue,clrChocolate
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 3
#property indicator_width1  3
//---- отображение метки индикатора
#property indicator_label1  "Bezier"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован розовый цвет
#property indicator_color2  clrMagenta
//---- толщина линии индикатора 2 равна 3
#property indicator_width2  3
//---- отображение медвежьей метки индикатора
#property indicator_label2  "Dn_Signal"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде символа
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован зелёный цвет
#property indicator_color3  clrLime
//---- толщина линии индикатора 3 равна 3
#property indicator_width3  3
//---- отображение бычей метки индикатора
#property indicator_label3  "Up_Signal"
//+----------------------------------------------+
//|  ОБЪЯВЛЕНИЕ ПЕРЕЧИСЛЕНИЙ                     |
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
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//|  ОБЪЯВЛЕНИЕ ПЕРЕЧИСЛЕНИЙ                     |
//+----------------------------------------------+
enum Signal_mode
  {
   Trend, //по тренду
   Kalman //по Кальману
  };
//+----------------------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА                |
//+----------------------------------------------+
input uint BPeriod=8;                      //период усреднения
input double T=0.5;                        //коэффициент чувствительности (от 0 до 1)               
input Applied_price_ IPC=PRICE_WEIGHTED_;  //ценовая константа
input double dK=2.0;                       //коэффициент для квадратичного фильтра
input uint std_period=9;                   //период квадратичного фильтра
input int Shift=0;                         //сдвиг индикатора по горизонтали в барах
input int PriceShift=0;                    //cдвиг индикатора по вертикали в пунктах
//+----------------------------------------------+
//---- индикаторные буферы
double BezierBuffer[],ColorBezierBuffer[];
double BearsBuffer[];
double BullsBuffer[];
//----
double dPriceShift,t,dBezier[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_1;
//+------------------------------------------------------------------+    
//| Bezier indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_1=int(BPeriod);
   min_rates_total=min_rates_1+1+int(std_period);

//---- Инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
   t=MathMin(MathMax(T,0),1);
   
//---- Распределение памяти под массивы переменных  
   ArrayResize(dBezier,std_period);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,BezierBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorBezierBuffer,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
   
//---- превращение динамического массива BearsBuffer в индикаторный буфер
   SetIndexBuffer(2,BearsBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);

//---- превращение динамического массива BullsBuffer в индикаторный буфер
   SetIndexBuffer(3,BullsBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 3 по горизонтали
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
   
//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Bezier(",BPeriod,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| Bezier iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчёта индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчёта индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- Объявление локальных переменных
   int first,bar;
   double SMAdif,Sum,StDev,dstd,BEARS,BULLS,Filter;
//----

   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
        first=min_rates_1+1; // стартовый номер для расчёта всех баров
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double r=0;
      for(int iii=int(BPeriod); iii>=0; iii--)
         r+=PriceSeries(IPC,bar-iii,open,low,high,close)*
            (factorial(BPeriod)/(factorial(iii)*factorial(BPeriod-iii)))*MathPow(t,iii)*MathPow(1-t,BPeriod-iii);
            
      BezierBuffer[bar]=r+dPriceShift;
     }
     
//---- корректировка значения переменной first
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=min_rates_total; // стартовый номер для расчета всех баров
           
//---- Основной цикл раскраски сигнальной линии
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      ColorBezierBuffer[bar]=0;
      if(BezierBuffer[bar-1]<BezierBuffer[bar]) ColorBezierBuffer[bar]=1;
      if(BezierBuffer[bar-1]>BezierBuffer[bar]) ColorBezierBuffer[bar]=2;
     }

//---- основной цикл расчёта индикатора стандартных отклонений
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- загружаем приращения индикатора в массив для промежуточных вычислений
      for(int iii=0; iii<int(std_period); iii++) dBezier[iii]=BezierBuffer[bar-iii-0]-BezierBuffer[bar-iii-1];

      //---- находим простое среднее приращений индикатора
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dBezier[iii];
      SMAdif=Sum/std_period;

      //---- находим сумму квадратов разностей приращений и среднего
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dBezier[iii]-SMAdif,2);

      //---- определяем итоговое значение среднеквадратичного отклонения StDev от приращения индикатора
      StDev=MathSqrt(Sum/std_period);

      //---- инициализация переменных
      dstd=NormalizeDouble(dBezier[0],_Digits+2);
      Filter=NormalizeDouble(dK*StDev,_Digits+2);
      BEARS=0;
      BULLS=0;

      //---- вычисление индикаторных значений
      if(dstd<-Filter) BEARS=BezierBuffer[bar]; //есть нисходящий тренд
      if(dstd>+Filter) BULLS=BezierBuffer[bar]; //есть восходящий тренд

      //---- инициализация ячеек индикаторных буферов полученными значениями 
      BullsBuffer[bar]=BULLS;
      BearsBuffer[bar]=BEARS;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+    
//| вычисление факториала                                            | 
//+------------------------------------------------------------------+  
int factorial(int value)
  {
//---- 
   int res=1;
   for(int j=2; j<value+1; j++) res*=j;
//---- возврат факториала
   return(res);
  }
//+------------------------------------------------------------------+   
//| Получение значения ценовой таймсерии                             |
//+------------------------------------------------------------------+ 
double PriceSeries
(
 uint applied_price,// Ценовая константа
 uint   bar,// Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд).
 const double &Open[],
 const double &Low[],
 const double &High[],
 const double &Close[]
 )
//PriceSeries(applied_price, bar, open, low, high, close)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----
   switch(applied_price)
     {
      //---- Ценовые константы из перечисления ENUM_APPLIED_PRICE
      case  PRICE_CLOSE: return(Close[bar]);
      case  PRICE_OPEN: return(Open [bar]);
      case  PRICE_HIGH: return(High [bar]);
      case  PRICE_LOW: return(Low[bar]);
      case  PRICE_MEDIAN: return((High[bar]+Low[bar])/2.0);
      case  PRICE_TYPICAL: return((Close[bar]+High[bar]+Low[bar])/3.0);
      case  PRICE_WEIGHTED: return((2*Close[bar]+High[bar]+Low[bar])/4.0);

      //----                            
      case  8: return((Open[bar] + Close[bar])/2.0);
      case  9: return((Open[bar] + Close[bar] + High[bar] + Low[bar])/4.0);
      //----                                
      case 10:
        {
         if(Close[bar]>Open[bar])return(High[bar]);
         else
           {
            if(Close[bar]<Open[bar])
               return(Low[bar]);
            else return(Close[bar]);
           }
        }
      //----         
      case 11:
        {
         if(Close[bar]>Open[bar])return((High[bar]+Close[bar])/2.0);
         else
           {
            if(Close[bar]<Open[bar])
               return((Low[bar]+Close[bar])/2.0);
            else return(Close[bar]);
           }
         break;
        }
      //----         
      case 12:
        {
         double res=High[bar]+Low[bar]+Close[bar];
         if(Close[bar]<Open[bar]) res=(res+Low[bar])/2;
         if(Close[bar]>Open[bar]) res=(res+High[bar])/2;
         if(Close[bar]==Open[bar]) res=(res+Close[bar])/2;
         return(((res-Low[bar])+(res-High[bar]))/2);
        }
      //----
      default: return(Close[bar]);
     }
//----
//return(0);
  }
//+------------------------------------------------------------------+
