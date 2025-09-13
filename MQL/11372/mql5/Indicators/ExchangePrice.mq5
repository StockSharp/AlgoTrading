//+----------------------------------------------------------------------------+
//|                                                          ExchangePrice.mq5 |
//|                                                  Copyright 2013, papaklass |
//|                                     http://www.mql4.com/ru/users/papaklass |
//+----------------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright 2013, papaklass"
//--- ссылка на сайт автора
#property link      "http://www.mql4.com/ru/users/papaklass"
#property description "ExchangePrice"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Параметры отрисовки индикатора 1             |
//+----------------------------------------------+
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrBlue,clrIndianRed
//--- отображение метки индикатора
#property indicator_label1  "ExchangePrice"
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int countBarsS = 96;
input int countBarsL = 288;
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double UpBuffer[],DnBuffer[];
//--- объявление целочисленных переменных для хранения хендлов индикаторов
int Ind_Handle;
//--- объявление целочисленных  переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_total=int(MathMax(countBarsS,countBarsL));
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(UpBuffer,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(DnBuffer,true);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"ExchangePrice");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const int begin,          // номер начала достоверного отсчёта баров
                const double &price[])    // ценовой массив для расчёта индикатора
  {
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total+begin) return(RESET);
//--- объявления локальных переменных 
   double current,historyL,historyS;
   int limit,bar;
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(price,true);
//--- расчёты необходимого количества копируемых данных
//--- и стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-min_rates_total-1-begin; // стартовый номер для расчёта всех баров
      //--- осуществление сдвига начала отсчёта отрисовки индикатора
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
//--- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      current=price[bar];
      historyS=price[bar+countBarsS];
      historyL=price[bar+countBarsL];
      UpBuffer[bar]=(current-historyS)/_Point;
      DnBuffer[bar]=(current-historyL)/_Point;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
