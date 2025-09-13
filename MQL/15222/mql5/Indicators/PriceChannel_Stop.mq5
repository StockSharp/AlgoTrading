//+------------------------------------------------------------------+
//|                                            PriceChannel_Stop.mq5 | 
//|                           Copyright © 2005, TrendLaboratory Ltd. | 
//|                                       E-mail: igorad2004@list.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, TrendLaboratory Ltd." 
//---- ссылка на сайт автора
#property link "E-mail: igorad2004@list.ru" 
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано шесть буферов
#property indicator_buffers 6
//---- использовано всего шесть графических построений
#property indicator_plots   6
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//---- в качестве цвета символа входа использован розовый цвет
#property indicator_color1  Magenta
//---- толщина линии индикатора 1 равна 1
#property indicator_width1  1
//---- отображение метки индикатора 1
#property indicator_label1  "SellSignal"

//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета символов стоплоссов использован розовый цвет
#property indicator_color2  Magenta
//---- толщина линии индикатора 2 равна 1
#property indicator_width2  1
//---- отображение метки индикатора 2
#property indicator_label2 "SellStopSignal"

//---- отрисовка индикатора 3 в виде символа
#property indicator_type3   DRAW_LINE
//---- в качестве цвета линии стоплоссов использован розовый цвет
#property indicator_color3  Magenta
//---- толщина линии индикатора 3 равна 1
#property indicator_width3  1
//---- отображение метки индикатора 3
#property indicator_label3 "SellStopLine"
//+----------------------------------------------+
//|  Параметры отрисовки бычьего индикатора      |
//+----------------------------------------------+
//---- отрисовка индикатора 4 в виде символа
#property indicator_type4   DRAW_ARROW
//---- в качестве цвета символа входа использован светло-зеленый цвет
#property indicator_color4  Lime
//---- толщина линии индикатора 4 равна 1
#property indicator_width4  1
//---- отображение метки индикатора 4
#property indicator_label4  "BuySignal"

//---- отрисовка индикатора 5 в виде символа
#property indicator_type5   DRAW_ARROW
//---- в качестве цвета символов стоплоссов использован светло-зеленый цвет
#property indicator_color5  Lime
//---- толщина линии индикатора 5 равна 1
#property indicator_width5  1
//---- отображение метки индикатора 5
#property indicator_label5 "BuyStopSignal"

//---- отрисовка индикатора 6 в виде символа
#property indicator_type6   DRAW_LINE
//---- в качестве цвета линии стоплоссов использован светло-зеленый цвет
#property indicator_color6  Lime
//---- толщина линии индикатора 6 равна 1
#property indicator_width6  1
//---- отображение метки индикатора 6
#property indicator_label6 "BuyStopLine"

//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int ChannelPeriod=5;
input double Risk=0.10;
input bool Signal=true;
input bool Line=true;
//+----------------------------------------------+

//---- объявление динамических массивов, которые в дальнейшем 
//---- будут использованы в качестве индикаторных буферов
double DownTrendSignal[];
double DownTrendBuffer[];
double DownTrendLine[];
double UpTrendSignal[];
double UpTrendBuffer[];
double UpTrendLine[];
//----
int StartBars;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация глобальных переменных 
   StartBars=ChannelPeriod+1;
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,DownTrendSignal,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"SellSignal");
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(DownTrendSignal,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,DownTrendBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"SellStopSignal");
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(DownTrendBuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,DownTrendLine,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"SellStopLine");
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(DownTrendLine,true);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,UpTrendSignal,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"BuySignal");
//---- символ для индикатора
   PlotIndexSetInteger(3,PLOT_ARROW,108);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(UpTrendSignal,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(4,UpTrendBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 5
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(4,PLOT_LABEL,"BuyStopSignal");
//---- символ для индикатора
   PlotIndexSetInteger(4,PLOT_ARROW,159);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(UpTrendBuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(5,UpTrendLine,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 6
   PlotIndexSetInteger(5,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(5,PLOT_LABEL,"BuyStopLine");
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(UpTrendLine,true);
//---- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(5,PLOT_EMPTY_VALUE,0.0);
   
//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="PriceChannel_Stop";
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
   int limit,bar,iii,trend;
   double bsmax[],bsmin[],High,Low,Price,dPrice;

//---- объявления переменных памяти  
   static int trend_;
   static double bsmax_,bsmin_;

//---- расчеты стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-StartBars; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }

//---- индексация элементов в массивах как в таймсериях
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- изменение размеров временных массивов 
   if(ArrayResize(bsmax,limit+2)!=limit+2) return(0);
   if(ArrayResize(bsmin,limit+2)!=limit+2) return(0);

//---- предварительный цикл расчета временных массивов
   for(bar=limit; bar>=0; bar--)
     {
      High=high[bar];
      Low =low [bar];
      iii=bar-1+ChannelPeriod;
      while(iii>=bar)
        {
         Price=high[iii];
         if(High<Price)High=Price;
         Price=low[iii];
         if(Low>Price) Low=Price;
         iii--;
        }
      dPrice=(High-Low)*Risk;
      bsmax[bar]=High-dPrice;
      bsmin[bar]=Low +dPrice;
     }

//---- восстанавливаем значения переменных
   bsmax[limit+1]=bsmax_;
   bsmin[limit+1]=bsmin_;
   trend=trend_;

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0; bar--)
     {
//---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0)
        {
         bsmax_=bsmax[1];
         bsmin_=bsmin[1];
         trend_=trend;
        }
//----        
      UpTrendBuffer  [bar]=0.0;
      DownTrendBuffer[bar]=0.0;
      UpTrendSignal  [bar]=0.0;
      DownTrendSignal[bar]=0.0;
      UpTrendLine    [bar]=0.0;
      DownTrendLine  [bar]=0.0;
//----
      if(close[bar]>bsmax[bar+1]) trend= 1;
      if(close[bar]<bsmin[bar+1]) trend=-1;
//----
      if(trend>0 && bsmin[bar]<bsmin[bar+1]) bsmin[bar]=bsmin[bar+1];
      if(trend<0 && bsmax[bar]>bsmax[bar+1]) bsmax[bar]=bsmax[bar+1];
//----
      if(trend>0)
        {
         Price=bsmin[bar];
         if(Signal && DownTrendBuffer[bar+1]>0)
           {
            UpTrendSignal[bar]=Price;
            if(Line) UpTrendLine[bar]=Price;
           }
         else
           {
            UpTrendBuffer[bar]=Price;
            if(Line) UpTrendLine[bar]=Price;
           }
        }
//----
      if(trend<0)
        {
         Price=bsmax[bar];
         if(Signal && UpTrendBuffer[bar+1]>0)
           {
            DownTrendSignal[bar]=Price;
            if(Line) DownTrendLine[bar]=Price;
           }
         else
           {
            DownTrendBuffer[bar]=Price;
            if(Line) DownTrendLine[bar]=Price;
           }
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
