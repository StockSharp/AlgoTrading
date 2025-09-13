//+------------------------------------------------------------------+
//|                                           SilverTrend_Signal.mq5 |
//|                                        Ramdass - Conversion only |
//+------------------------------------------------------------------+
#property copyright "SilverTrend  rewritten by CrazyChart"
#property link      "http://viac.ru/"
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
//---- в качестве цвета медвежьей линии индикатора использован цвет Red
#property indicator_color1  clrRed
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение метки медвежьей линии индикатора
#property indicator_label1  "Silver Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьего индикатора      |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычьей линии индикатора использован цвет Lime
#property indicator_color2  clrLime
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение метки бычьей линии индикатора
#property indicator_label2 "Silver Buy"

//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int SSP=9;
input int RISK=3;                                //степень риска
input int NumberofAlerts=2;
//+----------------------------------------------+

//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//----
int K;
int counter=0;
bool old,uptrend_;
//---- объявление целых переменных начала отсчета данных
int StartBars;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   StartBars=SSP+1;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Silver Sell");
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(SellBuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Silver Buy");
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);

//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="SilverTrend_Signal";
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
   if(rates_total<StartBars) return(0);

//---- объявления локальных переменных 
   int limit;
   double Range,AvgRange,smin,smax,SsMax,SsMin,price;
   bool uptrend;

//---- расчеты необходимого количества копируемых данных
//---- и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      K=33-RISK;
      limit=rates_total-StartBars;       // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }

//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- восстанавливаем значения переменных
   uptrend=uptrend_;

//---- основной цикл расчета индикатора
   for(int bar=limit; bar>=0; bar--)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0)
        {
         uptrend_=uptrend;
        }

      Range=0;
      AvgRange=0;
      for(int iii=bar; iii<=bar+SSP; iii++) AvgRange=AvgRange+MathAbs(high[iii]-low[iii]);
      Range=AvgRange/(SSP+1);
      //----
      SsMax=low[bar];
      SsMin=close[bar];

      for(int kkk=bar; kkk<=bar+SSP-1; kkk++)
        {
         price=high[kkk];
         if(SsMax<price) SsMax=price;
         price=low[kkk];
         if(SsMin>=price) SsMin=price;
        }

      smin=SsMin+(SsMax-SsMin)*K/100;
      smax=SsMax-(SsMax-SsMin)*K/100;

      SellBuffer[bar]=0;
      BuyBuffer[bar]=0;

      if(close[bar]<smin) uptrend=false;
      if(close[bar]>smax) uptrend=true;

      if(uptrend!=old && uptrend==true)
        {
         BuyBuffer[bar]=low[bar]-Range*0.5;

         if(bar==0)
           {
            if(counter<=NumberofAlerts)
              {
               Alert("Silver Trend ",EnumToString(Period())," ",Symbol()," BUY");
               counter++;
              }
           }
         else counter=0;
        }
      if(uptrend!=old && uptrend==false)
        {
         SellBuffer[bar]=high[bar]+Range*0.5;

         if(bar==0)
           {
            if(counter<=NumberofAlerts)
              {
               Alert("Silver Trend ",EnumToString(Period())," ",Symbol()," SELL");
               counter++;
              }
           }
         else counter=0;
        }

      if(bar>0) old=uptrend;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
