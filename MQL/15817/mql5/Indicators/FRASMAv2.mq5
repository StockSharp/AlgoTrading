//+------------------------------------------------------------------+
//|                                                     FRASMAv2.mq5 | 
//|                              Copyright © 2008, jppoton@yahoo.com | 
//|                              http://fractalfinance.blogspot.com/ | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2008, jppoton@yahoo.com"
#property link "http://fractalfinance.blogspot.com/"
//---- номер версии индикатора
#property version   "1.02"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде многоцветной линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цветов трехцветной линии использованы
#property indicator_color1  clrSpringGreen,clrGray,clrMagenta
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "FRASMAv2"
//+-----------------------------------+
//|  объявление перечислений          |
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
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input  uint e_period=30;
input uint normal_speed=20;
input Applied_price_ IPC=PRICE_CLOSE_;//Ценовая константа
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // Сдвиг индикатора по вертикали в пунктах
//+-----------------------------------+

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[];
double ColorIndBuffer[];

//---- Объявление переменной значения вертикального сдвига мувинга
double dPriceShift;
//---- Объявление целых переменных начала отсчета данных
int min_rates_total,g_period_minus_1;
//---- объявление глобальных переменных
int Count[],MAXSIZE;
double Data[],LOG_2;
//+------------------------------------------------------------------+
//|  Пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// Возврат по ссылке номера текущего значения ценового ряда
                          int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=Size;
   Max1=Max2-1;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчета данных
   g_period_minus_1=int(e_period)-1;
   min_rates_total=int(e_period)+g_period_minus_1;
   LOG_2=MathLog(2.0);
   MAXSIZE=10000;

//---- Инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;

//---- распределение памяти под массивы переменных  
   ArrayResize(Count,MAXSIZE);
   ArrayResize(Data,MAXSIZE);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorIndBuffer,true);

//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"FRASMAv2(",e_period,", ",normal_speed,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| Custom indicator function                                        | 
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
//---- Объявление целых переменных и получение уже посчитанных баров
   int limit,bar;

//--- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-1; // стартовый номер для расчета всех баров
      ArrayInitialize(Count,0);
      ArrayInitialize(Data,0.0);
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }

//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- Основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- Вызов функции PriceSeries для получения входной цены price
      Data[Count[0]]=PriceSeries(IPC,bar,open,low,high,close);
      //----
      double priceMax=0.0;
      for(int iii=0; iii<int(e_period); iii++) if(Data[Count[iii]]>priceMax) priceMax=Data[Count[iii]];
      double priceMin=99999999.0;
      for(int iii=0; iii<int(e_period); iii++) if(Data[Count[iii]]<priceMin) priceMin=Data[Count[iii]];
      double range=priceMax-priceMin;
      //----
      double length=0.0;
      double fdi,priorDiff=0.0;
      //----
      for(int kkk=0; kkk<=g_period_minus_1; kkk++)
        {
         if(range>0.0)
           {
            double diff=(Data[Count[kkk]]-priceMin)/range;
            if(kkk>0) length+=MathSqrt(MathPow(diff-priorDiff,2.0)+(1.0/MathPow(e_period,2.0)));
            priorDiff=diff;
           }
        }
      if(length>0.0)
        {
         fdi=1.0+(MathLog(length)+LOG_2)/MathLog(2*g_period_minus_1);
        }
      else
        {
/*
         ** The FDI algorithm suggests in this case a zero value.
         ** I prefer to use the previous FDI value.
         */
         fdi=0.0;
        }
      double res=2-fdi;
      if(!res) res=2;
      double trail_dim=1/res; // This is the trail dimension, the inverse of the Hurst-Holder exponent 
      double alpha=trail_dim/2;
      int speed=int(MathMin(MathMax(MathRound(normal_speed*alpha),1),MAXSIZE));
      double sum=0.0;
      
      for(int iii=0; iii<speed; iii++) sum+=Data[Count[iii]];
      IndBuffer[bar]=sum/speed;
      IndBuffer[bar]+=dPriceShift;
      
      
      if(bar) Recount_ArrayZeroPos(Count,MAXSIZE);
     }

//---- корректировка значения переменной first
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      limit--; // стартовый номер для расчета всех баров

//---- Основной цикл раскраски сигнальной линии
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ColorIndBuffer[bar]=1;
      if(IndBuffer[bar+1]<IndBuffer[bar]) ColorIndBuffer[bar]=0;
      if(IndBuffer[bar+1]>IndBuffer[bar]) ColorIndBuffer[bar]=2;
     }
//----     
   return(rates_total);
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
