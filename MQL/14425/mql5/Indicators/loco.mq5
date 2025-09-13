//+------------------------------------------------------------------+
//|                                                         Loco.mq5 |
//|                                     Copyright © 2008, John Smith | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
//---- авторство индикатора
#property copyright "Copyright © 2008, John Smith"
//---- авторство индикатора
#property link      ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора в виде двухцветного значка
#property indicator_type1   DRAW_COLOR_ARROW
//---- в качестве цветов значка индикатора использованы
#property indicator_color1 clrLime,clrMagenta
//---- толщина линии индикатора 1 равна 3
#property indicator_width1  3
//---- отображение метки индикатора
#property indicator_label1  "Loco"
//+----------------------------------------------+
//| Объявление перечислений                      |
//+----------------------------------------------+
enum ENUM_APPLIED_PRICE_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //PRICE_CLOSE
   PRICE_OPEN_,          //PRICE_OPEN
   PRICE_HIGH_,          //PRICE_HIGH
   PRICE_LOW_,           //PRICE_LOW
   PRICE_MEDIAN_,        //PRICE_MEDIAN
   PRICE_TYPICAL_,       //PRICE_TYPICAL
   PRICE_WEIGHTED_,      //PRICE_WEIGHTED
   PRICE_SIMPL_,         //PRICE_SIMPL_
   PRICE_QUARTER_,       //PRICE_QUARTER_
   PRICE_TRENDFOLLOW0_,  //PRICE_TRENDFOLLOW0_
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price 
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint Length=1;                              // Глубина расчета                   
input ENUM_APPLIED_PRICE_ IPC=PRICE_CLOSE_;       // Ценовая константа
input int Shift=0;                                // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0;                           // Сдвиг индикатора по вертикали в пунктах
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double LocoBuffer[],ColorLocoBuffer[];
double dPriceShift;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(Length)+1;
//---- инициализация переменных
   dPriceShift=PriceShift*_Point;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,LocoBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(LocoBuffer,true);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorLocoBuffer,true);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorLocoBuffer,INDICATOR_COLOR_INDEX);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Loco(",Length,", ",EnumToString(IPC),", ",Shift,", ",PriceShift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);
//---- объявление локальных переменных 
   int limit,bar;
   double series0,series1,result;
   static double prev;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
      prev=PriceSeries(IPC,limit,open,low,high,close);
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   result=prev;
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      series0=PriceSeries(IPC,bar,open,low,high,close);
      series1=PriceSeries(IPC,bar+Length,open,low,high,close);
      //----
      if(series0==prev)result=prev;
      else
        {
         if(series1>prev && series0>prev)
           {
            result=MathMax(prev,series0*0.999);
            ColorLocoBuffer[bar]=0;
           }
         else if(series1<prev && series0<prev)
           {
            result=MathMin(prev,series0*1.001);
            ColorLocoBuffer[bar]=1;
           }
         else
           {
            if(series0>prev)
              {
               result=series0*0.999;
               ColorLocoBuffer[bar]=0;
              }
            else
              {
               result=series0*1.001;
               ColorLocoBuffer[bar]=1;
              }
           }
        }
      LocoBuffer[bar]=result+dPriceShift;
      if(bar) prev=result;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+   
//| Получение значения ценовой таймсерии                             |
//+------------------------------------------------------------------+ 
double PriceSeries(uint applied_price,// ценовая константа
                   uint   bar,        // индекс сдвига относительно текущего бара на указанное количество периодов назад или вперед
                   const double &Open[],
                   const double &Low[],
                   const double &High[],
                   const double &Close[])
  {
//----
   switch(applied_price)
     {
      //---- ценовые константы из перечисления ENUM_APPLIED_PRICE
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
