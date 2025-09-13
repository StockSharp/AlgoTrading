//+------------------------------------------------------------------+
//|                                                   Supertrend.mq5 |
//|                   Copyright © 2005, Jason Robinson (jnrtrading). | 
//|                                      http://www.jnrtrading.co.uk | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2005, Jason Robinson (jnrtrading)." 
#property link      "http://www.jnrtrading.co.uk" 
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- количество индикаторных буферов 4
#property indicator_buffers 4 
//---- использовано всего четыре графических построения
#property indicator_plots   4
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1 DRAW_LINE
//---- в качестве окраски индикатора использован цвет Lime
#property indicator_color1 clrLime
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки сигнальной линии
#property indicator_label1  "Supertrend Up"
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type2 DRAW_LINE
//---- в качестве окраски индикатора использовано три цвета
#property indicator_color2 clrRed
//---- линия индикатора - сплошная
#property indicator_style2 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width2 2
//---- отображение метки сигнальной линии
#property indicator_label2  "Supertrend Down"
//+----------------------------------------------+
//|  Параметры отрисовки бычьего индикатора      |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде значка
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета бычей линии индикатора использован цвет MediumTurquoise
#property indicator_color3  clrMediumTurquoise
//---- линия индикатора 3 - непрерывная кривая
#property indicator_style3  STYLE_SOLID
//---- толщина линии индикатора 3 равна 4
#property indicator_width3  4
//---- отображение бычьей метки индикатора
#property indicator_label3  "Buy Supertrend signal"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде значка
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован цвет DarkOrange
#property indicator_color4  clrDarkOrange
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style4  STYLE_SOLID
//---- толщина линии индикатора 4 равна 4
#property indicator_width4  4
//---- отображение медвежьей метки индикатора
#property indicator_label4  "Sell Supertrend signal"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int CCIPeriod=50; // Период индикатора CCI 
input int ATRPeriod=5;  // Период индикатора ATR
input int Level=0;      // Уровень срабатывания CCI
input int Shift=0;      // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double TrendUp[],TrendDown[];
double SignUp[];
double SignDown[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление целочисленных переменных для хендлов индикаторов
int ATR_Handle,CCI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=MathMax(CCIPeriod,ATRPeriod);
//---- получение хендла индикатора CCI
   CCI_Handle=iCCI(NULL,0,CCIPeriod,PRICE_TYPICAL);
   if(CCI_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора CCI");
//---- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATRPeriod);
   if(ATR_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора ATR");
//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Supertrend(",string(CCIPeriod),", ",string(ATRPeriod),", ",string(Shift),")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);

//---- превращение динамического массива ExtBuffer[] в индикаторный буфер
   SetIndexBuffer(0,TrendUp,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- индексация элементов в буферах, как в таймсериях   
   ArraySetAsSeries(TrendUp,true);

//---- превращение динамического массива ExtBuffer[] в индикаторный буфер
   SetIndexBuffer(1,TrendDown,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- индексация элементов в буферах, как в таймсериях   
   ArraySetAsSeries(TrendDown,true);

//---- превращение динамического массива SignUp [] в индикаторный буфер
   SetIndexBuffer(2,SignUp,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах, как в таймсериях   
   ArraySetAsSeries(SignUp,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//---- символ для индикатора
   PlotIndexSetInteger(2,PLOT_ARROW,108);

//---- превращение динамического массива SignDown[] в индикаторный буфер
   SetIndexBuffer(3,SignDown,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах, как в таймсериях   
   ArraySetAsSeries(SignDown,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0.0);
//---- символ для индикатора
   PlotIndexSetInteger(3,PLOT_ARROW,108);
//----
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
   if
   (BarsCalculated(CCI_Handle)<rates_total
    || BarsCalculated(ATR_Handle)<rates_total
    || rates_total<min_rates_total) return(0);

//---- объявления локальных переменных 
   double ATR[],CCI[];
   int limit,to_copy,bar;

//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(CCI,true);

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total;                 // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated;                 // стартовый номер для расчета новых баров
     }

   to_copy=limit+1;

//---- копируем вновь появившиеся данные в массив ATR[]
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(0);

   to_copy++;
//---- копируем вновь появившиеся данные в массив CCI[]
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCI)<=0) return(0);

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      TrendUp[bar]=0.0;
      TrendDown[bar]=0.0;
      SignUp[bar]=0.0;
      SignDown[bar]=0.0;

      if(CCI[bar]>=Level && CCI[bar+1]<Level) TrendUp[bar]=TrendDown[bar+1];

      if(CCI[bar]<=Level && CCI[bar+1]>Level) TrendDown[bar]=TrendUp[bar+1];

      if(CCI[bar]>Level)
        {
         TrendUp[bar]=low[bar]-ATR[bar];
         if(TrendUp[bar]<TrendUp[bar+1] && CCI[bar+1]>=Level) TrendUp[bar]=TrendUp[bar+1];
        }

      if(CCI[bar]<Level)
        {
         TrendDown[bar]=high[bar]+ATR[bar];
         if(TrendDown[bar]>TrendDown[bar+1] && CCI[bar+1]<=Level) TrendDown[bar]=TrendDown[bar+1];
        }

      if(TrendDown[bar+1]!=0.0 && TrendUp[bar]!=0.0) SignUp[bar]=TrendUp[bar];

      if(TrendUp[bar+1]!=0.0 && TrendDown[bar]!=0.0) SignDown[bar]=TrendDown[bar];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
