//+------------------------------------------------------------------+
//|                                             JBrainTrend1Stop.mq5 |
//|                               Copyright © 2005, BrainTrading Inc |
//|                                      http://www.braintrading.com |
//+------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright © 2005, BrainTrading Inc."
//--- ссылка на сайт автора
#property link      "http://www.braintrading.com/"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчёта и отрисовки индикатора использовано четыре буфера
#property indicator_buffers 4
//--- использовано всего четыре графических построения
#property indicator_plots   4
//+----------------------------------------------+
//| объявление констант                          |
//+----------------------------------------------+
#define RESET 0 // Константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован Orange цвет
#property indicator_color1  clrOrange
//--- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//--- отображение бычей метки индикатора
#property indicator_label1  "JBrain1 Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьго индикатора        |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычей линии индикатора использован SpringGreen цвет
#property indicator_color2  clrSpringGreen
//--- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//--- отображение медвежьей метки индикатора
#property indicator_label2 "JBrain1 Buy"
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//--- отрисовка индикатора 3 в виде символа
#property indicator_type3   DRAW_LINE
//--- в качестве цвета медвежьей линии индикатора использован Orange цвет
#property indicator_color3  clrOrange
//--- толщина линии индикатора 3 равна 1
#property indicator_width3  1
//--- линия индикатора - сплошная
#property indicator_style3 STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width3 2
//--- отображение бычей метки индикатора
#property indicator_label3  "JBrain1 Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьго индикатора        |
//+----------------------------------------------+
//--- отрисовка индикатора 4 в виде символа
#property indicator_type4   DRAW_LINE
//--- в качестве цвета бычей линии индикатора использован SpringGreen цвет
#property indicator_color4  clrSpringGreen
//--- толщина линии индикатора 4 равна 1
#property indicator_width4  1
//--- линия индикатора - сплошная
#property indicator_style4 STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width4 2
//--- отображение медвежьей метки индикатора
#property indicator_label4 "JBrain1 Buy"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int ATR_Period=7;                       // Период ATR
input int STO_Period=9;                       // Период стохастика
input ENUM_MA_METHOD MA_Method = MODE_SMA;    // Метод усреднения
input ENUM_STO_PRICE STO_Price = STO_LOWHIGH; // Метод расчёта цен 
input int Stop_dPeriod=3;                     // Приращение периода для стопа
input int Length_=7;                          // Глубина JMA сглаживания
input int Phase_=100;                         // Параметр JMA сглаживания
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double SellStopBuffer[];
double BuyStopBuffer[];
double SellStopBuffer_[];
double BuyStopBuffer_[];
//---
double d,s,r,R_;
int p,x1,x2,P_,min_rates_total;
//--- Объявление целых переменных для хендлов индикаторов
int ATR_Handle,ATR1_Handle,STO_Handle,JH_Handle,JL_Handle,JC_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- инициализация глобальных переменных 
   d=2.3;
   s=1.5;
   x1 = 53;
   x2 = 47;
   min_rates_total=int(MathMax(MathMax(MathMax(ATR_Period,STO_Period),ATR_Period+Stop_dPeriod),30)+2);
//--- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора ATR
   ATR1_Handle=iATR(NULL,0,ATR_Period+Stop_dPeriod);
   if(ATR1_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR1");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора Stochastic
   STO_Handle=iStochastic(NULL,0,STO_Period,STO_Period,1,MA_Method,STO_Price);
   if(STO_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Stochastic");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора JMA
   JL_Handle=iCustom(NULL,0,"JMA",Length_,Phase_,4,0,0);
   if(JL_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора JMA");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора JMA
   JC_Handle=iCustom(NULL,0,"JMA",Length_,Phase_,1,0,0);
   if(JC_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора JMA");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора JMA
   JH_Handle=iCustom(NULL,0,"JMA",Length_,Phase_,3,0,0);
   if(JH_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора JMA");
      return(INIT_FAILED);
     }
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellStopBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,159);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellStopBuffer,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyStopBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyStopBuffer,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,SellStopBuffer_,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellStopBuffer_,true);
//--- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,BuyStopBuffer_,INDICATOR_DATA);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyStopBuffer_,true);
//--- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0.0);
//--- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- имя для окон данных и метка для подокон
   string short_name="JBrainTrend1Stop";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---
   return(INIT_SUCCEEDED);
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
//--- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(ATR1_Handle)<rates_total
      || BarsCalculated(STO_Handle)<rates_total
      || BarsCalculated(JH_Handle)<rates_total
      || BarsCalculated(JL_Handle)<rates_total
      || BarsCalculated(JC_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- объявления локальных переменных 
   int to_copy,limit,bar;
   double range,range1,val1,val2,val3;
   double value2[],Range[],Range1[],JH[],JL[],JC[],value3,value4,value5;
//--- расчёты необходимого количества копируемых данных
//--- и стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
      limit=rates_total-min_rates_total;   // стартовый номер для расчёта всех баров
   else limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров    
   to_copy=limit+1;
//--- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(ATR_Handle,0,0,to_copy,Range)<=0) return(RESET);
   if(CopyBuffer(STO_Handle,0,0,to_copy,value2)<=0) return(RESET);
   if(CopyBuffer(ATR1_Handle,0,0,to_copy,Range1)<=0) return(RESET);
   if(CopyBuffer(JH_Handle,0,0,to_copy,JH)<=0) return(RESET);
   if(CopyBuffer(JL_Handle,0,0,to_copy,JL)<=0) return(RESET);
   if(CopyBuffer(JC_Handle,0,0,to_copy+2,JC)<=0) return(RESET);
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(Range,true);
   ArraySetAsSeries(Range1,true);
   ArraySetAsSeries(value2,true);
   ArraySetAsSeries(JH,true);
   ArraySetAsSeries(JL,true);
   ArraySetAsSeries(JC,true);
//--- восстанавливаем значения переменных
   p=P_;
   r=R_;
//--- основной цикл расчёта индикатора
   for(bar=limit; bar>=0; bar--)
     {
      //--- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0)
        {
         P_=p;
         R_=r;
        }
      range=Range[bar]/d;
      range1=Range1[bar]*s;

      val1 = 0.0;
      val2 = 0.0;
      val3=MathAbs(NormalizeDouble(JC[bar],_Digits)-NormalizeDouble(JC[bar+2],_Digits));

      SellStopBuffer[bar]=0.0;
      BuyStopBuffer[bar]=0.0;
      SellStopBuffer_[bar]=0.0;
      BuyStopBuffer_[bar]=0.0;

      if(val3>range)
        {
         if(value2[bar]<x2 && p!=1)
           {
            value3=JH[bar]+range1/4;
            val1=value3;
            p = 1;
            r = val1;
            SellStopBuffer[bar]=val1;
            SellStopBuffer_[bar]=val1;
           }

         if(value2[bar]>x1 && p!=2)
           {
            value3=JL[bar]-range1/4;
            val2=value3;
            p = 2;
            r = val2;
            BuyStopBuffer[bar]=val2;
            BuyStopBuffer_[bar]=val2;
           }
        }

      value4 = JH[bar] + range1;
      value5 = JL[bar] - range1;

      if(val1==0 && val2==0)
        {
         if(p==1)
           {
            if(value4<r) r=value4;
            SellStopBuffer[bar]=r;
            SellStopBuffer_[bar]=r;
           }

         if(p==2)
           {
            if(value5>r) r=value5;
            BuyStopBuffer[bar]=r;
            BuyStopBuffer_[bar]=r;
           }
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
