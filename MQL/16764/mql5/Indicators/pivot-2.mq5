//+------------------------------------------------------------------+ 
//|                                                      Pivot-2.mq5 | 
//|                                       Copyright © 2004, Aborigen | 
//|                                          http://forex.kbpauk.ru/ | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2004, Aborigen"
#property link "http://forex.kbpauk.ru/"
//--- номер версии индикатора
#property version   "1.00"
#property description "Линии сопротивлений и поддержки"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window
//--- количество индикаторных буферов 7
#property indicator_buffers 7 
//--- использовано всего семь графических построений
#property indicator_plots   7
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET 0                      // Константа для возврата терминалу команды на пересчет индикатора
#define INDICATOR_NAME "Pivot-2"     // Константа для имени индикатора
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//--- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//--- в качестве цвета индикатора использован
#property indicator_color1  clrTeal
//--- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//--- толщина линии индикатора 1 равна 2
#property indicator_width1  2
//--- отображение метки индикатора
#property indicator_label1  "Res 3"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 2            |
//+----------------------------------------------+
//--- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//--- в качестве цвета индикатора использован
#property indicator_color2  clrDodgerBlue
//--- линия индикатора - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//--- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//--- отображение метки индикатора
#property indicator_label2  "Res 2"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 3            |
//+----------------------------------------------+
//--- отрисовка индикатора 3 в виде линии
#property indicator_type3   DRAW_LINE
//--- в качестве цвета индикатора использован
#property indicator_color3  clrLime
//--- линия индикатора - непрерывная кривая
#property indicator_style3  STYLE_SOLID
//--- толщина линии индикатора 3 равна 2
#property indicator_width3  2
//--- отображение метки индикатора
#property indicator_label3  "Res 1"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 4            |
//+----------------------------------------------+
//--- отрисовка индикатора 4 в виде линии
#property indicator_type4   DRAW_LINE
//--- в качестве цвета индикатора использован
#property indicator_color4  clrBlueViolet
//--- линия индикатора - непрерывная кривая
#property indicator_style4  STYLE_SOLID
//--- толщина линии индикатора 4 равна 4
#property indicator_width4  4
//--- отображение метки индикатора
#property indicator_label4  "Pivot"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 5            |
//+----------------------------------------------+
//--- отрисовка индикатора 5 в виде линии
#property indicator_type5   DRAW_LINE
//--- в качестве цвета индикатора использован
#property indicator_color5  clrRed
//--- линия индикатора - непрерывная кривая
#property indicator_style5  STYLE_SOLID
//--- толщина линии индикатора 5 равна 2
#property indicator_width5  2
//--- отображение метки индикатора
#property indicator_label5  "Sup 1"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 6            |
//+----------------------------------------------+
//--- отрисовка индикатора 6 в виде линии
#property indicator_type6   DRAW_LINE
//--- в качестве цвета индикатора использован
#property indicator_color6  clrMagenta
//--- линия индикатора - непрерывная кривая
#property indicator_style6  STYLE_SOLID
//--- толщина линии индикатора 6 равна 2
#property indicator_width6  2
//--- отображение метки индикатора
#property indicator_label6  "Sup 2"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 7            |
//+----------------------------------------------+
//--- отрисовка индикатора 7 в виде линии
#property indicator_type7   DRAW_LINE
//--- в качестве цвета индикатора использован
#property indicator_color7  clrBrown
//--- линия индикатора - непрерывная кривая
#property indicator_style7  STYLE_SOLID
//--- толщина линии индикатора 7 равна 2
#property indicator_width7  2
//--- отображение метки индикатора
#property indicator_label7  "Sup 3"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int Shift=0;                 // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double Ind1Buffer[];
double Ind2Buffer[];
double Ind3Buffer[];
double Ind4Buffer[];
double Ind5Buffer[];
double Ind6Buffer[];
double Ind7Buffer[];
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- проверка таймфрейма индикатора на корректность
   if(!TimeFramesCheck(INDICATOR_NAME,Period())) return(INIT_FAILED);
//--- инициализация переменных 
   min_rates_total=2*PeriodSeconds(PERIOD_D1)/PeriodSeconds(Period());
//--- инициализация индикаторных буферов
   IndInit(0,Ind1Buffer,0.0,min_rates_total,Shift);
   IndInit(1,Ind2Buffer,0.0,min_rates_total,Shift);
   IndInit(2,Ind3Buffer,0.0,min_rates_total,Shift);
   IndInit(3,Ind4Buffer,0.0,min_rates_total,Shift);
   IndInit(4,Ind5Buffer,0.0,min_rates_total,Shift);
   IndInit(5,Ind6Buffer,0.0,min_rates_total,Shift);
   IndInit(6,Ind7Buffer,0.0,min_rates_total,Shift);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   string shortname;
   StringConcatenate(shortname,INDICATOR_NAME,"(",Shift,")");
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+    
void OnDeinit(const int reason)
  {
//---
   Comment("");
//---
  }
//+------------------------------------------------------------------+  
//| Custom iteration function                                        | 
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
//--- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(RESET);
//--- объявление целочисленных переменных
   int limit,bar;
//--- объявление переменных с плавающей точкой  
   double P,S1,R1,S2,R2,S3,R3;
   static double LastHigh,LastLow;
//---    
   datetime iTime[1];
   static uint LastCountBar;
//--- расчеты необходимого количества копируемых данных
//--- и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // стартовый номер для расчета всех баров
      LastCountBar=rates_total;
      LastHigh=0;
      LastLow=999999999;
     }
   else limit=int(LastCountBar)+rates_total-prev_calculated; // стартовый номер для расчета новых баров 
//--- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(time,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(open,true);
//--- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Ind1Buffer[bar]=0.0;
      Ind2Buffer[bar]=0.0;
      Ind3Buffer[bar]=0.0;
      Ind4Buffer[bar]=0.0;
      Ind5Buffer[bar]=0.0;
      Ind6Buffer[bar]=0.0;
      Ind7Buffer[bar]=0.0;

      if(high[bar+1]>LastHigh) LastHigh=high[bar+1];
      if(low[bar+1]<LastLow) LastLow=low[bar+1];
      //--- копируем вновь появившиеся данные в массив
      if(CopyTime(Symbol(),PERIOD_D1,time[bar],1,iTime)<=0) return(RESET);

      if(time[bar]>=iTime[0] && time[bar+1]<iTime[0])
        {
         LastCountBar=bar;
         Ind1Buffer[bar+1]=0.0;
         Ind2Buffer[bar+1]=0.0;
         Ind3Buffer[bar+1]=0.0;
         Ind4Buffer[bar+1]=0.0;
         Ind5Buffer[bar+1]=0.0;
         Ind6Buffer[bar+1]=0.0;
         Ind7Buffer[bar+1]=0.0;

         P=(LastHigh+LastLow+close[bar+1])/3;
         double P2=2*P;
         R1=P2-LastLow;
         S1=P2-LastHigh;
         double diff=LastHigh-LastLow;
         R2=P+diff;
         S2=P-diff;
         R3=P2+(LastHigh-2*LastLow);
         S3=P2-(2*LastHigh-LastLow);
         LastLow=open[bar];
         LastHigh=open[bar];
         //--- загрузка полученных значений в индикаторные буферы
         Ind1Buffer[bar]=R3;
         Ind2Buffer[bar]=R2;
         Ind3Buffer[bar]=R1;
         Ind4Buffer[bar]=P;
         Ind5Buffer[bar]=S1;
         Ind6Buffer[bar]=S2;
         Ind7Buffer[bar]=S3;
         //--- печать комментария
         Comment("\n",
                 "Res3=",DoubleToString(R3,_Digits),"\n",
                 "Res2=",DoubleToString(R2,_Digits),"\n",
                 "Res1=",DoubleToString(R1,_Digits),"\n",
                 "Pivot=",DoubleToString(P,_Digits),"\n",
                 "Sup1=",DoubleToString(S1,_Digits),"\n",
                 "Sup2=",DoubleToString(S2,_Digits),"\n",
                 "Sup3=",DoubleToString(S3,_Digits));
        }
      if(Ind1Buffer[bar+1] && !Ind1Buffer[bar])
        {
         Ind1Buffer[bar]=Ind1Buffer[bar+1];
         Ind2Buffer[bar]=Ind2Buffer[bar+1];
         Ind3Buffer[bar]=Ind3Buffer[bar+1];
         Ind4Buffer[bar]=Ind4Buffer[bar+1];
         Ind5Buffer[bar]=Ind5Buffer[bar+1];
         Ind6Buffer[bar]=Ind6Buffer[bar+1];
         Ind7Buffer[bar]=Ind7Buffer[bar+1];
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| Инициализация индикаторного буфера                               |
//+------------------------------------------------------------------+    
void IndInit(int Number,double &Buffer[],double Empty_Value,int Draw_Begin,int nShift)
  {
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(Number,Buffer,INDICATOR_DATA);
//--- осуществление сдвига начала отсчета отрисовки индикатора
   PlotIndexSetInteger(Number,PLOT_DRAW_BEGIN,Draw_Begin);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(Number,PLOT_EMPTY_VALUE,Empty_Value);
//--- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(Number,PLOT_SHIFT,nShift);
//--- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(Buffer,true);
//---
  }
//+------------------------------------------------------------------+
//| TimeFramesCheck()                                                |
//+------------------------------------------------------------------+    
bool TimeFramesCheck(string IndName,
                     ENUM_TIMEFRAMES TFrame)//Период графика индикатора
  {
//--- проверка периодов графиков на корректность
   if(TFrame>=PERIOD_H12)
     {
      Print("Период графика для индикатора "+IndName+" не может быть ,больше H12");
      return(RESET);
     }
//---
   return(true);
  }
//+------------------------------------------------------------------+
