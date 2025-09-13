//+------------------------------------------------------------------+
//|Based on NonLagDOT.mq4 by TrendLaboratory                         |
//|http://finance.groups.yahoo.com/group/TrendLaboratory             |
//|igorad2003@yahoo.co.uk                                            |
//|                                                         Dots.mq5 |
//|                                  Copyright © 2011, EarnForex.com |
//|                                         http://www.earnforex.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, EarnForex"
#property link      "http://www.earnforex.com"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Объявление констант               |
//+-----------------------------------+
#define RESET 0                        // константа для возврата терминалу команды на пересчет индикатора
#define PI 3.1415926535                // константа для числа Пи
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//---- отрисовка индикатора в виде многоцветных значков
#property indicator_type1   DRAW_COLOR_ARROW
//---- в качестве цветов значков использованы
#property indicator_color1  clrBlue,clrRed
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "Dots"
//+-----------------------------------+
//| Объявление перечислений           |
//+-----------------------------------+
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
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input uint Length = 10;                // Глубина сглаживания
input uint Filter = 0;                 // Параметр, позволяющий фильтровать всплески цен без добавления задержек в отображение
input Applied_price_ IPC=PRICE_CLOSE_; // Ценовая константа
input int PriceShift=0;                // Сдвиг индикатора по вертикали в пунктах
input int Shift=0;                     // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- объявление глобальных переменных
double dPriceShift,Coeff,Phase,Res1,Res2,dFilter;
int Len,Cycle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   Coeff=3*PI;
   Phase=Length-1;
   Cycle=4;
   Len=int(Length*Cycle+Phase);
   Res1=1.0/(Phase-1);
   Res2=(2.0*Cycle-1.0)/(Cycle*Length-1.0);
   min_rates_total=int(Len)+1;
//---- инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
   dFilter=_Point*Filter;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Dots(",Length,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| Custom indicator iteration function                              | 
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
//---- объявление переменных с плавающей точкой  
   double alfa,beta,t,Sum,Weight,g,price,MA,MA_prev;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int first,bar,clr;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=min_rates_total; // стартовый номер для расчета всех баров
      bar=first-1;
      IndBuffer[bar]=close[bar]+dPriceShift;
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      Weight=0; Sum=0; t=0;
      //---
      for(int iii=0; iii<Len; iii++)
        {
         g=1.0/(Coeff*t+1);
         if(t<=0.5) g=1;
         beta=MathCos(PI*t);
         alfa=g*beta;
         price=PriceSeries(IPC,bar-iii,open,low,high,close);
         Sum+=alfa*price;
         Weight+=alfa;
         if(t<1) t+=Res1;
         else if(t<Len-1) t+=Res2;
        }
      //---
      MA_prev=IndBuffer[bar-1]-dPriceShift;
      if(Weight) MA=Sum/MathAbs(Weight);
      else MA=MA_prev;
      if(Filter && MathAbs(MA-MA_prev)<dFilter) MA=MA_prev;
      //---
      if(MA-MA_prev>dFilter) clr=0;
      else if(MA_prev-MA>dFilter) clr=1;
      //----       
      IndBuffer[bar]=MA+dPriceShift;
      ColorIndBuffer[bar]=clr;
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
