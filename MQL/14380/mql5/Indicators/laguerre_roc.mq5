//+------------------------------------------------------------------+ 
//|                                                 Laguerre_ROC.mq5 | 
//|                           Copyright © 2005, Emerald King , MTE&I | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, Emerald King , MTE&I"
#property link ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 3
#property indicator_buffers 3 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//| Объявление констант                          |
//+----------------------------------------------+
#define RESET 0                       // константа для возврата терминалу команды на пересчет индикатора
#define INDICATOR_NAME "Laguerre_ROC" // константа для имени индикатора
//+----------------------------------------------+
//| Параметры отрисовки индикатора               |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде гистограммы
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- в качестве цветов индикатора использованы
#property indicator_color1  clrDarkOrange,clrBrown,clrGray,clrBlue,clrDeepSkyBlue
//---- толщина линии индикатора 1 равна 5
#property indicator_width1  5
//---- отображение метки индикатора
#property indicator_label1  INDICATOR_NAME
//+----------------------------------------------+
//| Параметры границ окна индикатора             |
//+----------------------------------------------+
#property indicator_maximum +1.1
#property indicator_minimum -0.1
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level1 0
#property indicator_levelcolor clrBlueViolet
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Объявление перечисления                      |
//+----------------------------------------------+  
enum WIDTH
  {
   Width_1=1, //1
   Width_2,   //2
   Width_3,   //3
   Width_4,   //4
   Width_5    //5
  };
//+----------------------------------------------+
//| Объявление перечисления                      |
//+----------------------------------------------+
enum STYLE
  {
   SOLID_,       // Сплошная линия
   DASH_,        // Штриховая линия
   DOT_,         // Пунктирная линия
   DASHDOT_,     // Штрих-пунктирная линия
   DASHDOTDOT_   // Штрих-пунктирная линия с двойными точками
  };

//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint vPeriod=5;                 // Период
input double gamma=0.500;             // Коэффициент усреднения                
input double UpLevel=0.75;            // Уровень перекупленности в %%
input double DnLevel=0.25;            // Уровень перепроданности в %%
input color UpLevelsColor=clrMagenta; // Цвет уровня перекупленности
input color DnLevelsColor=clrMagenta; // Цвет уровня перепроданности
input STYLE Levelstyle=DASH_;         // Стиль уровней
input WIDTH  LevelsWidth=Width_1;     // Толщина уровней
input int  Shift=0;                   // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double UpIndBuffer[],DnIndBuffer[],ColorIndBuffer[];
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- инициализация переменных 
   min_rates_total=int(vPeriod+1);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,UpIndBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(UpIndBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DnIndBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(DnIndBuffer,true);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(2,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorIndBuffer,true);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,INDICATOR_NAME);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- параметры отрисовки линий  
   IndicatorSetInteger(INDICATOR_LEVELS,3);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,UpLevel);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,UpLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,0,LevelsWidth);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,DnLevel);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,DnLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,1,LevelsWidth);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,0.5);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,DASHDOTDOT_);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,2,0);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Custom iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const int begin,          // номер начала достоверного отсчета баров
                const double &price[]) // ценовой массив для расчета индикатора
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total+begin) return(RESET);
//---- объявления статических переменных для хранения действительных значений коэфициентов
   static double L0_,L1_,L2_,L3_,L0A_,L1A_,L2A_,L3A_;
//---- объявления локальных переменных 
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A,CU,CD,ROC,LROC=0;
   int limit,bar,vbar,clr;
//---- расчеты необходимого количества копируемых данных и
//---- стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1-begin; // стартовый номер для расчета всех баров
      //---- стартовая инициализация расчетных коэффициентов
      bar=limit+1;
      vbar=limit+int(vPeriod)+1;
      ROC=(price[bar]-price[vbar])/price[vbar]+_Point;
      L0_ = ROC;
      L1_ = ROC;
      L2_ = ROC;
      L3_ = ROC;
      L0A_ = ROC;
      L1A_ = ROC;
      L2A_ = ROC;
      L3A_ = ROC;
      //---- осуществление сдвига начала отсчета отрисовки индикатора 1
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров 
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(price,true);
//---- восстанавливаем значения переменных
   L0 = L0_;
   L1 = L1_;
   L2 = L2_;
   L3 = L3_;
   L0A = L0A_;
   L1A = L1A_;
   L2A = L2A_;
   L3A = L3A_;
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      L0A = L0;
      L1A = L1;
      L2A = L2;
      L3A = L3;
      vbar=bar+int(vPeriod);
      ROC=(price[bar]-price[vbar])/price[vbar]+_Point;
      //----
      L0 = (1 - gamma) * ROC + gamma * L0A;
      L1 = - gamma * L0 + L0A + gamma * L1A;
      L2 = - gamma * L1 + L1A + gamma * L2A;
      L3 = - gamma * L2 + L2A + gamma * L3A;
      //----
      CU = 0;
      CD = 0;
      //---- 
      if(L0 >= L1) CU  = L0 - L1; else CD  = L1 - L0;
      if(L1 >= L2) CU += L1 - L2; else CD += L2 - L1;
      if(L2 >= L3) CU += L2 - L3; else CD += L3 - L2;
      //----
      if(CU+CD!=0) LROC=CU/(CU+CD);
      //---- запоминаем значения переменных перед прогонами на текущем баре
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
      //----
      UpIndBuffer[bar]=LROC;
      DnIndBuffer[bar]=0.5;
      clr=2;
      if(LROC>UpLevel) clr=4;
      else if(LROC>0.5) clr=3;
      //----
      if(LROC<DnLevel) clr=0;
      else if(LROC<0.5) clr=1;
      //----
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
