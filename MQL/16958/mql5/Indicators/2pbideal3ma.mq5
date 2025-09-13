//+------------------------------------------------------------------+
//|                                                  2pbIdeal3MA.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2011, Nikolay Kositsin"
//---- ссылка на сайт автора
#property link "farria@mail.redcom.ru"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в основном окне
#property indicator_chart_window
//---- для расчёта и отрисовки индикатора использован один буфер
#property indicator_buffers 1
//---- использовано всего одно графическое построение
#property indicator_plots   1
//---- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета линии индикатора использован жёлтый цвет
#property indicator_color1  Yellow
//---- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width1  2
//---- отображение метки индикатора
#property indicator_label1  "2pbIdeal3MA"

//---- входные параметры индикатора
input int PeriodX1 = 10; //первое грубое усреднение
input int PeriodX2 = 10; //первое уточняющее усреднение
input int PeriodY1 = 10; //второе грубое усреднение
input int PeriodY2 = 10; //второе уточняющее усреднение
input int PeriodZ1 = 10; //третье грубое усреднение
input int PeriodZ2 = 10; //третье уточняющее усреднение
input int MAShift=0; //сдвиг мувинга по горизонтали в барах 

//---- объявление динамического массива, который будет в 
// дальнейшем использован в качестве индикаторного буфера
double ExtLineBuffer[];
//---- объявления переменных для сглаживающих констант
double wX1,wX2,wY1,wY2,wZ1,wZ2;
//---- объявления переменных для хранения результатов усреднения
double Moving01_,Moving11_,Moving21_;
//+------------------------------------------------------------------+
//|  Усреднение от Neutron                                           |
//+------------------------------------------------------------------+
double GetIdealMASmooth
(
 double W1_,//первая сглаживающая константа
 double W2_,//вторая сглаживающая константа
 double Series1,//значение тамсерии с текущего бара 
 double Series0,//значение тамсерии с предыдущего бара 
 double Resalt1 //значение мувинга с предыдущего бара
 )
  {
//----
   double Resalt0,dSeries,dSeries2;
   dSeries=Series0-Series1;
   dSeries2=dSeries*dSeries-1.0;

   Resalt0=(W1_ *(Series0-Resalt1)+
            Resalt1+W2_*Resalt1*dSeries2)
   /(1.0+W2_*dSeries2);
//----
   return(Resalt0);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализации переменных
   wX1=1.0/PeriodX1;
   wX2=1.0/PeriodX2;
   wY1=1.0/PeriodY1;
   wY2=1.0/PeriodY2;
   wZ1=1.0/PeriodZ1;
   wZ2=1.0/PeriodZ2;
//---- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора по горизонтали наMAShift
   PlotIndexSetInteger(0,PLOT_SHIFT,MAShift);
//---- установка позиции, с которой начинается отрисовка индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,1);
//---- инициализации переменной для короткого имени индикатора
   string shortname="2pbIdeal3MA";
//---- создание метки для отображения в Окне данных
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,     // количество истории в барах на текущем тике
                const int prev_calculated, // количество истории в барах на предыдущем тике
                const int begin,           // номер начала достоверного отсчёта баров
                const double &price[]      // ценовой массив для расчёта индикатора
                )
  {
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<1+begin) return(0);

//---- объявления локальных переменных 
   int first,bar;
   double Moving00,Moving10,Moving20;
   double Moving01,Moving11,Moving21;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=1+begin;  // стартовый номер для расчёта всех баров
      //---- увеличим позицию начала данных на begin баров, вследствие расчетов на данных другого индикатора
      if(begin>0)
         PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,begin+1);

      //---- стартовая инициализация  
      ExtLineBuffer[begin]=price[begin];
      Moving01_=price[begin];
      Moving11_=price[begin];
      Moving21_=price[begin];
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- восстанавливаем значения переменных
   Moving01=Moving01_;
   Moving11=Moving11_;
   Moving21=Moving21_;

//---- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total; bar++)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==rates_total-1)
        {
         Moving01_=Moving01;
         Moving11_=Moving11;
         Moving21_=Moving21;
        }

      Moving00=GetIdealMASmooth(wX1,wX2,price[bar-1],price[bar],Moving01);                    
      Moving10=GetIdealMASmooth(wY1,wY2,Moving01,    Moving00,  Moving11);
      Moving20=GetIdealMASmooth(wZ1,wZ2,Moving11,    Moving10,  Moving21);
      //----                       
      Moving01 = Moving00;
      Moving11 = Moving10;
      Moving21 = Moving20;
      //---- 
      ExtLineBuffer[bar]=Moving20;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
