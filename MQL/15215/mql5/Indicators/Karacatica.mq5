//+------------------------------------------------------------------+
//|                                                   Karacatica.mq5 |
//|                                       Copyright © 2005,  Дмитрий |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net/"
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
//---- в качестве цвета медвежьего значка индикатора использован розовый цвет
#property indicator_color1  clrMagenta
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение метки медвежьей линии индикатора
#property indicator_label1  "Karacatica Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычего значка индикатора использован зеленый цвет
#property indicator_color2  clrLime
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение метки бычьей линии индикатора
#property indicator_label2 "Karacatica Buy"

//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint iPeriod=70; //период индикатора
//+----------------------------------------------+

//---- объявление динамических массивов, которые будут
//---- в дальнейшем использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//---
double s;
int StartBars;
int ATR_Handle,ADX_Handle,ltr,ltr_;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация глобальных переменных 
   s=1.5/2.0;
   StartBars=int(iPeriod)+1;
//---- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,iPeriod);
   if(ATR_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора ATR");
//---- получение хендла индикатора ADX
   ADX_Handle=iADX(NULL,0,iPeriod);
   if(ADX_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора ADX");

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Karacatica Sell");
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Karacatica Buy");
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);

//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="Karacatica";
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
                const int &spread[]
                )
  {
//---- проверка количества баров на достаточность для расчета
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(ADX_Handle)<rates_total
      || rates_total<StartBars)
      return(0);

//---- объявления локальных переменных 
   int to_copy,limit,bar;
   double ADXP[],ADXM[],ATR[];

//---- расчеты необходимого количества копируемых данных и
//---- стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      to_copy=rates_total;         // расчетное количество всех баров
      limit=rates_total-StartBars; // стартовый номер для расчета всех баров
     }
   else
     {
      to_copy=rates_total-prev_calculated+1; // расчетное количество только новых баров
      limit=rates_total-prev_calculated;     // стартовый номер для расчета новых баров
     }

//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(0);
   if(CopyBuffer(ADX_Handle,1,0,to_copy,ADXP)<=0) return(0);
   if(CopyBuffer(ADX_Handle,2,0,to_copy,ADXM)<=0) return(0);

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(ADXP,true);
   ArraySetAsSeries(ADXM,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- восстанавливаем значения переменных
   ltr=ltr_;

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0; bar--)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0)
         ltr_=ltr;

      SellBuffer[bar]=0.0;
      BuyBuffer[bar]=0.0;

      if(BuyBuffer[bar+1]!=0 && BuyBuffer[bar+1]!=EMPTY_VALUE)ltr=1;
      if(SellBuffer[bar+1]!=0 && SellBuffer[bar+1]!=EMPTY_VALUE)ltr=2;

      if(close[bar]>close[bar+iPeriod] && ADXP[bar]>ADXM[bar] && ltr!=1)BuyBuffer[bar]=low[bar]-ATR[bar]*s;
      if(close[bar]<close[bar+iPeriod] && ADXP[bar]<ADXM[bar] && ltr!=2)SellBuffer[bar]=high[bar]+ATR[bar]*s;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+