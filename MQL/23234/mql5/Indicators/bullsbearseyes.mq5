//+------------------------------------------------------------------+
//|                                               BullsBearsEyes.mq5 | 
//|                   Copyright © 2007, EmeraldKing, transport_david | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, EmeraldKing, transport_david"
#property link "http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/"
#property description "BullsBearsEyes"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов
#property indicator_buffers 1 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован цвет BlueViolet
#property indicator_color1 BlueViolet
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 1
#property indicator_width1  1
//---- отображение метки индикатора
#property indicator_label1  "BullsBearsEyes"
//+----------------------------------------------+
//| Параметры отображения горизонтальных уровней |
//+----------------------------------------------+
#property indicator_level2 1.0
#property indicator_level3 0.75
#property indicator_level4 0.50
#property indicator_level5 0.25
#property indicator_level6 0.0
#property indicator_levelcolor LimeGreen
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Объявление констант                         |
//+----------------------------------------------+
#define RESET 0 // константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input double period=13; // Период усреднения индикатора
input double gamma=0.6; // Коэффициент сглаживания индикатора
input int    Shift=0;   // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамического массива, который в дальнейшем
//---- будет использован в качестве индикаторного буфера
double BullsBearsEyes[];
//---- объявление целочисленных переменных для хендлов индикаторов
int Bears_Handle,Bulls_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+   
//| BullsBearsEyes indicator initialization function                 | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(period+1);

//---- получение хендла индикатора iBearsPower
   Bears_Handle=iBearsPower(NULL,0,int(period));
   if(Bears_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iBearsPower");

//---- получение хендла индикатора iBullsPower
   Bulls_Handle=iBullsPower(NULL,0,int(period));
   if(Bulls_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iBullsPower");

//---- превращение динамического массива BullsBearsEyes[] в индикаторный буфер
   SetIndexBuffer(0,BullsBearsEyes,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(BullsBearsEyes,true);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"BullsBearsEyes(",period,", ",gamma,", ",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| BullsBearsEyes iteration function                                | 
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
//---- проверка количества баров на достаточность для расчета
   if(BarsCalculated(Bears_Handle)<rates_total
      || BarsCalculated(Bulls_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- объявление переменных с плавающей точкой  
   double result,Bears[],Bulls[],CU,CD;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A;
   static double L0_,L1_,L2_,L3_;
//---- Объявление целых переменных и получение уже посчитанных баров
   int limit,bar,to_copy;

//---- расчеты необходимого количества копируемых данных
//---- и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
   to_copy=limit+1;

//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(Bears,true);
   ArraySetAsSeries(Bulls,true);

//---- копируем вновь появившиеся данные в массив  
   if(CopyBuffer(Bears_Handle,0,0,to_copy,Bears)<=0) return(RESET);
   if(CopyBuffer(Bulls_Handle,0,0,to_copy,Bulls)<=0) return(RESET);

//---- восстанавливаем значения переменных
   L0=L0_;
   L1=L1_;
   L2=L2_;
   L3=L3_;

//---- пересчет значений индикаторов
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0)
        {
         L0_=L0;
         L1_=L1;
         L2_=L2;
         L3_=L3;
        }

      L0A=L0;
      L1A=L1;
      L2A=L2;
      L3A=L3;

      L0=(1.0-gamma)*(Bears[bar]+Bulls[bar])+gamma*L0A;
      L1=-gamma*L0+L0A+gamma*L1A;
      L2=-gamma*L1+L1A+gamma*L2A;
      L3=-gamma*L2+L2A+gamma*L3A;

      CU=0.0;
      CD=0.0;
      result=0.0;
      if(L0>=L1) CU=L0-L1; else CD=L1-L0;
      if(L1>=L2) CU+=L1-L2; else CD+=L2-L1;
      if(L2>=L3) CU+=L2-L3; else CD+=L3-L2;
      if(CU+CD!=0) result=CU/(CU+CD);

      BullsBearsEyes[bar]=result;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
