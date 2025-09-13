//+------------------------------------------------------------------+
//|                                                 DigitalF-T01.mq5 |
//|                                            Copyright © 2006, XXX |
//|                                                                  |
//+------------------------------------------------------------------+
//---- авторство индикатора
#property copyright "Copyright © 2006, XXX"
//---- авторство индикатора
#property link      ""
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в отдельном окне
#property indicator_separate_window
//---- для расчета и отрисовки индикатора использовано четыре буфера
#property indicator_buffers 4
//---- использовано три графических построения
#property indicator_plots   3
//+----------------------------------------------+
//| Параметры отрисовки индикатора DigitalF-T01  |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде линии
#property indicator_type1   DRAW_LINE
//---- в качестве цвета основной линии индикатора использован цвет DarkOrchid
#property indicator_color1  clrDarkOrchid
//---- линия индикатора 1 - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//---- толщина линии индикатора 1 равна 2
#property indicator_width1  2
//---- отображение метки линии индикатора
#property indicator_label1  "DigitalF-T01"
//+----------------------------------------------+
//| Параметры отрисовки индикатора Trigger       |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета сигнальной линии индикатора использован цвет Red
#property indicator_color2  clrRed
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение метки линии индикатора
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| Параметры отрисовки облака                   |
//+----------------------------------------------+
//---- отрисовка индикатора в виде цветного облака
#property indicator_type3   DRAW_FILLING
//---- в качестве цветов индикатора использованы
#property indicator_color3  clrPaleGreen,clrHotPink
//+----------------------------------------------+
//| Объявление перечисления                      |
//+----------------------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input uint halfchanel=25;  // Коэффициент срабатывания тригера
input Applied_price_ IPC=PRICE_CLOSE_;// Ценовая константа
input int Shift=0; // Сдвиг индикатора по горизонтали в барах
//+----------------------------------------------+
//---- объявление динамических массивов, которые в дальнейшем
//---- будут использованы в качестве индикаторных буферов
double DigBuffer[];
double TriggerBuffer[];
double UpBuffer[];
double DnBuffer[];
//----
double dHalfchanel;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных начала отсчета данных
   min_rates_total=24;
   int sh=int(MathRound(60*(23*60+59)/PeriodSeconds())+1);
   min_rates_total=MathMax(min_rates_total,sh);
   dHalfchanel=halfchanel*_Point;
//---- превращение динамического массива DigBuffer[] в индикаторный буфер
   SetIndexBuffer(0,DigBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива TriggerBuffer[] в индикаторный буфер
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2 на min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива UpBuffer[] в индикаторный буфер
   SetIndexBuffer(2,UpBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- превращение динамического массива DnBuffer[] в индикаторный буфер
   SetIndexBuffer(3,DnBuffer,INDICATOR_DATA);
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- инициализация переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"DigitalF-T01(",halfchanel,", ",EnumToString(IPC),", ",Shift,")");
//---- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
                const datetime &time[],
                const double &open[],
                const double& high[],     // ценовой массив максимумов цены для расчета индикатора
                const double& low[],      // ценовой массив минимумов цены  для расчета индикатора
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total) return(0);
//---- объявления локальных переменных 
   int first,bar;
//---- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
      first=24;                   // стартовый номер для расчета всех баров
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//---- основной цикл расчета цифрового фильтра
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      DigBuffer[bar]=
                     0.24470985659780*PriceSeries(IPC,bar,open,low,high,close)
                     +0.23139774006970*PriceSeries(IPC,bar-1,open,low,high,close)
                     +0.20613796947320*PriceSeries(IPC,bar-2,open,low,high,close)
                     +0.17166230340640*PriceSeries(IPC,bar-3,open,low,high,close)
                     +0.13146907903600*PriceSeries(IPC,bar-4,open,low,high,close)
                     +0.08950387549560*PriceSeries(IPC,bar-5,open,low,high,close)
                     +0.04960091651250*PriceSeries(IPC,bar-6,open,low,high,close)
                     +0.01502270569607*PriceSeries(IPC,bar-7,open,low,high,close)
                     -0.01188033734430*PriceSeries(IPC,bar-8,open,low,high,close)
                     -0.02989873856137*PriceSeries(IPC,bar-9,open,low,high,close)
                     -0.03898967104900*PriceSeries(IPC,bar-10,open,low,high,close)
                     -0.04014113626390*PriceSeries(IPC,bar-11,open,low,high,close)
                     -0.03511968085800*PriceSeries(IPC,bar-12,open,low,high,close)
                     -0.02611613850342*PriceSeries(IPC,bar-13,open,low,high,close)
                     -0.01539056955666*PriceSeries(IPC,bar-14,open,low,high,close)
                     -0.00495353651394*PriceSeries(IPC,bar-15,open,low,high,close)
                     +0.00368588764825*PriceSeries(IPC,bar-16,open,low,high,close)
                     +0.00963614049782*PriceSeries(IPC,bar-17,open,low,high,close)
                     +0.01265138888314*PriceSeries(IPC,bar-18,open,low,high,close)
                     +0.01307496106868*PriceSeries(IPC,bar-19,open,low,high,close)
                     +0.01169702291063*PriceSeries(IPC,bar-20,open,low,high,close)
                     +0.00974841844086*PriceSeries(IPC,bar-21,open,low,high,close)
                     +0.00898900012545*PriceSeries(IPC,bar-22,open,low,high,close)
                     -0.00649745721156*PriceSeries(IPC,bar-23,open,low,high,close);
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;
//---- основной цикл расчета тригерной линии
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      MqlDateTime tm;
      TimeToStruct(time[bar],tm);
      int per=int(MathRound(60*(tm.hour*60+tm.min)/PeriodSeconds())+1);
      int barX=bar-per;
      if(DigBuffer[bar]>=close[barX]) TriggerBuffer[bar]=close[barX]+dHalfchanel;
      if(DigBuffer[bar]<close[barX]) TriggerBuffer[bar]=close[barX]-dHalfchanel;
      UpBuffer[bar]=DigBuffer[bar];
      DnBuffer[bar]=TriggerBuffer[bar];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+   
//| Получение значения ценовой таймсерии                             |
//+------------------------------------------------------------------+ 
double PriceSeries(uint applied_price,// ценовая константа
                   uint   bar,// индекс сдвига относительно текущего бара на указанное количество периодов назад или вперед).
                   const double &Open[],
                   const double &Low[],
                   const double &High[],
                   const double &Close[])
  {
//----
   switch(applied_price)
     {
      //---- ценовые константы из перечисления ENUM_APPLIED_PRICE
      case  PRICE_CLOSE: return(Close[bar]);
      case  PRICE_OPEN: return(Open [bar]);
      case  PRICE_HIGH: return(High [bar]);
      case  PRICE_LOW: return(Low[bar]);
      case  PRICE_MEDIAN: return((High[bar]+Low[bar])/2.0);
      case  PRICE_TYPICAL: return((Close[bar]+High[bar]+Low[bar])/3.0);
      case  PRICE_WEIGHTED: return((2*Close[bar]+High[bar]+Low[bar])/4.0);
      //----                            
      case  8: return((Open[bar] + Close[bar])/2.0);                        // PRICE_SIMPL_
      case  9: return((Open[bar] + Close[bar] + High[bar] + Low[bar])/4.0); // PRICE_QUARTER_
      //----                                
      case 10: // PRICE_TRENDFOLLOW0_
        {
         if(Close[bar]>Open[bar])return(High[bar]);
         else
           {
            if(Close[bar]<Open[bar])
               return(Low[bar]);
            else return(Close[bar]);
           }
        }
      //----         
      case 11:  // PRICE_TRENDFOLLOW1_
        {
         if(Close[bar]>Open[bar])return((High[bar]+Close[bar])/2.0);
         else
           {
            if(Close[bar]<Open[bar])
               return((Low[bar]+Close[bar])/2.0);
            else return(Close[bar]);
           }
         break;
        }
      //----         
      case 12:  // PRICE_DEMARK_
        {
         double res=High[bar]+Low[bar]+Close[bar];
         //----
         if(Close[bar]<Open[bar]) res=(res+Low[bar])/2;
         if(Close[bar]>Open[bar]) res=(res+High[bar])/2;
         if(Close[bar]==Open[bar]) res=(res+Close[bar])/2;
         return(((res-Low[bar])+(res-High[bar]))/2);
        }
      //----
      default: return(Close[bar]);
     }
//----
//return(0);
  }
//+------------------------------------------------------------------+
