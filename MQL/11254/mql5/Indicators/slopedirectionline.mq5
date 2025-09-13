//+---------------------------------------------------------------------+ 
//|                                              SlopeDirectionLine.mq5 | 
//|                                        Copyright © 2006, WizardSerg | 
//|                                                  wizardserg@mail.ru | 
//+---------------------------------------------------------------------+ 
#property copyright "Copyright © 2006, WizardSerg"
#property link "wizardserg@mail.ru"
//--- номер версии индикатора
#property version   "1.01"
//--- отрисовка индикатора в главном окне
#property indicator_chart_window 
//--- количество индикаторных буферов
#property indicator_buffers 2 
//--- использовано всего одно графическое построение
#property indicator_plots   1
//+-----------------------------------+
//| Параметры отрисовки индикатора    |
//+-----------------------------------+
//--- отрисовка индикатора в виде многоцветной линии
#property indicator_type1   DRAW_COLOR_LINE
//--- в качестве цветов трёхцветной линии использованы
#property indicator_color1  clrDeepPink,clrGray,clrDarkViolet
//--- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width1  2
//--- отображение метки индикатора
#property indicator_label1  "SlopeDirectionLine"
//+-----------------------------------+
//| Описание класса CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//--- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3;
//+-----------------------------------+
//| объявление перечислений           |
//+-----------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//| объявление перечислений           |
//+-----------------------------------+
/*enum Smooth_Method - перечисление объявлено в файле SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+-----------------------------------+
//| Входные параметры индикатора      |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_LWMA;             // Метод первого усреднения
input uint Length1=12;                                // Глубина первого усреднения
input int Phase1=15;                                  // Параметр первого усреднения
input Smooth_Method MA_Method2=MODE_SMA;              // Метод усреднения второго сглаживания 
input int Phase2=15;                                  // Параметр второго сглаживания
input Applied_price_ IPC=PRICE_CLOSE;                 // Ценовая константа
input int Shift=0;                                    // Сдвиг индикатора по горизонтали в барах
input int PriceShift=0;                               // Сдвиг индикатора по вертикали в пунктах
input bool On_Push = false;                           // Разрешение на передачу push-сообщений
input bool On_Email = false;                          // Разрешение на отправку почты
input bool On_Alert = true;                           // Разрешение на подачу алерта
input bool On_Play_Sound = false;                     // Разрешение на подачу звукового сигнала
input string NameFileSound = "expert.wav";            // Имя для файла звукового сигнала
input string  CommentSirName="SlopeDirectionLine: ";  // Первая часть алерт-коммента
input uint SignalBar=1;                               // Номер бара для сигнала
//+-----------------------------------+
//--- объявление динамических массивов, которые в дальнейшем
//--- будут использованы в качестве индикаторных буферов
double IndBuffer[];
double ColorIndBuffer[];
//--- объявление переменных значений периодов усреднений
int LengthX,LengthR;
//--- объявление переменной значения вертикального сдвига мувинга
double dPriceShift;
//--- объявление целочисленных переменных начала отсчёта данных
int min_rates_total,min_rates_;
//+------------------------------------------------------------------+
//| Получение таймфрейма в виде строки                               |
//+------------------------------------------------------------------+
string GetStringTimeframe(ENUM_TIMEFRAMES timeframe)
  {return(StringSubstr(EnumToString(timeframe),7,-1));}
//+------------------------------------------------------------------+   
//| SlopeDirectionLine indicator initialization function             | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//--- инициализация переменных начала отсчёта данных
   LengthX=int(Length1/2);
   LengthR=int(MathMax(MathSqrt(Length1),1));
   min_rates_=+XMA1.GetStartBars(MA_Method1,Length1,Phase1);
   min_rates_total=min_rates_+XMA1.GetStartBars(MA_Method2,LengthR,Phase2);
//--- инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
//--- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//--- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- осуществление сдвига индикатора 1 по горизонтали
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- осуществление сдвига начала отсчёта отрисовки индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- инициализации переменной для короткого имени индикатора
   string shortname;
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   StringConcatenate(shortname,"SlopeDirectionLine(",Length1,", ",LengthR,", ",Smooth2,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//--- завершение инициализации
  }
//+------------------------------------------------------------------+ 
//| SlopeDirectionLine iteration function                            | 
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
//--- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);
//--- объявление переменных с плавающей точкой  
   double price,line,xline;
//--- объявление целочисленных переменных и получение уже подсчитанных баров
   int first,bar,clr;
//--- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first=0; // стартовый номер для расчёта всех баров
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров
//--- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);

      line=XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,price,bar,false);
      line=2*XMA2.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,LengthX,price,bar,false)-line;
      xline=XMA3.XMASeries(min_rates_,prev_calculated,rates_total,MA_Method2,Phase2,LengthR,line,bar,false);
      //---       
      IndBuffer[bar]=xline+dPriceShift;
     }
//--- корректировка значения переменной first
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
      first=min_rates_total-1; // стартовый номер для расчёта всех баров
//--- основной цикл раскраски сигнальной линии
   for(bar=first; bar<rates_total; bar++)
     {
      clr=1;
      ColorIndBuffer[bar]=1;
      if(IndBuffer[bar-1]<IndBuffer[bar]) clr=2;
      if(IndBuffer[bar-1]>IndBuffer[bar]) clr=0;
      ColorIndBuffer[bar]=clr;

      if(bar==rates_total-1-SignalBar)
        {
         if(ColorIndBuffer[bar-1]!=2 && clr==2)
           {
            datetime SignalTime=TimeCurrent();
            if(On_Play_Sound) PlaySound(NameFileSound);
            string period=GetStringTimeframe(Period());
            string comment,sTime=" CurrTime="+TimeToString(SignalTime,TIME_MINUTES);
            StringConcatenate(comment,CommentSirName,Symbol(),period," ",sTime," Сигнал на покупку!");
            if(On_Alert) Alert(comment);
            if(On_Push) SendNotification(comment);
            if(On_Email) SendMail(CommentSirName+Symbol()+period,comment);
           }
           
         if(ColorIndBuffer[bar-1]!=0 && clr==0)
           {
            datetime SignalTime=TimeCurrent();
            if(On_Play_Sound) PlaySound(NameFileSound);
            string period=GetStringTimeframe(Period());
            string comment,sTime=" CurrTime="+TimeToString(SignalTime,TIME_MINUTES);
            StringConcatenate(comment,CommentSirName,Symbol(),period," ",sTime," Сигнал на продажу!");
            if(On_Alert) Alert(comment);
            if(On_Push) SendNotification(comment);
            if(On_Email) SendMail(CommentSirName+Symbol()+period,comment);
           }
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
