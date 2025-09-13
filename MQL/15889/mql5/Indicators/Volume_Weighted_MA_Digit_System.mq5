//+------------------------------------------------------------------+
//|                              Volume_Weighted_MA_Digit_System.mq5 |
//|                               Copyright © 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Пробойная система с использованием индикатора Volume_Weighted_MA_Digit"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window
//---- для расчёта и отрисовки индикатора использовано семь буферов
#property indicator_buffers 7
//---- использовано четыре графических построения
#property indicator_plots   4
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 1            |
//+----------------------------------------------+
//---- отрисовка индикатора в виде одноцветного облака
#property indicator_type1   DRAW_FILLING
//---- в качестве цвета индикатора использован WhiteSmoke цвет
#property indicator_color1  clrWhiteSmoke
//---- отображение метки индикатора
#property indicator_label1  "Volume_Weighted_MA_Digit"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 2            |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета бычей линии индикатора использован DodgerBlue цвет
#property indicator_color2  clrDodgerBlue
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение бычей метки индикатора
#property indicator_label2  "Upper Volume_Weighted_MA_Digit"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 3            |
//+----------------------------------------------+
//---- отрисовка индикатора 3 в виде линии
#property indicator_type3   DRAW_LINE
//---- в качестве цвета медвежьей линии индикатора использован Magenta цвет
#property indicator_color3  clrMagenta
//---- линия индикатора 3 - непрерывная кривая
#property indicator_style3  STYLE_SOLID
//---- толщина линии индикатора 3 равна 2
#property indicator_width3  2
//---- отображение медвежьей метки индикатора
#property indicator_label3  "Lower Volume_Weighted_MA_Digit"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 4            |
//+----------------------------------------------+
//---- отрисовка индикатора в виде четырёхцветной гистограммы
#property indicator_type4 DRAW_COLOR_HISTOGRAM2
//---- в качестве цветов четырёхцветной гистограммы использованы
#property indicator_color4 clrRed,clrPurple,clrGray,clrTeal,clrLime
//---- линия индикатора - сплошная
#property indicator_style4 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width4 2
//---- отображение метки индикатора
#property indicator_label4 "Volume_Weighted_MA_Digit_BARS"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input string  SirName="Volume_Weighted_MA_Digit";     //первая часть имени графических объектов
input uint Length=12;                                 //глубина сглаживания                    
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;     //объём 
input uint Digit=2;                                   //количество разрядов округления
input int PriceShift=0;                               //cдвиг канала по вертикали в пунктах
input uint   Shift=2;                                 //сдвиг канала по горизонтали в барах 
input bool ShowPrice=true;                            //показывать ценовые метки
//---- цвета ценовых меток
input color  Up_Price_color=clrTeal;
input color  Dn_Price_color=clrMagenta;
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double Up1Buffer[],Dn1Buffer[];
double Up2Buffer[],Dn2Buffer[];
double UpIndBuffer[],DnIndBuffer[],ColorIndBuffer[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
int FATLSize;
double dPriceShift;
double PointPow10;
//---- Объявление стрингов для текстовых меток
string Dn_Price_name,Up_Price_name;
double Vol[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- инициализация переменных 
   min_rates_total=int(Length);
   min_rates_total+=int(Shift);
//---- Инициализация сдвига по вертикали
   dPriceShift=_Point*PriceShift;
   PointPow10=_Point*MathPow(10,Digit);
//---- Инициализация стрингов
   Up_Price_name=SirName+"Up_Price";
   Dn_Price_name=SirName+"Dn_Price";
//---- распределение памяти под массивы переменных  
   ArrayResize(Vol,Length);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,Up1Buffer,INDICATOR_DATA);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,Dn1Buffer,INDICATOR_DATA);
   
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,Up2Buffer,INDICATOR_DATA);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,Dn2Buffer,INDICATOR_DATA);
   
//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(4,UpIndBuffer,INDICATOR_DATA);

//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(5,DnIndBuffer,INDICATOR_DATA);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(6,ColorIndBuffer,INDICATOR_COLOR_INDEX);

   
//---- осуществление сдвига индикатора 1 по горизонтали на Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 1 на min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- осуществление сдвига индикатора 2 по горизонтали на Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 2 на min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- осуществление сдвига индикатора 3 по горизонтали на Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 3 на min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- осуществление сдвига индикатора 3 по горизонтали на Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,0);
//---- осуществление сдвига начала отсчёта отрисовки индикатора 4 на min_rates_total
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"Volume_Weighted_MA_Digit_System(",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+    
void OnDeinit(const int reason)
  {
//----
   ObjectDelete(0,Up_Price_name);
   ObjectDelete(0,Dn_Price_name);
//----
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // количество истории в барах на текущем тике
                const int prev_calculated,// количество истории в барах на предыдущем тике
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
//---- проверка количества баров на достаточность для расчёта
   if(rates_total<min_rates_total) return(0);

//---- объявления локальных переменных 
   int first,bar;
   //---- Объявление переменных с плавающей точкой  
   double mov,sum;

//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчёта индикатора
     {
      first=min_rates_total-1; // стартовый номер для расчёта всех баров
     }
   else first=prev_calculated-1; // стартовый номер для расчёта новых баров

//---- основной цикл расчёта индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- строим верхнюю границу канала
      sum=0.0;
      for(int kkk=int(bar-Length+1); kkk<=bar; kkk++)
        {
         int index=bar-kkk;
         if(VolumeType==VOLUME_TICK) Vol[index]=double(tick_volume[kkk]);
         else Vol[index]=double(volume[kkk]);
         sum+=Vol[index];
        }
      for(int rrr=0; rrr<int(Length); rrr++) Vol[rrr]/=sum;
      
      mov=0.0;
      for(int kkk=int(bar-Length+1); kkk<=bar; kkk++) mov+=high[kkk]*Vol[bar-kkk];
      mov+=dPriceShift;
      Up1Buffer[bar]=Up2Buffer[bar]=PointPow10*MathRound(mov/PointPow10);
      //---- строим нижнюю границу канала
      mov=0.0;
      for(int kkk=int(bar-Length+1); kkk<=bar; kkk++) mov+=low[kkk]*Vol[bar-kkk];
      mov+=dPriceShift;
      Dn1Buffer[bar]=Dn2Buffer[bar]=PointPow10*MathRound(mov/PointPow10);
     }


//---- расчёт стартового номера first для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;     
//---- Основной цикл раскраски баров индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;
      UpIndBuffer[bar]=0.0;
      DnIndBuffer[bar]=0.0;
      
      if(close[bar]>Up1Buffer[bar-Shift])
        {
         if(open[bar]<close[bar]) clr=4;
         else clr=3;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      if(close[bar]<Dn1Buffer[bar-Shift])
        {
         if(open[bar]>close[bar]) clr=0;
         else clr=1;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      ColorIndBuffer[bar]=clr;
     }
   if(ShowPrice)
     {
      int bar0=rates_total-1;
      datetime time0=time[bar0]+Shift*PeriodSeconds();
      SetRightPrice(0,Up_Price_name,0,time0,Up1Buffer[bar0-Shift],Up_Price_color);
      SetRightPrice(0,Dn_Price_name,0,time0,Dn1Buffer[bar0-Shift],Dn_Price_color);
     }
//----     
   ChartRedraw(0);
   return(rates_total);
  }
//+------------------------------------------------------------------+
//|  RightPrice creation                                             |
//+------------------------------------------------------------------+
void CreateRightPrice(long chart_id,// chart ID
                      string   name,              // object name
                      int      nwin,              // window index
                      datetime time,              // price level time
                      double   price,             // price level
                      color    Color              // Text color
                      )
//---- 
  {
//----
   ObjectCreate(chart_id,name,OBJ_ARROW_RIGHT_PRICE,nwin,time,price);
   ObjectSetInteger(chart_id,name,OBJPROP_COLOR,Color);
   ObjectSetInteger(chart_id,name,OBJPROP_BACK,true);
   ObjectSetInteger(chart_id,name,OBJPROP_WIDTH,2);
//----
  }
//+------------------------------------------------------------------+
//|  RightPrice reinstallation                                       |
//+------------------------------------------------------------------+
void SetRightPrice(long chart_id,// chart ID
                   string   name,              // object name
                   int      nwin,              // window index
                   datetime time,              // price level time
                   double   price,             // price level
                   color    Color              // Text color
                   )
//---- 
  {
//----
   if(ObjectFind(chart_id,name)==-1) CreateRightPrice(chart_id,name,nwin,time,price,Color);
   else ObjectMove(chart_id,name,0,time,price);
//----
  }
//+------------------------------------------------------------------+
