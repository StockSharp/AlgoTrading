//+------------------------------------------------------------------+
//|                                                     Karpenko.mq5 |
//|                        Copyright 2014, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2014, MetaQuotes Software Corp."
//--- ссылка на сайт автора
#property link "http://www.metaquotes.net" 
#property description "Karpenko"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window
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
#property indicator_color1  clrPaleGreen,clrLightPink
//--- отображение метки индикатора
#property indicator_label1  "Karpenko"
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint Basic_MA=144;     // Период MA
input uint History=500;      // Период истории
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double UpBuffer[],DnBuffer[];
//--- объявление целых переменных для хранения хендлов индикаторов
int Ind_Handle;
//--- объявление целых переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_total=int(MathMax(Basic_MA,History));
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
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"Karpenko");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчёта индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчёта индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(RESET);
//--- объявления локальных переменных 
   double sum_c,up,dw,base;
   int limit,bar,k;
//--- расчёты необходимого количества копируемых данных
//--- и стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчёта всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      k=0;
      sum_c=0.0;
      while(k<int(Basic_MA)) {sum_c+=close[bar+k]; k++;}
      base=sum_c/Basic_MA;

      k=0;
      sum_c=0.0;
      while(k<int(History)) {sum_c+=high[bar+k]-low[bar+k]; k++;}
      up=sum_c/History;
      dw=up;

      double Up=base;
      while(high[bar]>Up) {up*=1.618; Up=base+up;}

      double Dn=base;
      while(low[bar]<Dn) {dw*=1.618; Dn=base-dw;}

      if(base==Up)
        {
         UpBuffer[bar]=base-dw;
         DnBuffer[bar]=base;
        }
      else
        {
         UpBuffer[bar]=base+up;
         DnBuffer[bar]=base;
        }
     }
//---    

   return(rates_total);
  }
//+------------------------------------------------------------------+
