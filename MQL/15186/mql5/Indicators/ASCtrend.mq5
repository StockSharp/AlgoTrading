//+------------------------------------------------------------------+
//|                                                     ASCtrend.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "ASCtrend"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован розовый цвет
#property indicator_color1  clrMagenta
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение бычьей метки индикатора
#property indicator_label1  "ASCtrend Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьего индикатора      |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычьей линии индикатора использован синий цвет
#property indicator_color2  clrBlue
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение медвежьей метки индикатора
#property indicator_label2 "ASCtrend Buy"

//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET  0 // Константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int RISK=4;
//+----------------------------------------------+

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//---- объявление целых переменных начала отсчета данных
int min_rates_total;
int  x1,x2,value10,value11,WPR_Handle[3];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация глобальных переменных   
   x1=67+RISK;
   x2=33-RISK;
   value10=2;
   value11=value10;
   min_rates_total=int(MathMax(3+RISK*2,4)+1);

//---- получение хендла индикатора iWPR 1
   WPR_Handle[0]=iWPR(NULL,0,3);
   if(WPR_Handle[0]==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора iWPR 1");
//---- получение хендла индикатора iWPR 2
   WPR_Handle[1]=iWPR(NULL,0,4);
   if(WPR_Handle[1]==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора iWPR 2");
//---- получение хендла индикатора iWPR 3
   WPR_Handle[2]=iWPR(NULL,0,3+RISK*2);
   if(WPR_Handle[2]==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора iWPR 3");

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"ASCtrend Sell");
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(SellBuffer,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"ASCtrend Buy");
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);

//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="ASCtrend";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
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
   if(BarsCalculated(WPR_Handle[0])<rates_total
      || BarsCalculated(WPR_Handle[1])<rates_total
      || BarsCalculated(WPR_Handle[2])<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- объявления локальных переменных 
   int limit,bar,count,iii;
   double value2,value3,Vel=0,WPR[];
   double TrueCount,Range,AvgRange,MRO1,MRO2;

//---- расчеты необходимого количества копируемых данных и
//стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
      limit=rates_total-min_rates_total; // стартовый номер для расчета всех баров
   else limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров

//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(WPR,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Range=0.0;
      AvgRange=0.0;
      for(count=bar; count<=bar+9; count++) AvgRange=AvgRange+MathAbs(high[count]-low[count]);

      Range=AvgRange/10;
      count=bar;
      TrueCount=0;

      while(count<bar+9 && TrueCount<1)
        {
         if(MathAbs(open[count]-close[count+1])>=Range*2.0) TrueCount++;
         count++;
        }

      if(TrueCount>=1) MRO1=count;
      else             MRO1=-1;

      count=bar;
      TrueCount=0;

      while(count<bar+6 && TrueCount<1)
        {
         if(MathAbs(close[count+3]-close[count])>=Range*4.6) TrueCount++;
         count++;
        }

      if(TrueCount>=1) MRO2=count;
      else             MRO2=-1;

      if(MRO1>-1) {value11=0;} else {value11=value10;}
      if(MRO2>-1) {value11=1;} else {value11=value10;}

      if(CopyBuffer(WPR_Handle[value11],0,bar,1,WPR)<=0) return(RESET);

      value2=100-MathAbs(WPR[0]); // PercentR(value11=9)

      SellBuffer[bar]=0;
      BuyBuffer[bar]=0;

      value3=0;

      if(value2<x2)
        {
         iii=1;
         while(bar+iii<rates_total)
           {
            if(CopyBuffer(WPR_Handle[value11],0,bar+iii,1,WPR)<=0) return(RESET);
            Vel=100-MathAbs(WPR[0]);
            if(Vel>=x2 && Vel<=x1) iii++;
            else break;
           }

         if(Vel>x1)
           {
            value3=high[bar]+Range*0.5;
            SellBuffer[bar]=value3;
           }
        }
      if(value2>x1)
        {
         iii=1;
         while(bar+iii<rates_total)
           {
            if(CopyBuffer(WPR_Handle[value11],0,bar+iii,1,WPR)<=0) return(RESET);
            Vel=100-MathAbs(WPR[0]);
            if(Vel>=x2 && Vel<=x1) iii++;
            else break;
           }

         if(Vel<x2)
           {
            value3=low[bar]-Range*0.5;
            BuyBuffer[bar]=value3;
           }
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
