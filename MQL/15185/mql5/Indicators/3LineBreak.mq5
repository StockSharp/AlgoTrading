//+------------------------------------------------------------------+
//|                                                   3LineBreak.mq5 |
//|                               Copyright © 2004, Poul_Trade_Forum |
//|                                                         Aborigen |
//|                                          http://forex.kbpauk.ru/ |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright " Copyright © 2004, Poul_Trade_Forum"
//---- ссылка на сайт автора
#property link      " http://forex.kbpauk.ru/"
//---- номер версии индикатора
#property version   "1.00"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора              |
//+----------------------------------------------+
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано три буфера
#property indicator_buffers 3
//---- использовано всего одно графическое построение
#property indicator_plots   1
//---- в качестве цветов использованы
#property indicator_color1 clrBlue,clrRed
//---- толщина линии индикатора 1 равна 2
#property indicator_width1 2
//---- в качестве индикатора использованы разноцветные бары
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- отображение метки индикатора
#property indicator_label1  "UpTend; DownTrend;"

//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int Lines_Break=3;
//+----------------------------------------------+

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtColorsBuffer[];
//----
bool Swing_;
int StartBars;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- инициализация глобальных переменных 
   StartBars=Lines_Break;
//---- превращение динамических массивов в индикаторные буферы
   SetIndexBuffer(0,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtLowBuffer,INDICATOR_DATA);
//---- превращение динамического массива в цветовой индексный буфер   
   SetIndexBuffer(2,ExtColorsBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буферах, как в таймсериях   
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtColorsBuffer,true);

//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="3LineBreak";
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
   int limit,bar;
   double HH,LL;
   bool Swing;

//---- расчеты необходимого количества копируемых данных и
//стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-StartBars; // стартовый номер для расчета всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
     }

//---- индексация элементов в массивах, как в таймсериях  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- восстанавливаем значения переменных
   Swing=Swing_;

//---- основной цикл расчета индикатора
   for(bar=limit; bar>=0; bar--)
     {
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(rates_total!=prev_calculated && bar==0) Swing_=Swing;

      HH = high[ArrayMaximum(high,bar+1,Lines_Break)];
      LL = low [ArrayMinimum(low, bar+1,Lines_Break)];
      //----
      if( Swing && low [bar]<LL) Swing=false;
      if(!Swing && high[bar]>HH) Swing=true;
      //----
      ExtHighBuffer[bar]=high[bar];
      ExtLowBuffer [bar]=low [bar];

      if(Swing) ExtColorsBuffer[bar]=0;
      else      ExtColorsBuffer[bar]=1;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
