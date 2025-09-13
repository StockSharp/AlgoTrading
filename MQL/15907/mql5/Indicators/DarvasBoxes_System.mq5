//+------------------------------------------------------------------+
//|                                           DarvasBoxes_System.mq5 |
//|                               Copyright © 2013, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2013, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Пробойная система с использованием индикатора DarvasBoxes"
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
#property indicator_label1  "DarvasBoxes"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 2            |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде линии
#property indicator_type2   DRAW_LINE
//---- в качестве цвета бычей линии индикатора использован MediumSeaGreen цвет
#property indicator_color2  clrMediumSeaGreen
//---- линия индикатора 2 - непрерывная кривая
#property indicator_style2  STYLE_SOLID
//---- толщина линии индикатора 2 равна 2
#property indicator_width2  2
//---- отображение бычей метки индикатора
#property indicator_label2  "Upper DarvasBoxes"
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
#property indicator_label3  "Lower DarvasBoxes"
//+----------------------------------------------+
//|  Параметры отрисовки индикатора 4            |
//+----------------------------------------------+
//---- отрисовка индикатора в виде четырёхцветной гистограммы
#property indicator_type4 DRAW_COLOR_HISTOGRAM2
//---- в качестве цветов четырёхцветной гистограммы использованы
#property indicator_color4 clrDeepPink,clrPurple,clrGray,clrMediumBlue,clrDodgerBlue
//---- линия индикатора - сплошная
#property indicator_style4 STYLE_SOLID
//---- толщина линии индикатора равна 2
#property indicator_width4 2
//---- отображение метки индикатора
#property indicator_label4 "DarvasBoxes_BARS"

//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input bool symmetry=true;
input uint   Shift=2;   // сдвиг канала по горизонтали в барах 
//+----------------------------------------------+
//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double Up1Buffer[],Dn1Buffer[];
double Up2Buffer[],Dn2Buffer[];
double UpIndBuffer[],DnIndBuffer[],ColorIndBuffer[];
//---- Объявление целых переменных начала отсчёта данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Инициализация переменных начала отсчёта данных
   min_rates_total=int(2+Shift);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,Up1Buffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(Up1Buffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,Dn1Buffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(Dn1Buffer,true);
   
//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(2,Up2Buffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(Up2Buffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(3,Dn2Buffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(Dn2Buffer,true);
   
//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(4,UpIndBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(UpIndBuffer,true);

//---- превращение динамического массива IndBuffer в индикаторный буфер
   SetIndexBuffer(5,DnIndBuffer,INDICATOR_DATA);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(DnIndBuffer,true);

//---- превращение динамического массива в цветовой, индексный буфер   
   SetIndexBuffer(6,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- индексация элементов в буфере как в таймсерии
   ArraySetAsSeries(ColorIndBuffer,true);
   
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
   StringConcatenate(shortname,"DarvasBoxes(",Shift,")");
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
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

//---- индексация элементов в массивах как в таймсериях  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- Объявление целых переменных
   int limit;
//---- Объявление статических переменных
   static int state,STATE;
   static double box_top,box_bottom,BOX_TOP,BOX_BUTTOM;

//---- расчёты стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчёта индикатора
     {
      limit=rates_total-min_rates_total; // стартовый номер для расчёта всех баров
      BOX_TOP=high[limit+1];
      BOX_BUTTOM=low[limit+1];
      STATE=1;
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчёта новых баров
     }

//---- восстанавливаем значения переменных
   state=STATE;
   box_top=BOX_TOP;
   box_bottom=BOX_BUTTOM;

//---- основной цикл расчёта индикатора
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      switch(state)
        {
         case 1:  box_top=high[bar]; if(symmetry)box_bottom=low[bar]; break;
         case 2:  if(box_top<=high[bar]) box_top=high[bar]; break;
         case 3:  if(box_top> high[bar]) box_bottom=low[bar]; else box_top=high[bar]; break;
         case 4:  if(box_top > high[bar]) {if(box_bottom >= low[bar]) box_bottom=low[bar];} else box_top=high[bar]; break;
         case 5:  if(box_top > high[bar]) {if(box_bottom >= low[bar]) box_bottom=low[bar];} else box_top=high[bar]; state=0; break;
        }

      Up1Buffer[bar]=box_top;
      Dn1Buffer[bar]=box_bottom;
      Up2Buffer[bar]=box_top;
      Dn2Buffer[bar]=box_bottom;
      state++;
      
      //---- запоминаем значения переменных перед прогонами на текущем баре
      if(bar==1)
        {
         STATE=state;
         BOX_TOP=box_top;
         BOX_BUTTOM=box_bottom;
        }
     }

//---- расчёт стартового номера limit для цикла пересчёта баров
   if(prev_calculated>rates_total || prev_calculated<=0) limit-=int(Shift);     
//---- Основной цикл раскраски баров индикатора
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      int clr=2;
      UpIndBuffer[bar]=0.0;
      DnIndBuffer[bar]=0.0;
      
      if(close[bar]>Up1Buffer[bar+Shift])
        {
         if(open[bar]<close[bar]) clr=4;
         else clr=3;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      if(close[bar]<Dn1Buffer[bar+Shift])
        {
         if(open[bar]>close[bar]) clr=0;
         else clr=1;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
