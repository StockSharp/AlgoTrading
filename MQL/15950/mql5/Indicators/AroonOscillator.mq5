//+------------------------------------------------------------------+
//|                                              AroonOscillator.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2011, Nikolay Kositsin"
//---- ссылка на сайт автора
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 1
#property indicator_buffers 1 
//---- использовано всего одно графические построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде трехцветной линии
#property indicator_type1 DRAW_LINE
//---- в качестве цвета линии использован красный цвет
#property indicator_color1 clrRed
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки сигнальной линии
#property indicator_label1  "AroonOscillator"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 +50
#property indicator_level2   0
#property indicator_level3 -50
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int AroonPeriod= 9; // период индикатора 
input int AroonShift = 0; // сдвиг индикатора по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double ExtLineBuffer[];
//+------------------------------------------------------------------+
//|  searching index of the highest bar                              |
//+------------------------------------------------------------------+
int iHighest(
             const double &array[],// массив для поиска индекса максимального элемента
             int count,// число элементов массива (в направлении от текущего бара в сторону убывания индекса), 
             // среди которых должен быть произведен поиск.
             int startPos //индекс (смещение относительно текущего бара) начального бара, 
             // с которого начинается поиск наибольшего значения
             )
  {
//----
   int index=startPos;

//---- проверка стартового индекса на корректность
   if(startPos<0)
     {
      Print("Неверное значение в функции iHighest, startPos = ",startPos);
      return(0);
     }

//---- проверка значения startPos на корректность
   if(startPos-count<0)
      count=startPos;

   double max=array[startPos];

//---- поиск индекса
   for(int i=startPos; i>startPos-count; i--)
     {
      if(array[i]>max)
        {
         index=i;
         max=array[i];
        }
     }
//---- возврат индекса наибольшего бара
   return(index);
  }
//+------------------------------------------------------------------+
//|  searching index of the lowest bar                               |
//+------------------------------------------------------------------+
int iLowest(
            const double &array[],// массив для поиска индекса минимального элемента
            int count,// число элементов массива (в направлении от текущего бара в сторону убывания индекса), 
            // среди которых должен быть произведен поиск.
            int startPos //индекс (смещение относительно текущего бара) начального бара, 
            // с которого начинается поиск наименьшего значения
            )
  {
//----
   int index=startPos;

//---- проверка стартового индекса на корректность
   if(startPos<0)
     {
      Print("Неверное значение в функции iLowest, startPos = ",startPos);
      return(0);
     }

//---- проверка значения startPos на корректность
   if(startPos-count<0)
      count=startPos;

   double min=array[startPos];

//---- поиск индекса
   for(int i=startPos; i>startPos-count; i--)
     {
      if(array[i]<min)
        {
         index=i;
         min=array[i];
        }
     }
//---- возврат индекса наименьшего бара
   return(index);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"AroonOscillator(",AroonPeriod,")");
//---- осуществление сдвига индикатора 1 по горизонтали на AroonShift
   PlotIndexSetInteger(0,PLOT_SHIFT,AroonShift);
//---- создание метки для отображения в Окне данных
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<AroonPeriod) return(0);

//---- объявления локальных переменных 
   int first,bar,highest,lowest;

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=AroonPeriod-1; // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров

//---- основной цикл расчета индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      highest=iHighest(high,AroonPeriod,bar);
      lowest=iLowest(low,AroonPeriod,bar);
      //----
      ExtLineBuffer[bar]=100*(highest-lowest)/AroonPeriod;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
