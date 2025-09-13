//+------------------------------------------------------------------+
//|                                                  Super_Trend.mq5 |
//|                   Copyright © 2005, Jason Robinson (jnrtrading). | 
//|                                      http://www.jnrtrading.co.uk | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2005, Jason Robinson (jnrtrading)." 
#property link      "http://www.jnrtrading.co.uk" 
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- количество индикаторных буферов 4
#property indicator_buffers 4 
//---- использовано всего четыре графических построения
#property indicator_plots   4
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1 DRAW_LINE
//---- в качестве окраски индикатора использован цвет MediumSeaGreen
#property indicator_color1 clrMediumSeaGreen
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки сигнальной линии
#property indicator_label1  "Super_Trend Up"
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type2 DRAW_LINE
//---- в качестве окраски индикатора использован цвет Red
#property indicator_color2 clrRed
//---- линия индикатора - сплошная
#property indicator_style2 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width2 2
//---- отображение метки сигнальной линии
#property indicator_label2  "Super_Trend Down"
//+----------------------------------------------+
//| Параметры отрисовки бычьего индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде значка
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета бычьей линии индикатора использован цвет MediumTurquoise
#property indicator_color3  clrMediumTurquoise
//---- линия индикатора 3 - непрерывная кривая
#property indicator_style3  STYLE_SOLID
//---- толщина линии индикатора 3 равна 1
#property indicator_width3  1
//---- отображение бычьей метки индикатора
#property indicator_label3  "Buy Super_Trend signal"
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде значка
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован цвет DarkOrange
#property indicator_color4  clrDarkOrange
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style4  STYLE_SOLID
//---- толщина линии индикатора 4 равна 1
#property indicator_width4  1
//---- отображение медвежьей метки индикатора
#property indicator_label4  "Sell Super_Trend signal"
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET 0                             // константа для возврата терминалу команды на пересчет индикатора
#define UP_DOWN_SHIFT_CR  0
#define UP_DOWN_SHIFT_M1  3
#define UP_DOWN_SHIFT_M2  3
#define UP_DOWN_SHIFT_M3  4
#define UP_DOWN_SHIFT_M4  5
#define UP_DOWN_SHIFT_M5  5
#define UP_DOWN_SHIFT_M6  5
#define UP_DOWN_SHIFT_M10 6
#define UP_DOWN_SHIFT_M12 6
#define UP_DOWN_SHIFT_M15 7
#define UP_DOWN_SHIFT_M20 8
#define UP_DOWN_SHIFT_M30 9
#define UP_DOWN_SHIFT_H1  20
#define UP_DOWN_SHIFT_H2  27
#define UP_DOWN_SHIFT_H3  30
#define UP_DOWN_SHIFT_H4  35
#define UP_DOWN_SHIFT_H6  33
#define UP_DOWN_SHIFT_H8  35
#define UP_DOWN_SHIFT_H12 37
#define UP_DOWN_SHIFT_D1  40
#define UP_DOWN_SHIFT_W1  100
#define UP_DOWN_SHIFT_MN1 120
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int CCIPeriod=14; // Период индикатора CCI 
input int Level=0;      // Уровень срабатывания CCI
input int Shift=0;      // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double TrendUp[],TrendDown[];
double SignUp[];
double SignDown[];
//----
double UpDownShift;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление целочисленных переменных для хендлов индикаторов
int CCI_Handle;
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
int GetUpDownShift(ENUM_TIMEFRAMES Timeframe)
  {
//----
   switch(Timeframe)
     {
      case PERIOD_M1:     return(UP_DOWN_SHIFT_M1);
      case PERIOD_M2:     return(UP_DOWN_SHIFT_M2);
      case PERIOD_M3:     return(UP_DOWN_SHIFT_M3);
      case PERIOD_M4:     return(UP_DOWN_SHIFT_M4);
      case PERIOD_M5:     return(UP_DOWN_SHIFT_M5);
      case PERIOD_M6:     return(UP_DOWN_SHIFT_M6);
      case PERIOD_M10:     return(UP_DOWN_SHIFT_M10);
      case PERIOD_M12:     return(UP_DOWN_SHIFT_M12);
      case PERIOD_M15:     return(UP_DOWN_SHIFT_M15);
      case PERIOD_M20:     return(UP_DOWN_SHIFT_M20);
      case PERIOD_M30:     return(UP_DOWN_SHIFT_M30);
      case PERIOD_H1:     return(UP_DOWN_SHIFT_H1);
      case PERIOD_H2:     return(UP_DOWN_SHIFT_H2);
      case PERIOD_H3:     return(UP_DOWN_SHIFT_H3);
      case PERIOD_H4:     return(UP_DOWN_SHIFT_H4);
      case PERIOD_H6:     return(UP_DOWN_SHIFT_H6);
      case PERIOD_H8:     return(UP_DOWN_SHIFT_H8);
      case PERIOD_H12:     return(UP_DOWN_SHIFT_H12);
      case PERIOD_D1:     return(UP_DOWN_SHIFT_D1);
      case PERIOD_W1:     return(UP_DOWN_SHIFT_W1);
      case PERIOD_MN1:     return(UP_DOWN_SHIFT_MN1);
     }
//----
   return(UP_DOWN_SHIFT_CR);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(CCIPeriod)+1;
//---- получение хендла индикатора CCI
   CCI_Handle=iCCI(NULL,0,CCIPeriod,PRICE_TYPICAL);
   if(CCI_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора CCI");
      return(INIT_FAILED);
     }
//---- инициализация переменной для сдвига значений     
   UpDownShift=GetUpDownShift(Period())*_Point;
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Super_Trend(",string(CCIPeriod),", ",string(Shift),")");
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
//--- завершение инициализации
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
   if(BarsCalculated(CCI_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//---- объявления локальных переменных 
   double CCI[],cciTrendNow,cciTrendPrevious;
   int limit,to_copy,bar;
//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);
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
//----
   to_copy=limit+2;
//----
   to_copy++;
//---- копируем вновь появившиеся данные в массив CCI[]
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCI)<=0) return(RESET);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      SignUp[bar]=0.0;
      SignDown[bar]=0.0;
      SignUp[bar+1]=0.0;
      SignDown[bar+1]=0.0;
      //----
      cciTrendNow=CCI[bar]+70;
      cciTrendPrevious=CCI[bar+1]+70;
      //----
      if(cciTrendNow>=Level && cciTrendPrevious<Level) TrendUp[bar+1]=TrendDown[bar+1];
      if(cciTrendNow<=Level && cciTrendPrevious>Level) TrendDown[bar+1]=TrendUp[bar+1];
      //----
      if(cciTrendNow>Level)
        {
         TrendDown[bar]=0.0;
         TrendUp[bar]=low[bar]-UpDownShift;
         if(close[bar]<open[bar] && TrendDown[bar+1]!=TrendUp[bar+1]) TrendUp[bar]=TrendUp[bar+1];
         if(TrendUp[bar]<TrendUp[bar+1] && TrendDown[bar+1]!=TrendUp[bar+1]) TrendUp[bar]=TrendUp[bar+1];
         if(high[bar]<high[bar+1] && TrendDown[bar+1]!=TrendUp[bar+1]) TrendUp[bar]=TrendUp[bar+1];
        }
      //----
      if(cciTrendNow<Level)
        {
         TrendUp[bar]=0.0;
         TrendDown[bar]=high[bar]+UpDownShift;
         if(close[bar]>open[bar] && TrendUp[bar+1]!=TrendDown[bar+1]) TrendDown[bar]=TrendDown[bar+1];
         if(TrendDown[bar]>TrendDown[bar+1] && TrendDown[bar+1]!=TrendUp[bar+1]) TrendDown[bar]=TrendDown[bar+1];
         if(low[bar]>low[bar+1] && TrendUp[bar+1]!=TrendDown[bar+1]) TrendDown[bar]=TrendDown[bar+1];
        }
      //----
      if(TrendDown[bar+1]!=0.0 && TrendUp[bar]!=0.0) SignUp[bar+1]=TrendDown[bar+1];
      if(TrendUp[bar+1]!=0.0 && TrendDown[bar]!=0.0) SignDown[bar+1]=TrendUp[bar+1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+