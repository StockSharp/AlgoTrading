//+------------------------------------------------------------------+
//|                                                     Laguerre.mq5 |
//|                             Copyright © 2010,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//--- для расчета и отрисовки индикатора использован один буфер
#property indicator_buffers 1
//--- использовано всего одно графическое построение
#property indicator_plots   1
//--- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//--- в качестве цвета линии индикатора использован Magenta цвет
#property indicator_color1  Magenta
//--- значения горизонтальных уровней индикатора
#property indicator_level2 0.75
#property indicator_level3 0.45
#property indicator_level4 0.15
//--- в качестве цвета линии горизонтального уровня использован синий цвет
#property indicator_levelcolor Blue
//--- в линии горизонтального уровня использован короткий штрих-пунктир
#property indicator_levelstyle STYLE_DASHDOTDOT
//--- входные параметры индикатора
input double gamma=0.7;
//--- объявление динамического массива, который в дальнейшем
//--- будет использован в качестве индикаторного буфера
double ExtLineBuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//--- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Laguerre(",gamma,")");
//--- создание метки для отображения в Окне данных
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//--- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,     // количество истории в барах на текущем тике
                const int prev_calculated, // количество истории в барах на предыдущем тике
                const int begin,           // номер начала достоверного отсчета баров
                const double &price[])     // ценовой массив для расчета индикатора
  {
//--- проверка количества баров на достаточность для расчета
   if(rates_total<begin) return(0);
//--- объявления локальных переменных 
   int first,bar;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A,LRSI=0,CU,CD;
//--- объявления статических переменных для хранения действительных значений коэфициентов
   static double L0_,L1_,L2_,L3_,L0A_,L1A_,L2A_,L3A_;
//--- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=begin; // стартовый номер для расчета всех баров
      //--- стартовая инициализация расчетных коэффициентов
      L0_ = price[first];
      L1_ = price[first];
      L2_ = price[first];
      L3_ = price[first];
      L0A_ = price[first];
      L1A_ = price[first];
      L2A_ = price[first];
      L3A_ = price[first];
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//--- восстанавливаем значения переменных
   L0 = L0_;
   L1 = L1_;
   L2 = L2_;
   L3 = L3_;
   L0A = L0A_;
   L1A = L1A_;
   L2A = L2A_;
   L3A = L3A_;
//--- основной цикл расчета индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      //--- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==rates_total-1)
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

      L0A = L0;
      L1A = L1;
      L2A = L2;
      L3A = L3;
      //---
      L0 = (1 - gamma) * price[bar] + gamma * L0A;
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
      if(CU+CD!=0) LRSI=CU/(CU+CD);
      //--- инициализация ячейки индикаторного буфера полученным значением LRSI
      ExtLineBuffer[bar]=LRSI;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
