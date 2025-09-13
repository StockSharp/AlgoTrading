//+------------------------------------------------------------------+
//|                                                     EMAAngle.mq5 |
//|                                         Copyright © 2008, jpkfox |
//|                                                                  |
//| You can use this indicator to measure when the EMA angle is      |
//| "near zero". AngleTreshold determines when the angle for the     |
//| EMA is "about zero": This is when the value is between           |
//| [-AngleTreshold, AngleTreshold] (or when the histogram is red).  |
//|   EMAPeriod: EMA period                                          |
//|   AngleTreshold: The angle value is "about zero" when it is      |
//|     between the values [-AngleTreshold, AngleTreshold].          |      
//|   StartEMAShift: The starting point to calculate the             |   
//|     angle. This is a shift value to the left from the            |
//|     observation point. Should be StartEMAShift > EndEMAShift.    | 
//|   StartEMAShift: The ending point to calculate the               |
//|     angle. This is a shift value to the left from the            | 
//|     observation point. Should be StartEMAShift > EndEMAShift.    |
//|                                                                  |
//|   Modified by MrPip                                              |
//|       Red for down                                               |
//|       Yellow for near zero                                       |
//|       Green for up                                               |
//|  10/15/05  MrPip                                                 |
//|            Corrected problem with USDJPY and optimized code      |   
//|                                                                  |
//+------------------------------------------------------------------+
#property  copyright "Copyright © 2008, jpkfox"
#property  link      "http://www.strategybuilderfx.com/forums/showthread.php?t=15274&page=1&pp=8"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде четырёхцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов пятицветной гистограммы использованы
#property indicator_color1 clrMagenta,clrPurple,clrGray,clrTeal,clrChartreuse
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение лэйбы индикатора
#property indicator_label1  "EMAAngle"
//+-----------------------------------+
//|  объявление констант              |
//+-----------------------------------+
#define RESET  0       // Константа для возврата терминалу команды на пересчёт индикатора
#define PI     3.14159 // Значение числа пи
//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input uint EMAPeriod=34;
input  ENUM_MA_METHOD   MAType=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
input double AngleTreshold=3.0;
input uint StartEMAShift=6;
input uint EndEMAShift=0;
//+-----------------------------------+

//---- Объявление целых переменных начала отсчёта данных
int  min_rates_total;
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//---- Объявление целых переменных для хендлов индикаторов
int MA_Handle;
double dFactor,mFactor;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   if(StartEMAShift<=EndEMAShift)
     {
      Print("Неверное значение входного параметров StartEMAShift и EndEMAShift!!!");
      return;
     }
   min_rates_total=int(EMAPeriod +MathMax(StartEMAShift,EndEMAShift));
   
//---- получение хендла индикатора iMA
   MA_Handle=iMA(NULL,0,EMAPeriod,0,MAType,MAPrice);
   if(MA_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iMA");
//----  
   dFactor=3.14159/180.0;
   mFactor=10000.0;
   if (Symbol()=="USDJPY") mFactor=100.0;
   double ShiftDif=StartEMAShift-EndEMAShift;
   mFactor/=ShiftDif;
   
//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorIndBuffer,true);

//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"EMAAngle("+string(EMAPeriod)+")");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
    
//---- количество  горизонтальных уровней индикатора 2   
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,+AngleTreshold);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,-AngleTreshold);
//---- в качестве цветов линий горизонтальных уровней использован розовый и синий цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrMagenta);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrBlue);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
//---- завершение инициализации
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(MA_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
      
//---- объявления локальных переменных 
   int to_copy,limit,bar,clr;
   double fEndMA,fStartMA,fAngle,MA[];

//---- расчёт стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
        limit=rates_total-min_rates_total-1; // стартовый номер для расчёта всех баров
   else limit=rates_total-prev_calculated;  // стартовый номер для расчёта только новых баров

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(High,true);
   ArraySetAsSeries(Low,true);  
   ArraySetAsSeries(MA,true); 
   
   to_copy=limit+min_rates_total+1;
   
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);
   
//---- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      fEndMA=MA[bar+EndEMAShift];
      fStartMA=MA[bar+StartEMAShift];
      //---- 10000.0 : Multiply by 10000 so that the fAngle is not too small
      //---- for the indicator Window.
      fAngle=mFactor*(fEndMA-fStartMA);
      //---- fAngle = MathArctan(fAngle)/dFactor;
      IndBuffer[bar]=fAngle;
//----
      clr=2;

      if(fAngle>0)
        {
         if(fAngle>+AngleTreshold) clr=4;
         else clr=3;
        }
        
      if(fAngle<0)
        {
         if(fAngle<-AngleTreshold) clr=0;
         else clr=1;
        }
        
      ColorIndBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
