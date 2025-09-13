//+------------------------------------------------------------------+
//|                                                Arrows&Curves.mq5 |
//|          Copyright © 2007, Лукашук Виктор Геннадьевич aka lukas1 |
//|                                                    lukas1@ngs.ru |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2007, Лукашук Виктор Геннадьевич aka lukas1"
//---- ссылка на сайт автора
#property link      "lukas1@ngs.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано восемь буферов
#property indicator_buffers 8
//---- использовано всего восемь графических построений
#property indicator_plots   8
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован розовый цвет
#property indicator_color1  Magenta
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение метки медвежьей линии индикатора
#property indicator_label1  "Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычей линии индикатора использован зеленый цвет
#property indicator_color2  Lime
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение метки бычьей линии индикатора
#property indicator_label2 "Buy"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде символа
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован розовый цвет
#property indicator_color3  Magenta
//---- толщина линии индикатора 3 равна 4
#property indicator_width3  4
//---- отображение метки медвежьей линии индикатора
#property indicator_label3  "SellStop"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде символа
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета бычей линии индикатора использован зеленый цвет
#property indicator_color4  Lime
//---- толщина линии индикатора 4 равна 4
#property indicator_width4  4
//---- отображение метки бычьей линии индикатора
#property indicator_label4 "BuyStop"
//+--------------------------------------------+
//|  Параметры отрисовки  уровней индикатора   |
//+--------------------------------------------+
//---- отрисовка уровней  в виде линий
#property indicator_type5   DRAW_LINE
#property indicator_type6   DRAW_LINE
#property indicator_type7   DRAW_LINE
#property indicator_type8   DRAW_LINE
//---- в качестве цветов уровней четыре цвета
#property indicator_color5  Orange
#property indicator_color6  MediumSeaGreen
#property indicator_color7  MediumSeaGreen
#property indicator_color8  Orange
//---- уровни Боллинджера - штрихпунктирные кривые
#property indicator_style5 STYLE_DASHDOTDOT
#property indicator_style6 STYLE_DASHDOTDOT
#property indicator_style7 STYLE_DASHDOTDOT
#property indicator_style8 STYLE_DASHDOTDOT
//---- толщина уровней Боллинджера равна 1
#property indicator_width5  1
#property indicator_width6  1
#property indicator_width7  1
#property indicator_width8  1
//---- отображение меток уровней Боллинджера
#property indicator_label5  "BUY from here"
#property indicator_label6  "BuyStop"
#property indicator_label7  "SellStop"
#property indicator_label8  "SELL from here"

//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int SSP     = 20;   //период линейного разворота индикатора
input int Channel = 0;    //уменьшение диапазона канала. Д.б. в диапазоне 0-50
input int Ch_Stop = 30;   //уменьшение стопового канала (суммируется с основным)
input int relay   = 10;   //смещение линий вглубь истории на 4 бара 
//+----------------------------------------------+

//---- объявление динамических массивов, которые будут
//---- в дальнейшем использованы в качестве индикаторных буферов
double BuyBuffer[];
double SellBuffer[];
double HBuffer[];
double LBuffer[];
double HSBuffer[];
double LSBuffer[];
double BuyStopBuffer[],SellStopBuffer[];
//---
int StartBars;
bool uptrend_,old_,uptrend2_,old2_;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация глобальных переменных 
   StartBars=SSP+1+relay;

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Sell");
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Buy");
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,SellStopBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"SellStop");
//---- символ для индикатора
   PlotIndexSetInteger(2,PLOT_ARROW,251);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellStopBuffer,true);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,BuyStopBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"BuyStop");
//---- символ для индикатора
   PlotIndexSetInteger(3,PLOT_ARROW,251);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyStopBuffer,true);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(4,HBuffer,INDICATOR_DATA);
   SetIndexBuffer(5,HSBuffer,INDICATOR_DATA);
   SetIndexBuffer(6,LSBuffer,INDICATOR_DATA);
   SetIndexBuffer(7,LBuffer,INDICATOR_DATA);
//---- установка позиции, с которой начинается отрисовка уровней Боллинджера
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,StartBars);
   PlotIndexSetInteger(5,PLOT_DRAW_BEGIN,StartBars);
   PlotIndexSetInteger(6,PLOT_DRAW_BEGIN,StartBars);
   PlotIndexSetInteger(7,PLOT_DRAW_BEGIN,StartBars);
//---- создание меток для отображения в Окне данных
   PlotIndexSetString(4,PLOT_LABEL,"BUY from here");
   PlotIndexSetString(5,PLOT_LABEL,"BuyStop");
   PlotIndexSetString(6,PLOT_LABEL,"SellStop");
   PlotIndexSetString(7,PLOT_LABEL,"SELL from here");
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,0);
   PlotIndexSetDouble(5,PLOT_EMPTY_VALUE,0);
   PlotIndexSetDouble(6,PLOT_EMPTY_VALUE,0);
   PlotIndexSetDouble(7,PLOT_EMPTY_VALUE,0);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(HBuffer,true);
   ArraySetAsSeries(HSBuffer,true);
   ArraySetAsSeries(LSBuffer,true);
   ArraySetAsSeries(LBuffer,true);

//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и лэйба для субъокон 
   string short_name="Arrows&Curves";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
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
   if(rates_total<StartBars) return(0);

//---- объявления локальных переменных 
   int limit,bar;
   double High,Low,smin,smax,smin2,smax2,Close;
   static bool uptrend,old,uptrend2,old2;

//---- расчеты необходимого количества копируемых данных и
//стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-StartBars; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- восстанавливаем значения переменных
   uptrend=uptrend_;
   uptrend2=uptrend2_;
   old=old_;
   old2=old2_;

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0; bar--)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0)
        {
         uptrend_=uptrend;
         uptrend2_=uptrend2;
         old_=old;
         old2_=old2;
        }

      Close= close[bar];
      High = high[iHighest(high,SSP,bar+relay)];
      Low  = low [iLowest (low, SSP,bar+relay)];
      smax = High -(Low-High)*Channel/ 100;           // smax ниже High с учетом коэфф.Channel
      smin = Low+(High-Low)*Channel / 100;            // smin выше Low с учетом коэфф.Channel
      smax2= High -(High-Low)*(Channel+Ch_Stop)/ 100; // smax ниже High с учетом коэфф.Chan+Ch_Stop
      smin2= Low+(High-Low)*(Channel+Ch_Stop) / 100;  // smin выше Low с учетом коэфф.Channel
      BuyBuffer[bar]=0;
      SellBuffer[bar]=0;
      BuyStopBuffer[bar]=0;
      SellStopBuffer[bar]=0;
      //----
      if(Close<smin && Close<smax && uptrend2==true && bar!=0) uptrend=false;
      if( Close > smax  && Close > smin   && uptrend2 == false && bar!=0 ) uptrend  = true;
      if((Close > smax2 || Close > smin2) && uptrend  == false && bar!=0 ) uptrend2 = false;
      if((Close<smin2 || Close<smax2) && uptrend==true && bar!=0) uptrend2=true;
      //---- повторный сигнал не переключает режимы "uptrend"
      //---- но используется сигнал по крестикам
      if(close[bar]<smin && close[bar]<smax && uptrend2==false && bar!=0)
        {
         SellBuffer[bar]=Low;
         uptrend2=true;
        }
      //---- повторный сигнал не переключает режимы "uptrend"
      //---- но используется сигнал по крестикам
      if(Close>smax && Close>smin && uptrend2==true && bar!=0)
        {
         BuyBuffer[bar]=High;
         uptrend2=false;
        }
      //----
      if(uptrend != old && uptrend == false) SellBuffer[bar] = Low;
      if(uptrend != old && uptrend == true ) BuyBuffer[bar] = High;
      //----
      if(uptrend2 != old2 && uptrend2 == true ) BuyStopBuffer[bar] = smax2;
      if(uptrend2 != old2 && uptrend2 == false) SellStopBuffer[bar] = smin2;
      //----
      old=uptrend;
      old2=uptrend2;
      //----
      HBuffer[bar]=smax;
      LBuffer[bar]=smin;
      HSBuffer[bar]=smax2;
      LSBuffer[bar]=smin2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//|  searching index of the highest bar                              |
//+------------------------------------------------------------------+
int iHighest(const double &array[],// массив для поиска индекса максимального элемента
             int count,            // число элементов массива (в направлении от текущего бара в сторону убывания индекса), 
                                   // среди которых должен быть произведен поиск.
             int startPos          // индекс (смещение относительно текущего бара) начального бара, 
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

   double max=array[startPos];

//---- поиск индекса
   for(int i=startPos; i<startPos+count; i++)
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
int iLowest(const double &array[],  // массив для поиска индекса минимального элемента
            int count,              // число элементов массива (в направлении от текущего бара в сторону убывания индекса), 
                                    // среди которых должен быть произведен поиск.
            int startPos            // индекс (смещение относительно текущего бара) начального бара, 
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

   double min=array[startPos];

//---- поиск индекса
   for(int i=startPos; i<startPos+count; i++)
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