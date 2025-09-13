//+------------------------------------------------------------------+ 
//|                                                          VHF.mq5 | 
//|                             Copyright © 2010,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов
#property indicator_buffers 1 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован цвет BlueViolet
#property indicator_color1 BlueViolet
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки линии индикатора
#property indicator_label1  "VHF"
//+-----------------------------------+
//|  Входные параметры индикатора     |
//+-----------------------------------+
input int N=28;  // Период индикатора
//+-----------------------------------+

//---- объявление динамического массива, который в дальнейшем
//---- будет использован в качестве индикаторного буфера
double ExtLineBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве кольцевых буферов
int Count[];
double Temp[];
//+------------------------------------------------------------------+
//|  Пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// возврат по ссылке номера текущего значения ценового ряда
                          int Size)    // количество элементов в кольцевом буфере
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
//| VHF indicator initialization function                            | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=N+1;

//---- распределение памяти под массивы переменных  
   ArrayResize(Count,N);
   ArrayResize(Temp,N);

//---- инициализация массивов переменных
   ArrayInitialize(Count,0);
   ArrayInitialize(Temp,0.0);

//---- превращение динамического массива ExtLineBuffer[] в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtLineBuffer,true);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"VHF( N = ",N,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| JJRSX iteration function                                         | 
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
   double hh,ll,a,b,res;
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int limit,bar,maxbar;

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit=rates_total-2;                 // расчетное количество всех баров
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

   maxbar=rates_total-min_rates_total-1;

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0; bar--)
     {
      Temp[Count[0]]=MathAbs(close[bar]-close[bar+1]);

      if(bar<maxbar)
        {
         hh=high[ArrayMaximum(high,bar,N)];
         ll=low [ArrayMinimum(low, bar,N)];
         a = hh-ll;

         b=0.0;
         for(int kkk=0; kkk<N; kkk++) b+=Temp[Count[kkk]];

         if(b) res=a/b;
         else  res=0.0;
        }
      else res=EMPTY_VALUE;

      ExtLineBuffer[bar]=res;

      //---- пересчет позиций элементов в кольцевом буфере Temp[]
      if(bar>0) Recount_ArrayZeroPos(Count,N);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
