//+------------------------------------------------------------------+ 
//|                                         ForexProfitBoost_2nb.mq5 | 
//|                               Copyright © 2015, TradeLikeaPro.ru | 
//|                                         http://tradelikeapro.ru/ | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2015, TradeLikeaPro.ru"
#property link "http://tradelikeapro.ru/"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- количество индикаторных буферов 6
#property indicator_buffers 6 
//--- использовано три графических построения
#property indicator_plots   3
//+-----------------------------------+
//| Параметры отрисовки индикатора 1  |
//+-----------------------------------+
//--- отрисовка индикатора в виде гистограммы
#property indicator_type1   DRAW_HISTOGRAM2
//--- в качестве цвета индикатора использован
#property indicator_color1  clrOrange
//--- толщина линии индикатора равна 2
#property indicator_width1  2
//--- отображение метки индикатора
#property indicator_label1  "ForexProfitBoost_2nb 1"
//+-----------------------------------+
//| Параметры отрисовки индикатора 2  |
//+-----------------------------------+
//--- отрисовка индикатора в виде гистограммы
#property indicator_type2   DRAW_HISTOGRAM2
//--- в качестве цвета индикатора использован
#property indicator_color2  clrDeepPink
//--- толщина линии индикатора равна 2
#property indicator_width2  2
//--- отображение метки индикатора
#property indicator_label2  "ForexProfitBoost_2nb 2"
//+-----------------------------------+
//| Параметры отрисовки индикатора 3  |
//+-----------------------------------+
//--- отрисовка индикатора в виде гистограммы
#property indicator_type3   DRAW_HISTOGRAM2
//--- в качестве цвета индикатора использован
#property indicator_color3  clrBlue
//--- толщина линии индикатора равна 2
#property indicator_width3  2
//--- отображение метки индикатора
#property indicator_label3  "ForexProfitBoost_2nb 3"
//+-----------------------------------+
//| объявление констант               |
//+-----------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчет индикатора
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
//--- Параметры скользящего среднего 1
input uint   MAPeriod1=7;
input  ENUM_MA_METHOD   MAType1=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice1=PRICE_CLOSE;
//--- Параметры скользящего среднего 2
input uint   MAPeriod2=21;
input  ENUM_MA_METHOD   MAType2=MODE_SMA;
input ENUM_APPLIED_PRICE   MAPrice2=PRICE_CLOSE;
input uint BBPeriod=15;
input double BBDeviation=1;
input uint BBShift=1;
//+-----------------------------------+
//--- объявление целых переменных начала отсчета данных
int  min_rates_total;
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double upmax[],upmin[],dnmax[],dnmin[],max[],min[];
//--- объявление целочисленных переменных для хендлов индикаторов
int MA1_Handle,MA2_Handle,BB_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- инициализация переменных начала отсчета данных
   min_rates_total=int(MathMax(BBPeriod,MathMax(MAPeriod1,MAPeriod2)));
//--- получение хендла индикатора iMA 1
   MA1_Handle=iMA(NULL,0,MAPeriod1,0,MAType1,MAPrice1);
   if(MA1_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMA 1");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора iMA 2
   MA2_Handle=iMA(NULL,0,MAPeriod2,0,MAType2,MAPrice2);
   if(MA2_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMA 2");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора iMA 2
   MA2_Handle=iMA(NULL,0,MAPeriod2,0,MAType2,MAPrice2);
   if(MA2_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iMA 2");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора iBands
   BB_Handle=iBands(NULL,0,BBPeriod,BBShift,BBDeviation,PRICE_CLOSE);
   if(BB_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iBands");
      return(INIT_FAILED);
     }
//--- превращение динамического массивов в индикаторные буферы
   SetIndexBuffer(0,max,INDICATOR_DATA);
   SetIndexBuffer(1,min,INDICATOR_DATA);
   SetIndexBuffer(2,dnmax,INDICATOR_DATA);
   SetIndexBuffer(3,dnmin,INDICATOR_DATA);
   SetIndexBuffer(4,upmax,INDICATOR_DATA);
   SetIndexBuffer(5,upmin,INDICATOR_DATA);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(upmax,true);
   ArraySetAsSeries(upmin,true);
   ArraySetAsSeries(dnmax,true);
   ArraySetAsSeries(dnmin,true);
   ArraySetAsSeries(max,true);
   ArraySetAsSeries(min,true);
//--- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- инициализации переменной для короткого имени индикатора
   string shortname="ForexProfitBoost_2nb";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
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
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[])
  {
//--- проверка количества баров на достаточность для расчета
   if(BarsCalculated(BB_Handle)<rates_total
      || BarsCalculated(MA1_Handle)<rates_total
      || BarsCalculated(MA2_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- объявление переменных с плавающей точкой  
   double MA1[],MA2[],UpBB[],DnBB[];
//--- объявление целочисленных переменных
   int limit,to_copy;
//--- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
   else limit=rates_total-prev_calculated;  // стартовый номер для расчета только новых баров
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(MA1,true);
   ArraySetAsSeries(MA2,true);
   ArraySetAsSeries(UpBB,true);
   ArraySetAsSeries(DnBB,true);
//---   
   to_copy=limit+1;
//--- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(MA1_Handle,0,0,to_copy,MA1)<=0) return(RESET);
   if(CopyBuffer(MA2_Handle,0,0,to_copy,MA2)<=0) return(RESET);
   if(CopyBuffer(BB_Handle,UPPER_BAND,0,to_copy,UpBB)<=0) return(RESET);
   if(CopyBuffer(BB_Handle,LOWER_BAND,0,to_copy,DnBB)<=0) return(RESET);
//--- основной цикл расчета индикатора
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      max[bar]=MathMax(UpBB[bar],MathMax(MA1[bar],MA2[bar]));
      min[bar]=MathMin(DnBB[bar],MathMin(MA1[bar],MA2[bar]));
      //---
      if(MA1[bar]>MA2[bar])
        {
         upmax[bar]=MathMax(MA1[bar],MA2[bar]);
         upmin[bar]=MathMin(DnBB[bar],MathMin(MA1[bar],MA2[bar]));
         dnmax[bar]=EMPTY_VALUE;
         dnmin[bar]=EMPTY_VALUE;
        }
      else
        {
         dnmax[bar]=MathMax(UpBB[bar],MathMax(MA1[bar],MA2[bar]));
         dnmin[bar]=MathMin(MA1[bar],MA2[bar]);
         upmax[bar]=EMPTY_VALUE;
         upmin[bar]=EMPTY_VALUE;
        }
     }
//---    
   return(rates_total);
  }
//+------------------------------------------------------------------+
