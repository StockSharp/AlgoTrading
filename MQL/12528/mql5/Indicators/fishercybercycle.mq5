//+------------------------------------------------------------------+
//|                                             FisherCyberCycle.mq5 |
//|                                                                  |
//| Fisher Cyber Cycle                                               |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Coded by Witold Wozniak"
//--- авторство индикатора
#property link      "www.mqlsoft.com"
//--- номер версии индикатора
#property version   "1.10"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки индикатора Cyber Cycle   |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//--- в качестве цвета бычей линии индикатора использован цвет Red
#property indicator_color1  Red
//--- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//--- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//--- отображение метки линии индикатора
#property indicator_label1  "Fisher Cyber Cycle"
//+----------------------------------------------+
//| Параметры отрисовки индикатора Trigger       |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//--- в качестве цвета линии индикатора использован цвет Blue
#property indicator_color2  Blue
//--- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//--- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//--- отображение метки линии индикатора
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 +0.7
#property indicator_level2  0.0
#property indicator_level3 -0.7
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input double Alpha=0.07; // Коэффициент индикатора 
input int Length=8;      // Период индикатора 
input int Shift=0;       // Сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double FishCCBuffer[];
double TriggerBuffer[];
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//--- объявление глобальных переменных
int Count1[],Count2[];
double K0,K1,K2,K3,Smooth[],Cycle[],Value1[],Price[];
//+------------------------------------------------------------------+
//|  пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos1(int &CoArr[])// Возврат по ссылке номера текущего значения ценового ряда
  {
//---
   int numb,Max1,Max2;
   static int count=1;

   Max1=Length+1;
   Max2=Length+2;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//---
  }
//+------------------------------------------------------------------+
//|  Пересчет позиции самого нового элемента в массиве               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos2(int &CoArr[])// Возврат по ссылке номера текущего значения ценового ряда
  {
//---
   int numb,Max1,Max2;
   static int count=1;

   Max1=Length+2;
   Max2=Length+3;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчета данных
   min_rates_total=Length+3;

//--- инициализация переменных
   K0=MathPow((1.0 - 0.5*Alpha),2);
   K1=2.0;
   K2=2.0 *(1.0 - Alpha);
   K3=MathPow((1.0 - Alpha),2);

//--- распределение памяти под массивы переменных  
   ArrayResize(Count1,Length+2);
   ArrayResize(Cycle,Length+2);
   ArrayResize(Count2,Length+3);
   ArrayResize(Value1,Length+3);
   ArrayResize(Price,Length+3);
   ArrayResize(Smooth,Length+3);

//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,FishCCBuffer,INDICATOR_DATA);
//--- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//--- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчета отрисовки индикатора 2 на min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);

//--- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Fisher Cyber Cycle(",DoubleToString(Alpha,4),", ",Length,", ",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
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
//--- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);

//--- объявления локальных переменных 
   int first,bar;
   double hh,ll,tmp;

//--- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=3; // стартовый номер для расчета всех баров
      for(int numb=0; numb<Length+2; numb++) Count1[numb]=numb;
      for(int numb=0; numb<Length+3; numb++) Count2[numb]=numb;
      double val=(high[first]+low[first])/2.0;
      ArrayInitialize(Price,val);
      ArrayInitialize(Smooth,val);
      ArrayInitialize(Cycle,val);
      ArrayInitialize(Value1,val);
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров

//--- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      Price[Count2[0]]=(high[bar]+low[bar])/2.0;
      Smooth[Count2[0]]=(Price[Count2[0]]+2.0*Price[Count2[1]]+2.0*Price[Count2[2]]+Price[Count2[3]])/6.0;

      if(bar<3)
        {
         Recount_ArrayZeroPos1(Count1);
         Recount_ArrayZeroPos2(Count2);
         continue;
        }

      if(bar<min_rates_total) Cycle[Count1[0]]=(Price[Count1[0]]+2.0*Price[Count2[1]]+Price[Count2[2]])/4.0;
      else Cycle[Count1[0]]=K0*(Smooth[Count2[0]]-K1*Smooth[Count2[1]]+Smooth[Count2[2]])+K2*Cycle[Count1[1]]-K3*Cycle[Count1[2]];

      hh = Cycle[Count1[0]];
      ll = Cycle[Count1[0]];

      for(int iii=0; iii<Length; iii++)
        {
         tmp= Cycle[Count1[iii]];
         hh = MathMax(hh, tmp);
         ll = MathMin(ll, tmp);
        }

      if(hh!=ll) Value1[Count2[0]]=(Cycle[Count1[0]]-ll)/(hh-ll);
      else Value1[Count2[0]]=0.0;

      FishCCBuffer[bar]=(4.0*Value1[Count2[0]]+3.0*Value1[Count2[1]]+2.0*Value1[Count2[2]]+Value1[Count2[3]])/10.0;
      FishCCBuffer[bar] = 0.5 * MathLog((1.0 + 1.98 * (FishCCBuffer[bar] - 0.5)) / (1.0 - 1.98 * (FishCCBuffer[bar] - 0.5)));
      TriggerBuffer[bar]= FishCCBuffer[bar-1];

      if(bar<rates_total-1)
        {
         Recount_ArrayZeroPos1(Count1);
         Recount_ArrayZeroPos2(Count2);
        }

     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+

   