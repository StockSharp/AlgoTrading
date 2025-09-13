//+------------------------------------------------------------------+ 
//|                                                       DecEMA.mq5 | 
//|                                         Developed by Coders Guru |
//|                                            http://www.xpworx.com |                      
//|                         Revised by IgorAD,igorad2003@yahoo.co.uk |   
//|                                        http://www.forex-tsd.com/ |                                      
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Guru"
#property link "farria@mail.redcom.ru" 
//---- номер версии индикатора
#property version   "1.01"
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
//---- в качестве цвета линии индикатора использован розовый цвет
#property indicator_color1 clrRed
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "DecEMA"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1;
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
input uint EMA_Period=3; // Период EMA 
input int ELength=15;    // Глубина сглаживания                   
input Applied_price_ IPC=PRICE_CLOSE; // Ценовая константа
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // Сдвиг индикатора по вертикали в пунктах
//+-----------------------------------+
//---- индикаторный буфер
double IndBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление глобальных переменных
double alfa,dPriceShift;
//+------------------------------------------------------------------+    
//| DecEMA indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных 
   min_rates_total=2;
   alfa=2.0/(1.0+ELength);
//---- инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"DecEMA(",EMA_Period,",",ELength,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| DecEMA iteration function                                        | 
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
   if(rates_total<min_rates_total)return(0);
//---- объявление локальных переменных
   int first,bar,maxbar;
//----
   double price,EMA0,EMA1,EMA2,EMA3,EMA4,EMA5,EMA6,EMA7,EMA8,EMA9,EMA10;
   static double sdEMA1,sdEMA2,sdEMA3,sdEMA4,sdEMA5,sdEMA6,sdEMA7,sdEMA8,sdEMA9,sdEMA10;
//----
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=1; // стартовый номер для расчета всех баров
      //---- инициализация коэффициентов
      sdEMA1=PriceSeries(IPC,first-1,open,low,high,close);
      sdEMA2=sdEMA1;
      sdEMA3=sdEMA1;
      sdEMA4=sdEMA1;
      sdEMA5=sdEMA1;
      sdEMA6=sdEMA1;
      sdEMA7=sdEMA1;
      sdEMA8=sdEMA1;
      sdEMA9=sdEMA1;
      sdEMA10=sdEMA1;
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- восстановление переменных
   EMA1=sdEMA1;
   EMA2=sdEMA2;
   EMA3=sdEMA3;
   EMA4=sdEMA4;
   EMA5=sdEMA5;
   EMA6=sdEMA6;
   EMA7=sdEMA7;
   EMA8=sdEMA8;
   EMA9=sdEMA9;
   EMA10=sdEMA10;
   maxbar=rates_total-1;
//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      EMA0=XMA1.XMASeries(1,prev_calculated,rates_total,1,0,EMA_Period,price,bar,false);
      EMA1=alfa*EMA0 + (1-alfa)*EMA1;
      EMA2=alfa*EMA1 + (1-alfa)*EMA2;
      EMA3=alfa*EMA2 + (1-alfa)*EMA3;
      EMA4=alfa*EMA3 + (1-alfa)*EMA4;
      EMA5=alfa*EMA4 + (1-alfa)*EMA5;
      EMA6=alfa*EMA5 + (1-alfa)*EMA6;
      EMA7=alfa*EMA6 + (1-alfa)*EMA7;
      EMA8=alfa*EMA7 + (1-alfa)*EMA8;
      EMA9=alfa*EMA8 + (1-alfa)*EMA9;
      EMA10=alfa*EMA9+(1-alfa)*EMA10;
      IndBuffer[bar]=10*EMA1-45*EMA2+120*EMA3-210*EMA4+252*EMA5-210*EMA6+120*EMA7-45*EMA8+10*EMA9-EMA10;
      IndBuffer[bar]+=dPriceShift;
      //---- сохранение переменных 
      if(bar<maxbar)
        {
         sdEMA1=EMA1;
         sdEMA2=EMA2;
         sdEMA3=EMA3;
         sdEMA4=EMA4;
         sdEMA5=EMA5;
         sdEMA6=EMA6;
         sdEMA7=EMA7;
         sdEMA8=EMA8;
         sdEMA9=EMA9;
         sdEMA10=EMA10;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
