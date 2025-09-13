//+------------------------------------------------------------------+
//|                                            ForecastOscilator.mq5 |
//|                Copyright © 2005, Nick Bilak, beluck[AT]gmail.com |
//|                                    http://metatrader.50webs.com/ |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2005, Nick Bilak"
//---- ссылка на сайт автора
#property link      "http://metatrader.50webs.com/"
//---- номер версии индикатора
#property version   "1.01"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- для расчета и отрисовки индикатора использовано четыре буфера
#property indicator_buffers 4
//---- использовано всего четыре графических построения
#property indicator_plots   4
//+----------------------------------------------+
//| Параметры отрисовки индикатора 1             |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован бледно-голубой цвет
#property indicator_color1  clrCornflowerBlue
//---- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "Forecast Oscilator"
//+----------------------------------------------+
//| Параметры отрисовки индикатора 2             |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_LINE
//---- в качестве цвета линии индикатора использован оранжевый цвет
#property indicator_color2  clrOrange
//---- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//---- отображение метки индикатора
#property indicator_label2 "Signal line"
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде символа
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован розовый цвет
#property indicator_color3  clrMagenta
//---- толщина линии индикатора 3 равна 4
#property indicator_width3  4
//---- отображение медвежьей метки индикатора
#property indicator_label3  "Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьего индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде символа
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета бычьей линии индикатора использован зеленый цвет
#property indicator_color4  clrLime
//---- толщина линии индикатора 4 равна 4
#property indicator_width4  4
//---- отображение бычьей метки индикатора
#property indicator_label4 "Buy"
//+----------------------------------------------+
//| Объявление перечисления                      |
//+----------------------------------------------+
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
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint   length=15;
input uint   t3=3;
input double b=0.7;
input Applied_price_ IPC=PRICE_CLOSE_;// Ценовая константа
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double BuyBuffer[];
double SellBuffer[];
double IndBuffer[],SigBuffer[];
//---
int min_rates_total;
double b2,b3,c1,c2,c3,c4,w1,w2,n,Kx,Br;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация глобальных переменных
   b2=b*b;
   b3=b2*b;
   c1=-b3;
   c2=(3*(b2+b3));
   c3=-3*(2*b2+b+b3);
   c4=(1+3*b+b3+3*b2);
//----
   n=MathMax(n,t3);
   n=1+0.5*(n-1);
   w1=2/(n+1);
   w2=1-w1;
   Kx=6.0/(length*(length+1.0));
   Br=(length+1.0)/3.0;
   min_rates_total=int(length+1);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,SigBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- символ для индикатора
   PlotIndexSetInteger(2,PLOT_ARROW,158);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- символ для индикатора
   PlotIndexSetInteger(3,PLOT_ARROW,158);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="Forecast Oscilator";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
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
//---- объявления локальных переменных 
   int first,bar;
   double WT,forecastosc,t3_fosc,sum,e1,e2,e3,e4,e5,e6,tmp,tmp2;
   static double e1_,e2_,e3_,e4_,e5_,e6_;
//---- расчеты необходимого количества копируемых данных и
//---- стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      first=min_rates_total+1; // стартовый номер для расчета всех баров
      e1_=0.0;
      e2_=0.0;
      e3_=0.0;
      e4_=0.0;
      e5_=0.0;
      e6_=0.0;
     }
   else
     {
      first=prev_calculated-1; // стартовый номер для расчета новых баров
     }
//---- восстанавливаем значения переменных
   e1=e1_;
   e2=e2_;
   e3=e3_;
   e4=e4_;
   e5=e5_;
   e6=e6_;
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==rates_total-1)
        {
         e1_=e1;
         e2_=e2;
         e3_=e3;
         e4_=e4;
         e5_=e5;
         e6_=e6;
        }
      //----
      sum=0.0;
      for(int i=int(length); i>0; i--)
        {
         tmp=Br;
         tmp2=i;
         tmp=tmp2-tmp;
         sum+=tmp*PriceSeries(IPC,bar-length+i,open,low,high,close);
        }
      //----
      WT=sum*Kx;
      //----
      forecastosc=(PriceSeries(IPC,bar,open,low,high,close)-WT)/WT*100;
      e1=w1*forecastosc + w2*e1;
      e2=w1*e1 + w2*e2;
      e3=w1*e2 + w2*e3;
      e4=w1*e3 + w2*e4;
      e5=w1*e4 + w2*e5;
      e6=w1*e5 + w2*e6;
      //----
      t3_fosc=c1*e6+c2*e5+c3*e4+c4*e3;
      IndBuffer[bar]=forecastosc;
      SigBuffer[bar]=t3_fosc;
      BuyBuffer [bar]=EMPTY_VALUE;
      SellBuffer[bar]=EMPTY_VALUE;
      //----
      if(IndBuffer[bar-1] > SigBuffer[bar-2] && IndBuffer[bar-2]<=SigBuffer[bar-3] && SigBuffer[bar-1]<0) BuyBuffer [bar-1]=t3_fosc-0.05;
      if(IndBuffer[bar-1] < SigBuffer[bar-2] && IndBuffer[bar-2]>=SigBuffer[bar-3] && SigBuffer[bar-1]>0) SellBuffer[bar-1]=t3_fosc+0.05;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+   
//| Получение значения ценовой таймсерии                             |
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
