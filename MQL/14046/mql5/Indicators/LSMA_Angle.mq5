//+------------------------------------------------------------------+
//|                                                   LSMA_Angle.mq5 |
//|                                                           MrPip  |
//|                                                                  |
//| You can use this indicator to measure when the LSMA angle is     |
//| "near zero". AngleTreshold determines when the angle for the     |
//| LSMA is "about zero": This is when the value is between          |
//| [-AngleTreshold, AngleTreshold] (or when the histogram is red).  |
//|   LSMAPeriod: LSMA period                                        |
//|   AngleTreshold: The angle value is "about zero" when it is      |
//|     between the values [-AngleTreshold, AngleTreshold].          |      
//|   StartLSMAShift: The starting point to calculate the            |   
//|     angle. This is a shift value to the left from the            |
//|     observation point. Should be StartEMAShift > EndEMAShift.    | 
//|   EndLSMAShift: The ending point to calculate the                |
//|     angle. This is a shift value to the left from the            | 
//|     observation point. Should be StartEMAShift > EndEMAShift.    |
//|                                                                  |
//|   Modified by MrPip from EMAAngle by jpkfox                      |
//|       Red for down                                               |
//|       Yellow for near zero                                       |
//|       Green for up                                               |   
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, Robert L. Hill aka MrPip"
#property link "" 
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//|  Параметры отрисовки индикатора   |
//+-----------------------------------+
//---- отрисовка индикатора в виде четырёхцветной гистограммы
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- в качестве цветов четырёхцветной гистограммы использованы
#property indicator_color1 clrMagenta,clrViolet,clrGray,clrDodgerBlue,clrBlue
//---- линия индикатора - сплошная
#property indicator_style1 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1 2
//---- отображение метки индикатора
#property indicator_label1 "LSMA_Angle"

//+-----------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА     |
//+-----------------------------------+
input uint LSMAPeriod=25;
input int  AngleTreshold=15; // Порог срабатывания
input uint StartLSMAShift=4;
input uint EndLSMAShift=0;
//+-----------------------------------+
double mFactor,dFactor,ShiftDif;
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double IndBuffer[],ColorIndBuffer[];
//+------------------------------------------------------------------+
//|  LSMA - Least Squares Moving Average function calculation        |
//+------------------------------------------------------------------+
double LSMA(int Rperiod,const double &Close[],int shift)
  {
//----
   int iii;
   double sum;
   int length;
   double lengthvar;
   double tmp;
   double wt;
//----
   length=Rperiod;
//---- 
   sum=0;
   for(iii=length; iii>=1; iii--)
     {
      lengthvar = length+1;
      lengthvar/= 3;
      tmp=(iii-lengthvar)*Close[length-iii+shift];
      sum+=tmp;
     }
   wt=sum*6/(length*(length+1));
//----
   return(wt);
  }
//+------------------------------------------------------------------+    
//| LSMA_Angle indicator initialization function                     | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=int(MathMax(LSMAPeriod+StartLSMAShift,LSMAPeriod+EndLSMAShift));
   dFactor = 2*3.14159/180.0;
   mFactor = 100000.0;
   string Sym = StringSubstr(Symbol(),3,3);
   if (Sym == "JPY") mFactor = 1000.0;
   ShiftDif = StartLSMAShift-EndLSMAShift;
   mFactor /= ShiftDif; 

   if(EndLSMAShift>=StartLSMAShift)
     {
      Print("Error: EndLSMAShift >= StartLSMAShift");
      return(INIT_FAILED);
     }

//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(IndBuffer,true);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorIndBuffer,true);
   
//---- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);

//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,"LSMA_Angle");
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
   
//---- количество  горизонтальных уровней индикатора 9   
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,+AngleTreshold);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,-AngleTreshold);
//---- в качестве цветов линий горизонтальных уровней использованы серый и розовый цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrTeal);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrTeal);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);

//---- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| LSMA_Angle iteration function                                    | 
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
//---- Проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- объявления локальных переменных 
   int limit,bar,clr;
   double fEndMA,fStartMA,fAngle;

//---- расчёт стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-1-min_rates_total; // стартовый номер для расчёта всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
     }
    
//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(close,true);

//---- Основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      fEndMA=LSMA(LSMAPeriod,close,bar+EndLSMAShift);
      fStartMA=LSMA(LSMAPeriod,close,bar+StartLSMAShift);
      fAngle=mFactor*(fEndMA-fStartMA)/2;
      IndBuffer[bar]=fAngle;
     }

   if(prev_calculated>rates_total || prev_calculated<=0) limit--;
//---- Основной цикл раскраски индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      clr=2;

      if(IndBuffer[bar]>AngleTreshold)
        {
         if(IndBuffer[bar]>IndBuffer[bar+1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar+1]) clr=3;
        }

      if(IndBuffer[bar]<-AngleTreshold)
        {
         if(IndBuffer[bar]<IndBuffer[bar+1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar+1]) clr=1;
        }

      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
