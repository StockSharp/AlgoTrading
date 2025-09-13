//+------------------------------------------------------------------+
//|                                                 FineTuningMA.mq5 | 
//|                                         Copyright © 2010, gumgum |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, gumgum"
#property link      ""
//---- номер версии индикатора
#property version   "1.00"
//---- описание индикатора
#property description "Мувинг с более тонкой настройкой"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов
#property indicator_buffers 1 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован RoyalBlue цвет
#property indicator_color1 clrRoyalBlue
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 3
#property indicator_width1  3
//---- отображение метки индикатора
#property indicator_label1  "FineTuningMA"
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
enum Applied_price_ //тип константы
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
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint   FTMA=10;
input double rank1=2;
input double rank2=2;
input double rank3=2;
input double shift1=1;
input double shift2=1;
input double shift3=1;
input Applied_price_ IPC=PRICE_TYPICAL; // Ценовая константа
/* , по которой производится расчет индикатора ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // Сдвиг индикатора по вертикали в пунктах
//+-----------------------------------+
//---- объявление динамического массива, который будет в 
//---- дальнейшем использован в качестве индикаторного буфера
double LineBuffer[];
//----
double PM[];
//---- объявление переменной значения вертикального сдвига скользящей средней
double dPriceShift;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+   
//| FineTuningMA indicator initialization function                   | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
//---- распределение памяти под массивы переменных  
   ArrayResize(PM,FTMA);
//---- инициализация переменных
   double sum=0;
   for(int h=0; h<int(FTMA); h++)
     {
      PM[h]=shift1+MathPow(h/(FTMA-1.0),rank1)*(1.0-shift1);
      PM[h]=(shift2+MathPow(1-(h/(FTMA-1.0)),rank2)*(1.0-shift2))*PM[h];
      //----
      if((h/(FTMA-1.))<0.5) PM[h]=(shift3+MathPow((1-(h/(FTMA-1.0))*2.0),rank3)*(1.0-shift3))*PM[h];
      else PM[h]=(shift3+MathPow((h/(FTMA-1.0))*2.0-1.0,rank3)*(1.0-shift3))*PM[h];
      //----
      sum+=PM[h];
     }
//----
   double sum1=0;
   for(int h=0; h<int(FTMA); h++)
     {
      PM[h]=PM[h]/sum;
      sum1+=PM[h];
     }
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(FTMA);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,LineBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"FineTuningMA");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| FineTuningMA iteration function                                  | 
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
   if(rates_total<min_rates_total) return(0);
//---- объявление целых переменных и получение уже посчитанных баров
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=min_rates_total; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double sum1=0;
      for(int h=0; h<int(FTMA); h++) sum1+=PM[h]*PriceSeries(IPC,bar-h,open,low,high,close);
      LineBuffer[bar]=sum1+dPriceShift;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| PriceSeries() function                                           |
//+------------------------------------------------------------------+
double PriceSeries(uint applied_price, // ценовая константа
                   uint   bar,         // индекс сдвига относительно текущего бара на указанное количество периодов назад или вперед
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
      default: return(Close[bar]);
     }
//----
//return(0);
  }
//+------------------------------------------------------------------+
