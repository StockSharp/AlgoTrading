//+------------------------------------------------------------------+
//|                                                  WPRSIsignal.mq5 |
//|                                         Copyright © 2009, gumgum |
//|                                           1967policmen@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, gumgum"
//---- ссылка на сайт автора
#property link      "1967policmen@gmail.com"
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
//---- в качестве цвета медвежьего индикатора использован цвет Magenta
#property indicator_color1  clrMagenta
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение метки индикатора
#property indicator_label1  "WPRSI signal Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычего индикатора использован цвет Lime
#property indicator_color2  clrLime
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение метки индикатора
#property indicator_label2 "WPRSI signal Buy"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int WPRSI_period=27; //Период индикатора
input int filterUP=10; //глубина поиска для лонгов
input int filterDN=10; //глубина поиска для шортов
//+----------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//---
int WPR_Handle,RSI_Handle;
int min_rates_total,FilterMax;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация переменных начала отсчета данных 
   FilterMax=2+MathMax(filterUP,filterDN);
   min_rates_total=WPRSI_period+FilterMax;

//---- получение хендла индикатора WPR
   WPR_Handle=iWPR(NULL,0,WPRSI_period);
   if(WPR_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора WPR");

//---- получение хендла индикатора Stochastic
   RSI_Handle=iRSI(NULL,0,WPRSI_period,PRICE_CLOSE);
   if(RSI_Handle==INVALID_HANDLE)Print(" Не удалось получить хендл индикатора RSI");

//---- превращение динамического массива SellBuffer[] в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);

//---- превращение динамического массива BuyBuffer[] в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);

//---- установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="WPRSI signal";
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
   if(BarsCalculated(WPR_Handle)<rates_total
      || BarsCalculated(RSI_Handle)<rates_total
      || rates_total<min_rates_total)
      return(0);

//---- объявления локальных переменных 
   int to_copy,limit,bar;
   double WPR[],RSI[];

//---- расчеты необходимого количества копируемых данных
//---- и стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-min_rates_total-1; // расчетное количество всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }

//---- копируем вновь появившиеся данные в массивы  
   to_copy=limit+1;
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI)<=0) return(0);
   to_copy+=FilterMax;
   if(CopyBuffer(WPR_Handle,0,0,to_copy,WPR)<=0) return(0);

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(WPR,true);
   ArraySetAsSeries(RSI,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0; bar--)
     {
      BuyBuffer[bar]=EMPTY_VALUE;
      SellBuffer[bar]=EMPTY_VALUE;

      if(WPR[bar]>-20 && WPR[bar+1]<-20 && RSI[bar]>50)
        {
         double z=0;
         for(int k=2;k<=filterUP+2;k++) if(WPR[bar+k]>-20) z=1;

         if(z==0) BuyBuffer[bar]=low[bar]-(high[bar]-low[bar])/2;
        }

      if(WPR[bar+1]>-80 && WPR[bar]<-80 && RSI[bar]<50)
        {
         double h=0;
         for(int c=2;c<=filterDN+2;c++) if(WPR[bar+c]<-80) h=1;

         if(h==0) SellBuffer[bar]=high[bar]+(high[bar]-low[bar])/2;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
