//+---------------------------------------------------------------------+
//|                                                       MaChannel.mq5 | 
//|                                     Copyright © 2012, Ivan Kornilov | 
//|                                                    excelf@gmail.com | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2012, Ivan Kornilov"
#property link "excelf@gmail.com"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- количество индикаторных буферов 4
#property indicator_buffers 4 
//---- использовано всего четыре графических построения
#property indicator_plots   4
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в виде значка
#property indicator_type1 DRAW_ARROW
//---- в качестве окраски индикатора использован
#property indicator_color1 clrBlue
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1 1
//---- отображение метки сигнальной линии
#property indicator_label1  "MaChannel Up"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в виде значка
#property indicator_type2 DRAW_ARROW
//---- в качестве окраски индикатора использован
#property indicator_color2 clrRed
//---- линия индикатора - сплошная
#property indicator_style2 STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width2 1
//---- отображение метки сигнальной линии
#property indicator_label2  "MaChannel Down"
//+----------------------------------------------+
//|  Параметры отрисовки бычьего индикатора      |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде значка
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета бычей линии индикатора использован
#property indicator_color3  clrDodgerBlue
//---- линия индикатора 3 - непрерывная кривая
#property indicator_style3  STYLE_SOLID
//---- толщина линии индикатора 3 равна 1
#property indicator_width3  1
//---- отображение бычьей метки индикатора
#property indicator_label3  "Buy MaChannel signal"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде значка
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован
#property indicator_color4  clrMagenta
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style4  STYLE_SOLID
//---- толщина линии индикатора 2 равна 1
#property indicator_width4  1
//---- отображение медвежьей метки индикатора
#property indicator_label4  "Sell MaChannel signal"
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
/*enum SmoothMethod - перечисление объявлено в файле SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_SMA_;  //метод усреднения
input uint XLength=12;                     //глубина сглаживания                    
input int XPhase=15;                       //параметр сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input uint Renge=600;                      //Ценовой размах мувинга в пунктах
input bool oneWay= true;                   //одностороннее движение индикатора
input int Shift=0;                         //сдвиг индикатора по горизонтали
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double TrendUp[],TrendDown[];
double SignUp[];
double SignDown[];
//---- Объявление переменной значения вертикального сдвига мувинга
double dRenge;
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+   
//| MaChannel indicator initialization function                      | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=GetStartBars(XMA_Method,XLength,XPhase)+2;
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("XLength",XLength);
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);
//---- Инициализация сдвига по вертикали
   dRenge=_Point*Renge;
   
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,TrendUp,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,119);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,TrendDown,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,119);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,SignUp,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//---- символ для индикатора
   PlotIndexSetInteger(2,PLOT_ARROW,117);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,SignDown,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0.0);
//---- символ для индикатора
   PlotIndexSetInteger(3,PLOT_ARROW,117);

//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"MaChannel");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| MaChannel iteration function                                     | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- Объявление переменных с плавающей точкой  
   double up,down;
   static double up_prev,down_prev;
//---- Объявление целых переменных и получение уже посчитанных баров
   int first,bar,trend;
   static int trend_prev;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
   {
      first=0; // стартовый номер для расчёта всех баров
      trend_prev=0;
    }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- Основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      TrendUp[bar]=0.0;
      TrendDown[bar]=0.0;
      SignUp[bar]=0.0;
      SignDown[bar]=0.0;
      trend=trend_prev;
      up=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,high[bar],bar,false)+dRenge;
      down=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,low[bar],bar,false)+dRenge;

      if(oneWay)
        {
         if(trend_prev==+1)
           {
            if(down<down_prev && down_prev) down=down_prev;
           }
         else if(trend_prev==-1)
           {
            if(up>up_prev && up_prev) up=up_prev;
           }
        }

      if(high[bar]>up)
        {
         trend=+1;
        }
      else if(low[bar]<down)
        {
         trend=-1;
        }

      if(trend==-1.0)
        {
         TrendDown[bar]=up;
        }
      else if(trend==+1.0)
        {
         TrendUp[bar]=down;
        }
        
      if(trend_prev<=0 && trend>0) SignUp[bar]=TrendUp[bar];
      if(trend_prev>=0 && trend<0) SignDown[bar]=TrendDown[bar];

       if(bar<rates_total-1)
         {
          up_prev=up;
          down_prev=down;
          trend_prev=trend;         
         }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
