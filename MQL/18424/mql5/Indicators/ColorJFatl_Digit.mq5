//+---------------------------------------------------------------------+
//|                                                ColorJFatl_Digit.mq5 | 
//|                                 иCopyright © 2016, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "2016,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"

//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- для расчёта и отрисовки индикатора использован один буфер
#property indicator_buffers 2
//---- использовано всего одно графическое построение
#property indicator_plots   1
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_COLOR_LINE
//---- в качестве цветов трёхцветной линии использованы
#property indicator_color1  clrMagenta,clrGray,clrGold
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "ColorJFatl_Digit"
//+-----------------------------------+
//|  Описание класса CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- объявление переменной класса CJJMA из файла JJMASeries_Cls.mqh
CJJMA JMA;
//+-----------------------------------+
//|  объявление перечислений          |
//+-----------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //PRICE_CLOSE
   PRICE_OPEN_,          //PRICE_OPEN
   PRICE_HIGH_,          //PRICE_HIGH
   PRICE_LOW_,           //PRICE_LOW
   PRICE_MEDIAN_,        //PRICE_MEDIAN
   PRICE_TYPICAL_,       //PRICE_TYPICAL
   PRICE_WEIGHTED_,      //PRICE_WEIGHTED
   PRICE_SIMPL_,         //PRICE_SIMPL_
   PRICE_QUARTER_,       //PRICE_QUARTER_
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input string  SirName="ColorJFatl_Digit";     //Первая часть имени графических объектов
input int JLength=5; // глубина JMA сглаживания                   
input int JPhase=-100; // параметр JMA сглаживания,
                      //изменяющийся в пределах -100 ... +100,
//влияет на качество переходного процесса;
input Applied_price_ IPC=PRICE_CLOSE_;//ценовая константа
input int FATLShift=0; // сдвиг Фатла по горизонтали в барах
input int PriceShift=0; // cдвиг Фатла по вертикали в пунктах
input uint Digit=2;                       //количество разрядов округления
input bool ShowPrice=true; //показывать ценовые метки
//---- цвета ценовых меток
input color  Price_color=clrGray;
//+-----------------------------------+

//---- объявление и инициализация переменной для хранения количества расчётных баров
int FATLPeriod=39;

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double ExtLineBuffer[];
double ColorExtLineBuffer[];

int start,fstart,FATLSize;
double dPriceShift;
double PointPow10;
//---- Объявление стрингов для текстовых меток
string Price_name;
//+X----------------------------------------------X+ 
//| Инициализация коэффициентов цифрового фильтра  |
//+X----------------------------------------------X+ 
double dFATLTable[]=
  {
   +0.4360409450, +0.3658689069, +0.2460452079, +0.1104506886,
   -0.0054034585, -0.0760367731, -0.0933058722, -0.0670110374,
   -0.0190795053, +0.0259609206, +0.0502044896, +0.0477818607,
   +0.0249252327, -0.0047706151, -0.0272432537, -0.0338917071,
   -0.0244141482, -0.0055774838, +0.0128149838, +0.0226522218,
   +0.0208778257, +0.0100299086, -0.0036771622, -0.0136744850,
   -0.0160483392, -0.0108597376, -0.0016060704, +0.0069480557,
   +0.0110573605, +0.0095711419, +0.0040444064, -0.0023824623,
   -0.0067093714, -0.0072003400, -0.0047717710, +0.0005541115,
   +0.0007860160, +0.0130129076, +0.0040364019
  };
//+------------------------------------------------------------------+
// Описание функции iPriceSeries()                                   |
// Описание функции iPriceSeriesAlert()                              |
// Описание класса CJJMA                                             |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh>  
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали на FATLShift
   PlotIndexSetInteger(0,PLOT_SHIFT,FATLShift);
//---- инициализация переменных 
   FATLSize=ArraySize(dFATLTable);
   start=FATLSize+30;
//---- Инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
   PointPow10=_Point*MathPow(10,Digit);
//---- Инициализация стрингов
   Price_name=SirName+"Price";
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,start);
//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"ColorJFatl_Digit(",JLength," ,",JPhase,")");
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//--- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
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
   if(rates_total<start) return(0);

//---- объявления локальных переменных 
   int first,bar;
   double jfatl,FATL,trend;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=FATLPeriod-1; // стартовый номер для расчёта всех баров
      fstart=first;
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- формула для фильтра FATL
      FATL=0.0;
      for(int iii=0; iii<FATLSize; iii++) FATL+=dFATLTable[iii]*PriceSeries(IPC,bar-iii,open,low,high,close);
      jfatl=JMA.JJMASeries(fstart,prev_calculated,rates_total,0,JPhase,JLength,FATL,bar,false);
      jfatl+=dPriceShift;
      ExtLineBuffer[bar]=PointPow10*MathRound(jfatl/PointPow10);
     }
//---- корректировка значения переменной first
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first++; // стартовый номер для расчета всех баров

//---- Основной цикл раскраски сигнальной линии
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double clr=1;
      trend=ExtLineBuffer[bar]-ExtLineBuffer[bar-1];
      if(!trend) clr=ColorExtLineBuffer[bar-1];
      else
        {
         if(trend>0) clr=2;
         if(trend<0) clr=0;
        }
      ColorExtLineBuffer[bar]=clr;
     }
//----
   ChartRedraw(0);
   return(rates_total);
  }
//+------------------------------------------------------------------+
