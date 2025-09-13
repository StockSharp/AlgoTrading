//+------------------------------------------------------------------+
//|                                                   wlxBW5Zone.mq5 |
//|                                          Copyright © 2005, Wellx |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2005, Wellx"
//---- ссылка на сайт автора
#property link      "http://www.metaquotes.net/"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчёта и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+ 
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET 0 // Константа для возврата терминалу команды на пересчет индикатора
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован розовый цвет
#property indicator_color1  clrMagenta
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение бычей метки индикатора
#property indicator_label1  "wlxBW5Zone Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьго индикатора       |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычей линии индикатора использован зелёный цвет
#property indicator_color2  clrLime
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение медвежьей метки индикатора
#property indicator_label2 "wlxBW5Zone Buy"
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
enum Direct //Тип константы
  {
   ON = 0,     // По тренду
   OFF         // Против тренда
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input Direct Dir=ON; // Направление сигналов
//+----------------------------------------------+

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//---- Объявление целых переменных для хендлов индикаторов
int AC_Handle,AO_Handle,ATR_Handle;
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- инициализация глобальных переменных 
   int ATR_Period=12;
   int AC_Period=37;
   int AO_Period=33;
   min_rates_total=int(MathMax(MathMax(AC_Period,AO_Period)+4,ATR_Period));
   
//---- получение хендла индикатора ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ATR");
      return(INIT_FAILED);
     }

//---- получение хендла индикатора  Accelerator Oscillator 
   AC_Handle=iAC(Symbol(),PERIOD_CURRENT);
   if(AC_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора  Accelerator Oscillator");
      return(INIT_FAILED);
     }

//---- получение хендла индикатора  Awesome Oscillator 
   AO_Handle=iAO(Symbol(),PERIOD_CURRENT);
   if(AO_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора  Awesome Oscillator");
      return(INIT_FAILED);
     }

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,119);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(SellBuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,119);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);

//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и лэйба для субъокон 
   string short_name="wlxBW5ZoneSig";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
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
//---- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(AC_Handle)<rates_total
      || BarsCalculated(AO_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- объявления локальных переменных 
   int to_copy,limit,bar;
   double AC[],AO[],ATR[],range;
   bool flagUP,flagDown;
   static bool flagUP_,flagDown_;

//---- расчёты необходимого количества копируемых данных и
//стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-min_rates_total; // стартовый номер для расчёта всех баров
      flagUP_=false;
      flagDown_=false;
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
     }

   to_copy=limit+1;
//---- копируем вновь появившиеся данные в массивы AO[],AC[] и ATR[]
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   to_copy+=4;
   if(CopyBuffer(AC_Handle,0,0,to_copy,AC)<=0) return(RESET);
   if(CopyBuffer(AO_Handle,0,0,to_copy,AO)<=0) return(RESET);

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(AC,true);
   ArraySetAsSeries(AO,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- восстанавливаем значения переменных
   flagUP=flagUP_;
   flagDown=flagDown_;

//---- основной цикл расчёта индикатора
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(!flagUP)
         if((AO[bar]>AO[bar+1] && AO[bar+1]>AO[bar+2] && AO[bar+2]>AO[bar+3] && AO[bar+3]>AO[bar+4])
         && (AC[bar]>AC[bar+1] && AC[bar+1]>AC[bar+2] && AC[bar+2]>AC[bar+3] && AC[bar+3]>AC[bar+4]))
           {
            range=ATR[bar]*3/8;
            if(Dir==ON) BuyBuffer[bar]=low[bar]-range;
            else SellBuffer[bar]=high[bar]+range;
            flagUP=true;
           }

      if(!flagDown)
         if((AO[bar]<AO[bar+1] && AO[bar+1]<AO[bar+2] && AO[bar+2]<AO[bar+3] && AO[bar+3]<AO[bar+4])
         && (AC[bar]<AC[bar+1] && AC[bar+1]<AC[bar+2] && AC[bar+2]<AC[bar+3] && AC[bar+3]<AC[bar+4]))
           {
            range=ATR[bar]*3/8;
            if(Dir==ON) SellBuffer[bar]=high[bar]+range;
            else BuyBuffer[bar]=low[bar]-range;
            flagDown=true;
           }

      if(AO[bar+0]<AO[bar+1] || AC[bar+0]<AC[bar+1]) flagUP=false;
      if(AO[bar+0]>AO[bar+1] || AC[bar+0]>AC[bar+1]) flagDown=false;

      //---- сохраняем значения переменных
      if(bar)
        {
         flagUP_=flagUP;
         flagDown_=flagDown;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
