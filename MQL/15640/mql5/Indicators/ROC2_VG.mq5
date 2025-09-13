//+------------------------------------------------------------------+
//|                                                      ROC2_VG.mq5 |
//|                         Copyright © 2006, Vladislav Goshkov (VG) |
//|                                                      4vg@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, Vladislav Goshkov (VG)"
#property link      "4vg@mail.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- количество индикаторных буферов 2
#property indicator_buffers 2 
//---- использовано всего одно графическое построение
#property indicator_plots   1
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цветов облака
#property indicator_color1  clrForestGreen,clrOrangeRed
//---- отображение метки индикатора
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
enum ENUM_TYPE
  {
   MOM=1,  //MOM
   ROC,    //ROC
   ROCP,   //ROCP
   ROCR,   //ROC
   ROCR100 //ROCR100
  };
//+----------------------------------------------+
//|  ВХОДНЫЕ ПАРАМЕТРЫ ИНДИКАТОРА                |
//+----------------------------------------------+
input uint ROCPeriod1=8;
input ENUM_TYPE ROCType1=MOM;
input uint ROCPeriod2=14;
input ENUM_TYPE ROCType2=MOM;
input int Shift=0;                               // сдвиг индикатора по горизонтали в барах
input double Livel1=+0.005;
input double Livel2=+0.002;
input double Livel3=0.00;
input double Livel4=-0.002;
input double Livel5=-0.005;
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double IndBuffer1[];
double IndBuffer2[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total,min_rates_1,min_rates_2;
//---- Объявление целых переменных для хендлов индикаторов
int RSI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=int(MathMax(ROCPeriod1,ROCPeriod2));
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer1,INDICATOR_DATA);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,IndBuffer2,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- инициализации переменной для короткого имени индикатора
   string short_name="ROC2_VG( "+EnumToString(ROCType1)+" = "+string(ROCPeriod1)+" ,"+EnumToString(ROCType2)+" = "+string(ROCPeriod2)+")";
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- количество  горизонтальных уровней индикатора 3   
   IndicatorSetInteger(INDICATOR_LEVELS,5);
//---- значения горизонтальных уровней индикатора   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,Livel1);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,Livel2);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,Livel3);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,3,Livel4);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,4,Livel5);
//---- в качестве цветов линий горизонтальных уровней использованы цвета  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrDodgerBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,3,clrRed);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,4,clrMagenta);
//---- в линии горизонтального уровня использован короткий штрих-пунктир  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASH);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,3,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,4,STYLE_DASHDOTDOT);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчёта индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчёта индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(RESET);

//---- Объявление переменных с плавающей точкой  
   double price,prevPrice;
//---- Объявление целых переменных
   int first,bar;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first=min_rates_total; // стартовый номер для расчёта всех баров
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- Основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price     = close[bar];
      prevPrice = close[bar-ROCPeriod1];
      switch(ROCType1)
        {
         case MOM : IndBuffer1[bar]= (price - prevPrice);             break; //"MOM"
         case ROC : IndBuffer1[bar]= ((price/prevPrice)-1)*100;       break; //"ROC"
         case ROCP : IndBuffer1[bar]= (price-prevPrice)/prevPrice;    break; //"ROCP"
         case ROCR : IndBuffer1[bar]= (price/prevPrice);              break; //"ROCR"
         case ROCR100 : IndBuffer1[bar]= (price/prevPrice)*100;       break; //"ROCR100"
         default: IndBuffer1[bar]=(price-prevPrice)/prevPrice;       break;
        }

      prevPrice=close[bar-ROCPeriod2];
      switch(ROCType2)
        {
         case MOM : IndBuffer2[bar]= (price - prevPrice);             break; //"MOM"
         case ROC : IndBuffer2[bar]= ((price/prevPrice)-1)*100;       break; //"ROC"
         case ROCP : IndBuffer2[bar]= (price-prevPrice)/prevPrice;    break; //"ROCP"
         case ROCR : IndBuffer2[bar]= (price/prevPrice);              break; //"ROCR"
         case ROCR100 : IndBuffer2[bar]= (price/prevPrice)*100;       break; //"ROCR100"
         default: IndBuffer2[bar]=(price-prevPrice)/prevPrice;       break;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
