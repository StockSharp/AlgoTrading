//+---------------------------------------------------------------------+
//|                                                         TSI_CCI.mq4 |
//|                         Copyright © 2006, MetaQuotes Software Corp. |
//|                                           http://www.metaquotes.net |
//+---------------------------------------------------------------------+ 
//| Для работы индикатора следует положить файл SmoothAlgorithms.mqh    |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2006, MetaQuotes Software Corp."
//--- ссылка на сайт автора
#property link "http://www.metaquotes.net" 
#property description "TSI_CCI"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//--- отрисовка индикатора в виде цветного облака
#property indicator_type1   DRAW_FILLING
//--- в качестве цветов индикатора использованы
#property indicator_color1  clrBlue,clrIndianRed
//--- отображение метки индикатора
#property indicator_label1  "TSI_CCI"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 +50
#property indicator_level2   0
#property indicator_level3 -50
#property indicator_levelcolor clrMagenta
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных классов CXMA и CMomentum из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5;
CMomentum Mom;
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//| объявление перечислений                      |
//+----------------------------------------------+
/*enum Smooth_Method - перечисление объявлено в файле SmoothAlgorithms.mqh
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
input Smooth_Method XMA_Method=MODE_EMA;          // Метод усреднения
input uint CCIPeriod=15;                          // Период индикатора CCI
input ENUM_APPLIED_PRICE   CCIPrice=PRICE_CLOSE;  // Цена индикатора CCI
input uint MomPeriod=1;                           // Период моментума
input uint XLength1=5;                            // Глубина первого усреднения
input uint XLength2=8;                            // Глубина второго усреднения
input uint XLength3=10;                           // Глубина усреднения сигнальной линии
input int XPhase=15;                              // Параметр сглаживания
//--- XPhase: для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//--- XPhase: для VIDIA это период CMO, для AMA это период медленной скользящей
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double UpBuffer[],DnBuffer[];
//--- объявление целочисленных переменных для хранения хендлов индикаторов
int Ind_Handle;
//--- объявление целочисленных переменных начала отсчёта данных
int min_rates_total,min_rates_1,min_rates_2,min_rates_3,min_rates_4;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_1=int(CCIPeriod);
   min_rates_2=min_rates_1+int(MomPeriod);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_4=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total=min_rates_4+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
//--- получение хендла индикатора TSI_MACD
   Ind_Handle=iCCI(Symbol(),PERIOD_CURRENT,CCIPeriod,CCIPrice);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора TSI_MACD");
      return(INIT_FAILED);
     }
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
   IndicatorSetString(INDICATOR_SHORTNAME,"TSI_CCI");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
   if(rates_total<min_rates_total || BarsCalculated(Ind_Handle)<rates_total) return(RESET);
//--- объявления локальных переменных 
   double CCI[],mtm,xmtm,xxmtm,absmtm,xabsmtm,xxabsmtm,tsi,xtsi;
   int to_copy,limit,bar,maxbar1,maxbar2,maxbar3,maxbar4;
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(CCI,true);
//--- расчёты необходимого количества копируемых данных
//--- и стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-min_rates_1-1; // стартовый номер для расчёта всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
//---   
   to_copy=limit+1;
//--- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(Ind_Handle,0,0,to_copy,CCI)<=0) return(RESET);
//---  
   maxbar1=rates_total-min_rates_1-1;
   maxbar2=rates_total-min_rates_2-1;
   maxbar3=rates_total-min_rates_3-1;
   maxbar4=rates_total-min_rates_4-1;
//--- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      mtm=Mom.MomentumSeries(maxbar1,prev_calculated,rates_total,MomPeriod,CCI[bar],bar,true);
      absmtm=MathAbs(mtm);
      xmtm=XMA1.XMASeries(maxbar2,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,mtm,bar,true);
      xabsmtm=XMA2.XMASeries(maxbar2,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,absmtm,bar,true);
      xxmtm=XMA3.XMASeries(maxbar3,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xmtm,bar,true);
      xxabsmtm=XMA4.XMASeries(maxbar3,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xabsmtm,bar,true);
      if(xxabsmtm) tsi=100*xxmtm/xxabsmtm;
      else tsi=0;
      if(!tsi) tsi=0.000000001;
      xtsi=XMA5.XMASeries(maxbar4,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,tsi,bar,true);
      if(!xtsi) xtsi=0.000000001;
      UpBuffer[bar]=tsi;
      DnBuffer[bar]=xtsi;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+