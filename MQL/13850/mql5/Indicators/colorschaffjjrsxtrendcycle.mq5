//+------------------------------------------------------------------+
//|                                   ColorSchaffJJRSXTrendCycle.mq5 |
//|                                  Copyright © 2011, EarnForex.com |
//|                                        http://www.earnforex.com/ |
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2011, EarnForex.com"
#property link      "http://www.earnforex.com"
#property description "Schaff Trend Cycle - Cyclical Stoch over Stoch over JJRSX MACD."
#property description "The code adapted Nikolay Kositsin."
//---- номер версии индикатора
#property version   "2.10"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------------------+
//| Параметры отрисовки индикатора                |
//+-----------------------------------------------+
//---- отрисовка индикатора в виде цветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов гистограммы использованы
#property indicator_color1 clrDeepPink,clrMediumOrchid,clrGold,clrPeru,clrMediumBlue,clrDodgerBlue,clrGreen,clrLime
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "Schaff JJRSX Trend Cycle"
//+-----------------------------------------------+
//| Объявление перечислений                       |
//+-----------------------------------------------+
enum Applied_price_      // тип константы
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price 
  };
//+-----------------------------------------------+
//| Объявление констант                           |
//+-----------------------------------------------+
#define RESET  0 // константа для возврата терминалу команды на пересчет индикатора
//+-----------------------------------------------+
//| Входные параметры индикатора                  |
//+-----------------------------------------------+
input uint Fast_JJRSX = 23; // Период быстрого JJRSX
input uint Slow_JJRSX = 50; // Период медленного JJRSX
input uint Smooth = 8;  // Глубина JJMA усреднения
input int JPhase = 100; // Параметр JJMA усреднения
                        // изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
input Applied_price_ IPC=PRICE_CLOSE_; // Ценовая константа
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
input uint Cycle=10; // Период стохастического осциллятора
input int HighLevel=+60;
input int MiddleLevel=0;
input int LowLevel=-60;
//+-----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double STC_Buffer[];
double ColorSTC_Buffer[];
//----
int Count[];
bool st1_pass,st2_pass;
double MACD[],ST[],Factor;
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//--- объявление целочисленных переменных для хендлов индикаторов
int Ind1_Handle,Ind2_Handle;
//+------------------------------------------------------------------+
//| Пересчет позиции самого нового элемента в массиве                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// возврат по ссылке номера текущего значения ценового ряда
                          int Rates_total,
                          int Bar)
  {
//----
   if(!Bar) return;
//----
   int numb;
   static int count=1;
   count--;
//----
   if(count<0) count=int(Cycle)-1;
//----
   for(int iii=0; iii<int(Cycle); iii++)
     {
      numb=iii+count;
      if(numb>int(Cycle)-1) numb-=int(Cycle);
      CoArr[iii]=numb;
     }
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=32+int(Cycle)+1;
//--- получение хендла индикатора MA 1
   Ind1_Handle=iCustom(NULL,0,"JJRSX",Fast_JJRSX,Smooth,JPhase,IPC,0);
   if(Ind1_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора JJRSX 1");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора MA 2
   Ind2_Handle=iCustom(NULL,0,"JJRSX",Slow_JJRSX,Smooth,JPhase,IPC,0);
   if(Ind2_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора JJRSX 2");
      return(INIT_FAILED);
     }
//---- распределение памяти под массивы переменных
   if(ArrayResize(ST,Cycle)<int(Cycle))
     {
      Print("Не удалось распределить память под массив ST");
      return(INIT_FAILED);
     }
   if(ArrayResize(MACD,Cycle)<int(Cycle))
     {
      Print("Не удалось распределить память под массив MACD");
      return(INIT_FAILED);
     }
   if(ArrayResize(Count,Cycle)<int(Cycle))
     {
      Print("Не удалось распределить память под массив Count");
      return(INIT_FAILED);
     }
//---- инициализация констант  
   Factor=0.5;
   st1_pass = false;
   st2_pass = false;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,STC_Buffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(STC_Buffer,true);
//---- превращение динамического массива в цветовой буфер
   SetIndexBuffer(1,ColorSTC_Buffer,INDICATOR_COLOR_INDEX);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorSTC_Buffer,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Schaff JJRSX Trend Cycle( ",Fast_JJRSX,", ",Slow_JJRSX,", ",Cycle," )");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- количество  горизонтальных уровней индикатора 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MiddleLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- в качестве цветов линий горизонтальных уровней использованы серый и розовый цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrMediumSeaGreen);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrMagenta);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Schaff JJRSX Trend Cycle                                         |
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
//---- 
   if(rates_total<min_rates_total) return(RESET);
   if(BarsCalculated(Ind1_Handle)<Bars(Symbol(),PERIOD_CURRENT)) return(prev_calculated);
   if(BarsCalculated(Ind2_Handle)<Bars(Symbol(),PERIOD_CURRENT)) return(prev_calculated);
//---- объявление переменных с плавающей точкой  
   double fastJJRSX[],slowJJRSX[],LLV,HHV;
//---- объявление целочисленных переменных
   int limit,to_copy,bar,Bar0,Bar1;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      limit=rates_total-2; // стартовый номер для расчета всех баров
      STC_Buffer[limit+1]=0;
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(Ind1_Handle,0,0,to_copy,fastJJRSX)<=0) return(RESET);
   if(CopyBuffer(Ind2_Handle,0,0,to_copy,slowJJRSX)<=0) return(RESET);
//---- индексация элементов в массивах как в таймсериях
   ArraySetAsSeries(fastJJRSX,true);
   ArraySetAsSeries(slowJJRSX,true);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Bar0=Count[0];
      Bar1=Count[1];
      //----
      MACD[Bar0]=fastJJRSX[bar]-slowJJRSX[bar];
      //----
      LLV=MACD[ArrayMinimum(MACD)];
      HHV=MACD[ArrayMaximum(MACD)];
      //---- расчет первого стохастика
      if(HHV-LLV!=0) ST[Bar0]=((MACD[Bar0]-LLV)/(HHV-LLV))*100;
      else           ST[Bar0]=ST[Bar1];
      //----
      if(st1_pass) ST[Bar0]=Factor *(ST[Bar0]-ST[Bar1])+ST[Bar1];
      st1_pass=true;
      //----
      LLV=ST[ArrayMinimum(ST)];
      HHV=ST[ArrayMaximum(ST)];
      //---- расчет второго стохастика
      if(HHV-LLV!=0) STC_Buffer[bar]=((ST[Bar0]-LLV)/(HHV-LLV))*200-100;
      else           STC_Buffer[bar]=STC_Buffer[bar+1];
      //---- сглаживание второго стохастика
      if(st2_pass) STC_Buffer[bar]=Factor *(STC_Buffer[bar]-STC_Buffer[bar+1])+STC_Buffer[bar+1];
      st2_pass=true;
      //---- пересчет позиции элементов в кольцевых буферах на смене бара
      Recount_ArrayZeroPos(Count,rates_total,bar);
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) limit=rates_total-min_rates_total;
//---- основной цикл раскраски индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double Sts=STC_Buffer[bar];
      double dSts=Sts-STC_Buffer[bar+1];
      int clr=4;
      //----
      if(Sts>0)
        {
         if(Sts>HighLevel)
           {
            if(dSts>=0) clr=7;
            else clr=6;
           }
         else
           {
            if(dSts>=0) clr=5;
            else clr=4;
           }
        }
      //----  
      if(Sts<0)
        {
         if(Sts<LowLevel)
           {
            if(dSts<0) clr=0;
            else clr=1;
           }
         else
           {
            if(dSts<0) clr=2;
            else clr=3;
           }
        }
      //----  
      ColorSTC_Buffer[bar]=clr;
     }
//----
   return(rates_total);
  }
//+----------------------------------------------------
