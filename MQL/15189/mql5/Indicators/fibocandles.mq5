//+------------------------------------------------------------------+
//|                                                  FiboCandles.mq5 |
//|                                  Copyright © 2010, Ivan Kornilov |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Ivan Kornilov"
#property link "excelf@gmail.com"
#property description "Fibo Candles 2"
//---- номер версии индикатора
#property version   "1.00"
//+------------------------------------------------+
//|  Параметры отрисовки индикатора                |
//+------------------------------------------------+
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано пять буферов
#property indicator_buffers 5
//---- использовано всего одно графическое построение
#property indicator_plots   1
//---- в качестве индикатора использованы цветные свечи
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1  Teal, Magenta
//---- отображение метки индикатора
#property indicator_label1  "FiboCandles Open; FiboCandles High; FiboCandles Low; FiboCandles Close"
//+------------------------------------------------+
//|  объявление констант                           |
//+------------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчет индикатора
//---- константы фибо-уровней
#define LEVEL_1 0.236
#define LEVEL_2 0.382
#define LEVEL_3 0.500
#define LEVEL_4 0.618
#define LEVEL_5 0.762
//+------------------------------------------------+
//|  Перечисление для фибо-уровней                 |
//+------------------------------------------------+
enum ENUM_FIBORATIO //Тип константы
  {
   LEVEL_1_ = 1,   //0.236
   LEVEL_2_,       //0.382
   LEVEL_3_,       //0.500
   LEVEL_4_,       //0.618
   LEVEL_5_        //0.762
  };
//+------------------------------------------------+ 
//| Перечисление для индикации срабатывания уровня |
//+------------------------------------------------+ 
enum ENUM_ALERT_MODE //Тип константы
  {
   OnlySound,   //только звук
   OnlyAlert    //только алерт
  };
//+------------------------------------------------+
//| Входные параметры индикатора                   |
//+------------------------------------------------+
input int period=10;                        // Период индикатора
input ENUM_FIBORATIO fiboLevel=LEVEL_1_;    // Значение фибоуровня
//---- настройки для подаваемых алертов
input uint SignalBar=0;                     // Номер бара для получения сигнала (0 - текущий бар)
input ENUM_ALERT_MODE alert_mode=OnlySound; // Вариант индикации срабатывания
input uint AlertCount=0;                    // Количество подаваемых алертов
//+------------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int  min_rates_total;
//---- объявление переменной для хранения фибоуровня
double level;
//+------------------------------------------------------------------+
//|  Получение таймфрейма в виде строки                              |
//+------------------------------------------------------------------+
string GetStringTimeframe(ENUM_TIMEFRAMES timeframe)
  {
//----
   return(StringSubstr(EnumToString(timeframe),7,-1));
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация глобальных переменных 
   min_rates_total=period;

   switch(fiboLevel)
     {
      case 1: level = LEVEL_1; break;
      case 2: level = LEVEL_2; break;
      case 3: level = LEVEL_3; break;
      case 4: level = LEVEL_4; break;
      case 5: level = LEVEL_5; break;
     }

//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- превращение динамического массива ExtColorBuffer[] в цветовой индексный буфер   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);

//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);
   ArraySetAsSeries(ExtColorBuffer,true);

//---- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="Fibo Candles 2";
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
   if(rates_total<min_rates_total) return(RESET);

//---- объявления локальных переменных 
   int limit,bar,trend;
   double maxHigh,minLow,range;
   static int trend_;
   static uint buycount=0,sellcount=0;

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      trend_=0;
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);

//---- ставим счетчики алертов в исходное положение   
   if(rates_total!=prev_calculated && AlertCount)
     {
      buycount=AlertCount;
      sellcount=AlertCount;
     }

//---- восстанавливаем значения переменных
   trend=trend_;

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0) trend_=trend;

      maxHigh=high[ArrayMaximum(high,bar,period)];
      minLow=low[ArrayMinimum(low,bar,period)];
      range=maxHigh-minLow;

      if(open[bar]>close[bar])
        {
         if(!(trend<0 && range*level<close[bar]-minLow)) trend=+1;
         else trend=-1;
        }
      else
        {
         if(!(trend>0 && range*level<maxHigh-close[bar])) trend=-1;
         else trend=+1;
        }

      if(trend==+1)
        {
         ExtOpenBuffer [bar]=MathMax(open[bar], close[bar]);
         ExtCloseBuffer[bar]=MathMin(open[bar], close[bar]);
        }

      if(trend==-1)
        {
         ExtOpenBuffer [bar]=MathMin(open[bar], close[bar]);
         ExtCloseBuffer[bar]=MathMax(open[bar], close[bar]);
        }

      ExtHighBuffer [bar]=high[bar];
      ExtLowBuffer  [bar]=low[bar];

      //--- раскрашивание свечей
      if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) ExtColorBuffer[bar]=1.0;
      else                                       ExtColorBuffer[bar]=0.0;
     }

   if(ExtColorBuffer[SignalBar+1]==0 && ExtColorBuffer[SignalBar]==1 && buycount)
     {
      if(alert_mode==OnlyAlert) Alert("FiboCandles: Сигнал на покупку по ",Symbol(),GetStringTimeframe(_Period));
      if(alert_mode==OnlySound) PlaySound("alert.wav");
      buycount--;
     }

   if(ExtColorBuffer[SignalBar+1]==1 && ExtColorBuffer[SignalBar]==0 && sellcount)
     {
      if(alert_mode==OnlyAlert) Alert("FiboCandles: Сигнал на продажу по ",Symbol(),GetStringTimeframe(_Period));
      if(alert_mode==OnlySound) PlaySound("alert.wav");
      sellcount--;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
