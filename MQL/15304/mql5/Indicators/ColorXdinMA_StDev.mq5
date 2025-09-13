//+---------------------------------------------------------------------+
//|                                               ColorXdinMA_StDev.mq5 | 
//|                                          Copyright © 2011,   dimeon | 
//|                                                                     | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, dimeon"
#property link ""
//---- номер версии индикатора
#property version   "1.03"
//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- для расчёта и отрисовки индикатора использовано шесть буферов
#property indicator_buffers 6
//---- использовано всего пять графических построений
#property indicator_plots   5
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде многоцветной линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цветов трёхцветной линии использованы
#property indicator_color1  clrYellow,clrBlue,clrRed
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "XdinMA"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован розовый цвет
#property indicator_color2  clrMagenta
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение медвежьей метки индикатора
#property indicator_label2  "Dn_Signal 1"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде символа
#property indicator_type3   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован голубой цвет
#property indicator_color3  clrDeepSkyBlue
//---- толщина линии индикатора 3 равна 2
#property indicator_width3  2
//---- отображение бычей метки индикатора
#property indicator_label3  "Up_Signal 1"
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде символа
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета медвежьего индикатора использован розовый цвет
#property indicator_color4  clrMagenta
//---- толщина линии индикатора 4 равна 4
#property indicator_width4  4
//---- отображение медвежьей метки индикатора
#property indicator_label4  "Dn_Signal 2"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 5 в виде символа
#property indicator_type5   DRAW_ARROW
//---- в качестве цвета бычьего индикатора использован голубой цвет
#property indicator_color5  clrDeepSkyBlue
//---- толщина линии индикатора 5 равна 4
#property indicator_width5  4
//---- отображение бычей метки индикатора
#property indicator_label5  "Up_Signal 2"
//+-----------------------------------+
//|  Описание класса CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
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
//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_SMA_; //метод усреднения
input int Length_main=10; //глубина main усреднения
input int Length_plus=20; //глубина plus усреднения                  
input int PhaseX=15; //параметр усреднения,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE_;//ценовая константа
input double dK1=1.5;  //коэффициент 1 для квадратичного фильтра
input double dK2=2.5;  //коэффициент 2 для квадратичного фильтра
input uint std_period=9; //период квадратичного фильтра
input int Shift=0; // сдвиг индикатора по горизонтали в барах
input int PriceShift=0; // cдвиг индикатора по вертикали в пунктах
//+-----------------------------------+

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double XdinMA[],ColorXdinMA[];
double BearsBuffer1[],BullsBuffer1[];
double BearsBuffer2[],BullsBuffer2[];
//---- Объявление переменной значения вертикального сдвига мувинга
double dPriceShift;
double dXdinMA[];
//---- Объявление целых переменных начала отсчёта данных
int  min_rates_total;
//+------------------------------------------------------------------+   
//| XdinMA indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   int StartBars1=XMA1.GetStartBars(MA_Method1, Length_main, PhaseX);
   int StartBars2=XMA2.GetStartBars(MA_Method1, Length_plus, PhaseX);
   min_rates_total=MathMax(StartBars1,StartBars2)+1+int(std_period);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMALengthCheck("Length_main", Length_main);
   XMA2.XMALengthCheck("Length_plus", Length_plus);
//---- установка алертов на недопустимые значения внешних переменных
   XMA1.XMAPhaseCheck("PhaseX",PhaseX,MA_Method1);

//---- Инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
//---- Распределение памяти под массивы переменных  
   ArrayResize(dXdinMA,std_period);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,XdinMA,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorXdinMA,INDICATOR_COLOR_INDEX);

//---- превращение динамического массива BearsBuffer в индикаторный буфер
   SetIndexBuffer(2,BearsBuffer1,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива BullsBuffer в индикаторный буфер
   SetIndexBuffer(3,BullsBuffer1,INDICATOR_DATA);
//---- осуществление сдвига индикатора 3 по горизонтали
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива BearsBuffer в индикаторный буфер
   SetIndexBuffer(4,BearsBuffer2,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- превращение динамического массива BullsBuffer в индикаторный буфер
   SetIndexBuffer(5,BullsBuffer2,INDICATOR_DATA);
//---- осуществление сдвига индикатора 3 по горизонтали
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- выбор символа для отрисовки
   PlotIndexSetInteger(4,PLOT_ARROW,159);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(MA_Method1);
   StringConcatenate(shortname,"XdinMA(",Length_main,", ",Length_plus,", ",Smooth,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| XdinMA iteration function                                        | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- Объявление переменных с плавающей точкой  
   double price_,ma_main,ma_plus;
//---- Объявление целых переменных и получение уже посчитанных баров
   int first1,first2,bar;
   double xdinma,SMAdif,Sum,StDev,dstd,BEARS1,BULLS1,BEARS2,BULLS2,Filter1,Filter2;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first1=0; // стартовый номер для расчёта всех баров
      first2=min_rates_total;
     }
   else
     {
      first1=prev_calculated-1; // стартовый номер для расчёта новых баров
      first2=first1;
     }

//---- Основной цикл расчёта индикатора
   for(bar=first1; bar<rates_total && !IsStopped(); bar++)
     {
      //---- Вызов функции PriceSeries для получения входной цены price_
      price_=PriceSeries(IPC,bar,open,low,high,close);

      //---- Два вызова функции XMASeries.   
      ma_main = XMA1.XMASeries(0, prev_calculated, rates_total, MA_Method1, PhaseX, Length_main, price_, bar, false);
      ma_plus = XMA2.XMASeries(0, prev_calculated, rates_total, MA_Method1, PhaseX, Length_plus, price_, bar, false);
      //----       
      XdinMA[bar]=ma_main*2-ma_plus+dPriceShift;
     }

//---- Основной цикл раскраски индикатора
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorXdinMA[bar]=0;
      if(XdinMA[bar-1]<XdinMA[bar]) ColorXdinMA[bar]=1;
      if(XdinMA[bar-1]>XdinMA[bar]) ColorXdinMA[bar]=2;
     }
     
//---- пересчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first1=min_rates_total;
//---- основной цикл расчёта индикатора стандартных отклонений
   for(bar=first1; bar<rates_total && !IsStopped(); bar++)
     {
      //---- загружаем приращения индикатора в массив для промежуточных вычислений
      for(int iii=0; iii<int(std_period); iii++) dXdinMA[iii]=XdinMA[bar-iii]-XdinMA[bar-iii-1];

      //---- находим простое среднее приращений индикатора
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dXdinMA[iii];
      SMAdif=Sum/std_period;

      //---- находим сумму квадратов разностей приращений и среднего
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dXdinMA[iii]-SMAdif,2);

      //---- определяем итоговое значение среднеквадратичного отклонения StDev от приращения индикатора
      StDev=MathSqrt(Sum/std_period);

      //---- инициализация переменных
      dstd=NormalizeDouble(dXdinMA[0],_Digits+2);
      Filter1=NormalizeDouble(dK1*StDev,_Digits+2);
      Filter2=NormalizeDouble(dK2*StDev,_Digits+2);
      BEARS1=EMPTY_VALUE;
      BULLS1=EMPTY_VALUE;
      BEARS2=EMPTY_VALUE;
      BULLS2=EMPTY_VALUE;
      xdinma=XdinMA[bar];

      //---- вычисление индикаторных значений
      if(dstd<-Filter1 && dstd>=-Filter2) BEARS1=xdinma; //есть нисходящий тренд
      if(dstd<-Filter2) BEARS2=xdinma; //есть нисходящий тренд
      if(dstd>+Filter1 && dstd<=+Filter2) BULLS1=xdinma; //есть восходящий тренд
      if(dstd>+Filter2) BULLS2=xdinma; //есть восходящий тренд

      //---- инициализация ячеек индикаторных буферов полученными значениями 
      BullsBuffer1[bar]=BULLS1;
      BearsBuffer1[bar]=BEARS1;
      BullsBuffer2[bar]=BULLS2;
      BearsBuffer2[bar]=BEARS2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
