//+--------------------------------------------------------------------------+
//|                                                      Laguerre_PlusDi.mq5 |
//|                         Copyright © 2005, Emerald King / transport_david | 
//| http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/ | 
//+--------------------------------------------------------------------------+
#property copyright "Copyright © 2007, Emerald King / transport_david"
#property link "http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчёта и отрисовки индикатора использован один буфер
#property indicator_buffers 1
//--- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//--- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//--- в качестве цвета линии индикатора использован Teal цвет
#property indicator_color1  clrTeal
//--- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width1 2
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level2 0.75
#property indicator_level3 0.45
#property indicator_level4 0.15
//--- в качестве цвета линии горизонтального уровня использован розовый цвет
#property indicator_levelcolor clrMagenta
//--- в линии горизонтального уровня использован короткий штрих-пунктир
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint ADXPeriod=14;
input double gamma=0.764;
//+----------------------------------------------+
//--- объявление динамического массива, который в дальнейшем
//--- будет использован в качестве индикаторного буфера
double ExtLineBuffer[];
//--- объявление целочисленных переменных начала отсчёта данных
int min_rates_total;
//--- объявление целочисленных переменных для хендлов индикаторов
int ADX_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   min_rates_total=int(ADXPeriod);
//--- получение хендла индикатора iADX
   ADX_Handle=iADX(NULL,0,ADXPeriod);
   if(ADX_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора iADX");
      return(INIT_FAILED);
     }
//--- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ExtLineBuffer,true);
//--- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Laguerre_PlusDi(",ADXPeriod,", ",gamma,")");
//--- создание метки для отображения в Окне данных
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//--- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- осуществление сдвига начала отсчёта отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
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
   if(BarsCalculated(ADX_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//--- объявления локальных переменных 
   int to_copy,limit,bar;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A,LDMIP=0,CU,CD,DMIP[];
//--- объявления статических переменных для хранения действительных значений коэфициентов
   static double L0_,L1_,L2_,L3_,L0A_,L1A_,L2A_,L3A_;
//--- расчёт стартового номера для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчёта всех баров
      //--- стартовая инициализация расчётных коэффициентов
      L0_ = 0.0;
      L1_ = 0.0;
      L2_ = 0.0;
      L3_ = 0.0;
      L0A_ = 0.0;
      L1A_ = 0.0;
      L2A_ = 0.0;
      L3A_ = 0.0;
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
   to_copy=limit+1;
//--- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(ADX_Handle,PLUSDI_LINE,0,to_copy,DMIP)<=0) return(RESET);
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(DMIP,true);
//--- восстанавливаем значения переменных
   L0 = L0_;
   L1 = L1_;
   L2 = L2_;
   L3 = L3_;
   L0A = L0A_;
   L1A = L1A_;
   L2A = L2A_;
   L3A = L3A_;
//--- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      L0A = L0;
      L1A = L1;
      L2A = L2;
      L3A = L3;
      //---
      L0 = (1 - gamma) * DMIP[bar] + gamma * L0A;
      L1 = - gamma * L0 + L0A + gamma * L1A;
      L2 = - gamma * L1 + L1A + gamma * L2A;
      L3 = - gamma * L2 + L2A + gamma * L3A;
      //---
      CU = 0;
      CD = 0;
      //--- 
      if(L0 >= L1) CU  = L0 - L1; else CD  = L1 - L0;
      if(L1 >= L2) CU += L1 - L2; else CD += L2 - L1;
      if(L2 >= L3) CU += L2 - L3; else CD += L3 - L2;
      //---
      if(CU+CD!=0) LDMIP=CU/(CU+CD);
      //--- инициализация ячейки индикаторного буфера полученным значением LRSI
      ExtLineBuffer[bar]=LDMIP;
      //--- запоминаем значения переменных перед прогонами на текущем баре
      if(bar==1)
        {
         L0_ = L0;
         L1_ = L1;
         L2_ = L2;
         L3_ = L3;
         L0A_ = L0A;
         L1A_ = L1A;
         L2A_ = L2A;
         L3A_ = L3A;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
