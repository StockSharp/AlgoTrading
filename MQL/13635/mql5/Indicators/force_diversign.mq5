//+------------------------------------------------------------------+
//|                                              Force_DiverSign.mq5 | 
//|                                       Copyright © 2015, olegok83 | 
//|                           https://www.mql5.com/ru/users/olegok83 | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2015, olegok83"
#property link "https://www.mql5.com/ru/users/olegok83"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//| Параметры отрисовки медвежьего индикатора    |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//--- в качестве цвета медвежьей линии индикатора использован Gold цвет
#property indicator_color1  clrGold
//--- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//--- отображение медвежьей метки индикатора
#property indicator_label1  "Force_DiverSign Sell"
//+----------------------------------------------+
//| Параметры отрисовки бычьего индикатора       |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//--- в качестве цвета бычьей линии индикатора использован Aqua цвет
#property indicator_color2  clrAqua
//--- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//--- отображение бычьей метки индикатора
#property indicator_label2 "Force_DiverSign Buy"
//+-----------------------------------+
//| Объявление констант               |
//+-----------------------------------+
#define RESET  0 // константа для возврата терминалу команды на пересчет индикатора
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input  ENUM_MA_METHOD   MAType1=MODE_EMA;          // Метод усреднения быстрого индикатора
input uint iPeriod1=3;                             // Период быстрого индикатора
input  ENUM_MA_METHOD   MAType2=MODE_EMA;          // Метод усреднения медленого индикатора
input uint iPeriod2=7;                             // Период медленого индикатора
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // Объем
input int Shift=0;                                 // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- объявление динамических массивов, которые будут в 
//---- дальнейшем использованы в качестве индикаторных буферов
double SellBuffer[],BuyBuffer[];
//--- объявление целочисленных переменных для хендлов индикаторов
int ATR_Handle,Ind_Handle1,Ind_Handle2;
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total,min_rates_;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//---- инициализация глобальных переменных 
   int ATR_Period=10;
//---- инициализация переменных начала отсчета данных
   min_rates_=int(MathMax(iPeriod1,iPeriod2));
   min_rates_total=min_rates_+int(MathMax(iPeriod1,iPeriod2))+5;
   min_rates_total=int(MathMax(min_rates_total,ATR_Period));
//---- получение хендла индикатора ATR
   ATR_Handle=iATR(Symbol(),PERIOD_CURRENT,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора Force1
   Ind_Handle1=iForce(Symbol(),PERIOD_CURRENT,iPeriod1,MAType1,VolumeType);
   if(Ind_Handle1==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Force1");
      return(INIT_FAILED);
     }
//--- получение хендла индикатора Force2
   Ind_Handle2=iForce(Symbol(),PERIOD_CURRENT,iPeriod2,MAType2,VolumeType);
   if(Ind_Handle2==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Force2");
      return(INIT_FAILED);
     }
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,174);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,174);
//---- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//---- инициализация переменной для короткого имени индикатора
   string shortname="Force_DiverSign";
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| Custom indicator iteration function                              | 
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
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(Ind_Handle1)<rates_total
      || BarsCalculated(Ind_Handle2)<rates_total
      || rates_total<min_rates_total) return(RESET);
//---- объявление переменных с плавающей точкой  
   double Ind1[],Ind2[],ATR[];
//---- объявление целочисленных переменных и получение уже посчитанных баров
   int to_copy,limit,bar;
//---- расчет стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
     }
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
//----
   to_copy=limit+1;
//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(Ind1,true);
   ArraySetAsSeries(Ind2,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   to_copy+=4;
   if(CopyBuffer(Ind_Handle1,0,0,to_copy,Ind1)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle2,0,0,to_copy,Ind2)<=0) return(RESET);
//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //----
      if(SellCheck(open,close,bar))
         if(Ind1[bar+4]<Ind1[bar+3] && Ind1[bar+3]>Ind1[bar+2] && Ind1[bar+2]<Ind1[bar+1])
            if(Ind2[bar+4]<Ind2[bar+3] && Ind2[bar+3]>Ind2[bar+2] && Ind2[bar+2]<Ind2[bar+1])
              {
               if((Ind1[bar+3]>Ind1[bar+1] && Ind2[bar+3]<Ind2[bar+1])
                  || (Ind1[bar+3]<Ind1[bar+1] && Ind2[bar+3]>Ind2[bar+1])) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
              }
      //----
      if(BuyCheck(open,close,bar))
         if(Ind1[bar+4]>Ind1[bar+3] && Ind1[bar+3]<Ind1[bar+2] && Ind1[bar+2]>Ind1[bar+1])
            if(Ind2[bar+4]>Ind2[bar+3] && Ind2[bar+3]<Ind2[bar+2] && Ind2[bar+2]>Ind2[bar+1])
              {
               if((Ind1[bar+3]>Ind1[bar+1] && Ind2[bar+3]<Ind2[bar+1])
                  || (Ind1[bar+3]<Ind1[bar+1] && Ind2[bar+3]>Ind2[bar+1])) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
              }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| Проверка на наличие красной свечи между зелеными свечками        |
//+------------------------------------------------------------------+  
bool SellCheck(const double &Open[],const double &Close[],int index)
  {
//--- превращение динамического массива в цветовой индексный буфер
   if(Open[index+3]<Close[index+3] && Open[index+2]>Close[index+2] && Open[index+1]<Close[index+1]) return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Проверка на наличие зеленой свечи между красными свечками        |
//+------------------------------------------------------------------+  
bool BuyCheck(const double &Open[],const double &Close[],int index)
  {
//--- превращение динамического массива в цветовой индексный буфер
   if(Open[index+3]>Close[index+3] && Open[index+2]<Close[index+2] && Open[index+1]>Close[index+1]) return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
