//+------------------------------------------------------------------+
//|                                                  StepMA_NRTR.mq5 |
//|                                Copyright © 2006, TrendLaboratory |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                   E-mail: igorad2003@yahoo.co.uk |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2006, TrendLaboratory"
//---- ссылка на сайт автора
#property link "http://www.forex-instruments.info"
#property link "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- номер версии индикатора
#property version   "8.00"
//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- для расчета и отрисовки индикатора использовано 4 буфера
#property indicator_buffers 4
//---- использовано 4 графических построения
#property indicator_plots   4
//+----------------------------------------------+
//|  Параметры отрисовки линии индикатора        |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета бычей линии индикатора использован цвет BlueViolet
#property indicator_color1  clrBlueViolet
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 2
#property indicator_width1  2
//---- отображение метки бычьей линии индикатора
#property indicator_label1  "Upper StepMA"
//---- отрисовка индикатора 2 в виде линии
//+----------------------------------------------+
//|  Параметры отрисовки линии индикатора        |
//+----------------------------------------------+
#property indicator_type2   DRAW_LINE
//---- в качестве цвета медвежей линии индикатора использован цвет Gold
#property indicator_color2  clrGold
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение метки медвежьей линии индикатора
#property indicator_label2  "Lower StepMA"
//+----------------------------------------------+
//|  Параметры отрисовки значка индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде значка
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета бычей линии индикатора использован цвет SpringGreen
#property indicator_color3  clrSpringGreen
//---- линия индикатора 3 - непрерывная кривая
#property indicator_style3  STYLE_SOLID
//---- толщина линии индикатора 3 равна 4
#property indicator_width3  4
//---- отображение метки индикатора
#property indicator_label3  "StepMA Buy"
//+----------------------------------------------+
//|  Параметры отрисовки значка индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде значка
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета медвежей линии индикатора использован цвет Red
#property indicator_color4  clrRed
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style4  STYLE_SOLID
//---- толщина линии индикатора 4 равна 4
#property indicator_width4  4
//---- отображение метки индикатора
#property indicator_label4  "StepMA Sell"
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
enum MA_MODE // Тип константы
  {
   SMA,     // SMA
   LWMA     // LWMA
  };
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
enum PRICE_MODE // Тип константы
  {
   HighLow,     // High/Low
   CloseClose   // Close/Close
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int        Length      = 10;      // Volty Length
input double     Kv          = 1.0;     // Sensivity Factor
input int        StepSize    = 0;       // Constant Step Size (if need)
input double     Percentage  = 0;       // Percentage of Up/Down Moving   
input PRICE_MODE Switch      = HighLow; // High/Low Mode Switch (more sensitive)    
input int        Shift=0; // сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double UpBuffer[];
double DnBuffer[];
double SellBuffer[];
double BuyBuffer[];

double ratio;
int trend1,trend1_,trend0;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+ 
//| StepSize Calculation                                             |
//+------------------------------------------------------------------+ 
double StepSizeCalc(const double &High[],const double &Low[],int Len,double Km,int Size,int bar)
  {
//----
   double result;

   if(!Size)
     {
      double Range=0.0;
      double ATRmax=-1000000;
      double ATRmin=+1000000;

      for(int iii=Len-1; iii>=0; iii--)
        {
         Range=High[bar+iii]-Low[bar+iii];
         if(Range>ATRmax) ATRmax=Range;
         if(Range<ATRmin) ATRmin=Range;
        }
      result=MathRound(0.5*Km*(ATRmax+ATRmin)/_Point);
     }
   else result=Km*Size;
//----
   return(result);
  }
//+------------------------------------------------------------------+
//| StepMA Calculation                                               |
//+------------------------------------------------------------------+ 
double StepMACalc(const double &High[],const double &Low[],const double &Close[],bool HL,double Size,int bar)
  {
//----
   double result,smax0,smin0,SizeP,Size2P;
   static double smax1,smin1;
   static bool FirstStart=true;
   SizeP=Size*_Point;
   Size2P=2.0*SizeP;

//---- стартовая инициализация переменных
   if(FirstStart)
     {
      trend1=0;
      smax1=Low[bar]+Size2P;
      smin1=High[bar]-Size2P;
      FirstStart=false;
     }

   if(HL)
     {
      smax0=Low[bar]+Size2P;
      smin0=High[bar]-Size2P;
     }
   else
     {
      smax0=Close[bar]+Size2P;
      smin0=Close[bar]-Size2P;
     }

   trend0=trend1;

   if(Close[bar]>smax1) trend0=+1;
   if(Close[bar]<smin1) trend0=-1;

   if(trend0>0)
     {
      if(smin0<smin1) smin0=smin1;
      result=smin0+SizeP;
     }
   else
     {
      if(smax0>smax1) smax0=smax1;
      result=smax0-SizeP;
     }
   trend1_=trend1;

   if(bar)
     {
      smax1=smax0;
      smin1=smin0;
      trend1=trend0;
     }
//----
   return(result);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=Length+3;

//---- инициализация переменных  
   ratio=Percentage/100.0*_Point;

//---- превращение динамического массива BufferUp в индикаторный буфер
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(UpBuffer,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива BufferDown в индикаторный буфер
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(DnBuffer,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива BufferUp1 в индикаторный буфер
   SetIndexBuffer(2,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(BuyBuffer,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- символ для индикатора
   PlotIndexSetInteger(2,PLOT_ARROW,108);

//---- превращение динамического массива BufferDown1 в индикаторный буфер
   SetIndexBuffer(3,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- индексация элементов в буферах как в таймсериях   
   ArraySetAsSeries(SellBuffer,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- символ для индикатора
   PlotIndexSetInteger(3,PLOT_ARROW,108);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"StepMA NRTR (",Length,", ",Kv,", ",StepSize,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
                const double& low[],      // ценовой массив минимумов цены  для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);

//---- объявления локальных переменных 
   int limit,bar;
   double StepMA,Step;

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total;   // стартовый номер для расчета всех баров
      trend1_=0;
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0; bar--)
     {
      UpBuffer[bar]=EMPTY_VALUE;
      DnBuffer[bar]=EMPTY_VALUE;
      SellBuffer[bar]=EMPTY_VALUE;
      BuyBuffer[bar]=EMPTY_VALUE;

      Step=StepSizeCalc(high,low,Length,Kv,StepSize,bar);
      if(!Step) Step=1;

      StepMA=StepMACalc(high,low,close,Switch,Step,bar)+ratio/Step;

      if(trend0>0)
        {
         UpBuffer[bar]=StepMA-Step*_Point;
         if(trend1_<0) BuyBuffer[bar]=UpBuffer[bar];
         DnBuffer[bar]=EMPTY_VALUE;
        }

      if(trend0<0)
        {
         DnBuffer[bar]=StepMA+Step*_Point;
         if(trend1_>0) SellBuffer[bar]=DnBuffer[bar];
         UpBuffer[bar]=EMPTY_VALUE;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
