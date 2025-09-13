//+------------------------------------------------------------------+
//|                                                ColorLaguerre.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано всего одно графические построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде трехцветной линии
#property indicator_type1 DRAW_COLOR_LINE
//---- в качестве цветов трехцветной линии использованы
#property indicator_color1 clrGray,clrLime,clrRed
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки сигнальной линии
#property indicator_label1  "Signal Line"
//---- в качестве цвета линии горизонтальных уровней использован синий цвет
#property indicator_levelcolor clrBlue
//---- в линии горизонтального уровня использован короткий штрих-пунктир
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input double gamma=0.7;
input int inHighLevel=85;
input int inMiddleLevel=50;
input int inLowLevel=15;
//+-----------------------------------+
//---- объявление динамических массивов, которые будут
//---- в дальнейшем использованы в качестве индикаторных буферов
double ColorBuffer[],ExtLineBuffer[];
//---- объявление целых переменных начала отсчета данных
int min_rates_total;
//---- объявление целых переменных начала отсчета данных
int HighLevel,MiddleLevel,LowLevel;
//+------------------------------------------------------------------+
//| Окрашиваем индикатор в два цвета                                 |
//+------------------------------------------------------------------+ 
void PointIndicator(int Min_rates_total,
                    double &IndBuffer[],
                    double &ColorIndBuffer[],
                    double HighLevel_,
                    double MiddleLevel_,
                    double LowLevel_,
                    int bar)
  {
//----
   if(bar<Min_rates_total+1) return;

   enum LEVEL
     {
      EMPTY,
      HighLev,
      HighLevMiddle,
      LowLevMiddle,
      LowLev
     };

   LEVEL Level0=EMPTY,Level1=EMPTY;
   double IndVelue;

//---- раскраска индикатора
   IndVelue=IndBuffer[bar];
   if(IndVelue>HighLevel_) Level0=HighLev; else if(IndVelue> MiddleLevel_)Level0=HighLevMiddle;
   if(IndVelue<LowLevel_ ) Level0=LowLev;  else if(IndVelue<=MiddleLevel_)Level0=LowLevMiddle;

   IndVelue=IndBuffer[bar-1];
   if(IndVelue>HighLevel_) Level1=HighLev; else if(IndVelue> MiddleLevel_)Level1=HighLevMiddle;
   if(IndVelue<LowLevel_ ) Level1=LowLev;  else if(IndVelue<=MiddleLevel_)Level1=LowLevMiddle;

   switch(Level0)
     {
      case HighLev: ColorIndBuffer[bar]=1; break;

      case HighLevMiddle:
         switch(Level1)
           {
            case  HighLev: ColorIndBuffer[bar]=2; break;
            case  HighLevMiddle: ColorIndBuffer[bar]=ColorIndBuffer[bar-1]; break;
            case  LowLevMiddle: ColorIndBuffer[bar]=1; break;
            case  LowLev: ColorIndBuffer[bar]=1; break;
           }
         break;

      case  LowLevMiddle:
         switch(Level1)
           {
            case  HighLev: ColorIndBuffer[bar]=2; break;
            case  HighLevMiddle: ColorIndBuffer[bar]=2; break;
            case  LowLevMiddle: ColorIndBuffer[bar]=ColorIndBuffer[bar-1]; break;
            case  LowLev: ColorIndBuffer[bar]=1; break;
           }
         break;

      case LowLev: ColorIndBuffer[bar]=2; break;
     }
//----  
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчёта данных
   min_rates_total=2;
//---- инициализация переменных 
   HighLevel=inHighLevel;
   if(HighLevel<0) HighLevel=0;
   if(HighLevel>100) HighLevel=100;
   MiddleLevel=inMiddleLevel;
   if(MiddleLevel<0) MiddleLevel=0;
   if(MiddleLevel>100) MiddleLevel=100;
   LowLevel=inLowLevel;
   if(LowLevel<0) LowLevel=0;
   if(LowLevel>100) LowLevel=100;
//---- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Laguerre(",gamma,")");
//---- создание метки для отображения в Окне данных
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorBuffer,INDICATOR_COLOR_INDEX);
//---- количество  горизонтальных уровней индикатора 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MiddleLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- в качестве цветов линий горизонтальных уровней использованы серый и розовый цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,Magenta);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,Gray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,Magenta);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const int begin,          // номер начала достоверного отсчета баров
                const double &price[]     // ценовой массив для расчета индикатора
                )
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total+begin) return(0);

//---- объявления локальных переменных 
   int first,bar;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A,LRSI=0,CU,CD;

//---- объявления статических переменных для хранения действительных значений коэфициентов
   static double L0_,L1_,L2_,L3_,L0A_,L1A_,L2A_,L3A_;

//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=begin+1; // стартовый номер для расчета всех баров
      //---- стартовая инициализация расчетных коэффициентов
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
   for(bar=first; bar<rates_total; bar++)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
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
      //----
      L0 = (1 - gamma) * price[bar] + gamma * L0A;
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
      if(CU+CD!=0) LRSI=CU/(CU+CD);

      //---- Инициализация ячейки индикаторного буфера полученным значением LRSI
      LRSI*=100;
      ExtLineBuffer[bar]=LRSI;

      //---- раскраска индикатора
      PointIndicator(31,ExtLineBuffer,ColorBuffer,HighLevel,MiddleLevel,LowLevel,bar);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
