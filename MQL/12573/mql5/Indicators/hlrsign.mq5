//+------------------------------------------------------------------+
//|                                                      HLRSign.mq5 |
//|                                      Copyright © 2007, Alexandre |
//|                      http://www.kroufr.ru/content/view/1184/124/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, Alexandre"
#property link      "http://www.kroufr.ru/content/view/1184/124/"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//--- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован Salmon цвет
#property indicator_color1  clrSalmon
//--- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//--- отображение бычей метки индикатора
#property indicator_label1  "HLRSign Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычей линии индикатора использован MediumSeaGreen цвет
#property indicator_color2  clrMediumSeaGreen
//--- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//--- отображение медвежьей метки индикатора
#property indicator_label2 "HLRSign Buy"
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
enum Alg_Method
  {
   MODE_IN,  //Алгоритм на входе в зоны ПЗ и ПП
   MODE_OUT  //Алгоритм на выходе в зоны ПЗ и ПП
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Alg_Method Mode=MODE_IN;    // Пробойный алгоритм
input uint HLR_Range=40;          // Период усреднения индикатора
input uint HLR_UpLevel=80;        // Уровень перекупленности
input uint HLR_DnLevel=20;        // Уровень перепроданности
input int  Shift=0;               // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
int ATR_Handle;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- инициализация глобальных переменных 
   int ATR_Period=100;
   min_rates_total=int(MathMax(HLR_Range+1,ATR_Period));
//--- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }
   if(HLR_UpLevel<=HLR_DnLevel)
     {
      Print("Уровень перекупленности всегда должен быть больше уровня перепроданности!!!");
      return(INIT_FAILED);
     }
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,171);
//--- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,171);
//--- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//--- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"HLRSign(",HLR_Range,", ",Shift,")");
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
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- проверка количества баров на достаточность для расчета
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//--- объявление переменных
   int to_copy,limit;
//--- объявление переменных с плавающей точкой  
   double m_pr,HH,LL,HL,HLR0,ATR[];
   static double HLR1;
//--- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
      HLR1=0;
     }
   else limit=rates_total-prev_calculated;  // стартовый номер для расчета только новых баров
   to_copy=limit+1;
//--- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(ATR,true);
//---
   HLR0=HLR1;
//--- основной цикл расчета индикатора
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //---
      HH=high[ArrayMaximum(high,bar,HLR_Range)];
      LL=low[ArrayMinimum(low,bar,HLR_Range)];
      m_pr=(high[bar]+low[bar])/2.0;
      HL=HH-LL;
      if(HL) HLR0=100.0*(m_pr-LL)/(HL);
      else HLR0=0.0;
      //---
      if(Mode==MODE_IN)
        {
         if(HLR0>HLR_UpLevel && HLR1<=HLR_UpLevel) BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
         if(HLR0<HLR_DnLevel && HLR1>=HLR_DnLevel) SellBuffer[bar]=high[bar]+ATR[0]*3/8;
        }
      else
        {
         if(HLR0<HLR_UpLevel && HLR1>=HLR_UpLevel) SellBuffer[bar]=high[bar]+ATR[0]*3/8;
         if(HLR0>HLR_DnLevel && HLR1<=HLR_DnLevel) BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
        }

      if(bar) HLR1=HLR0;
     }
//---
   return(rates_total);
  }
//+------------------------------------------------------------------+
