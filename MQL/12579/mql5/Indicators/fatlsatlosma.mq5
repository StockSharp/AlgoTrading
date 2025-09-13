//+------------------------------------------------------------------+
//|                                                 FatlSatlOsma.mq5 |
//|                                  Copyright 2012, Dmitry Shmatkov |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+
//--- авторство индикатора
#property copyright "Copyright 2012, Dmitry Shmatkov"
//--- ссылка на сайт автора
#property link      "http://www.metaquotes.net/"
//--- номер версии индикатора
#property version   "1.00"
//--- отрисовка индикатора в отдельном окне
#property indicator_separate_window 
//--- для расчета и отрисовки индикатора использован один буфер
#property indicator_buffers 1
//--- использовано всего одно графическое построение
#property indicator_plots   1
//--- отрисовка индикатора в виде линии
#property indicator_type1   DRAW_LINE
//--- в качестве цвета линии индикатора использован DodgerBlue цвет
#property indicator_color1  clrDodgerBlue
//--- линия индикатора - непрерывная кривая
#property indicator_style1  STYLE_SOLID
//--- толщина линии индикатора равна 2
#property indicator_width1  2
//--- отображение метки индикатора
#property indicator_label1  "FatlSatlOsma"
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input int Shift=0; // сдвиг мувинга по горизонтали в барах 
//+----------------------------------------------+
//--- объявление и инициализация переменной для хранения количества расчетных баров
int FATLPeriod=39;
//--- объявление и инициализация переменной для хранения количества расчетных баров
int SATLPeriod=65;
//--- объявление динамического массива, который в дальнейшем
//--- будет использован в качестве индикаторного буфера
double ExtLineBuffer[];
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- инициализация переменных начала отсчета данных
   min_rates_total=int(MathMax(FATLPeriod,SATLPeriod));
//--- превращение динамического массива ExtLineBuffer в индикаторный буфер
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//--- осуществление сдвига индикатора по горизонтали на FATLShift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- установка позиции, с которой начинается отрисовка индикатора
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- инициализации переменной для короткого имени индикатора
   string shortname;
   StringConcatenate(shortname,"FatlSatlOsma(",Shift,")");
//--- создание метки для отображения в Окне данных
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//--- создание имени для отображения в отдельном подокне и во всплывающей подсказке
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- определение точности отображения значений индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- запрет на отрисовку индикатором пустых значений
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,     // количество истории в барах на текущем тике
                const int prev_calculated, // количество истории в барах на предыдущем тике
                const int begin,           // номер начала достоверного отсчета баров
                const double &price[])     // ценовой массив для расчета индикатора
  { 
//--- проверка количества баров на достаточность для расчета
   if(rates_total<min_rates_total+begin) return(0);
//--- объявления локальных переменных 
   int first,bar;
   double FATL,SATL;
//--- расчет стартового номера first для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0) // проверка на первый старт расчета индикатора
     {
      first=min_rates_total-1+begin;  // стартовый номер для расчета всех баров
      //--- увеличим позицию начала данных на begin баров, вследствие расчетов на данных другого индикатора
      if(begin>0)
         PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,begin+min_rates_total);
     }
   else first=prev_calculated-1; // стартовый номер для расчета новых баров
//--- основной цикл расчета индикатора
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //--- 
      FATL = 0.4360409450 * price[bar - 0]
           + 0.3658689069 * price[bar - 1]
           + 0.2460452079 * price[bar - 2]
           + 0.1104506886 * price[bar - 3]
           - 0.0054034585 * price[bar - 4]
           - 0.0760367731 * price[bar - 5]
           - 0.0933058722 * price[bar - 6]
           - 0.0670110374 * price[bar - 7]
           - 0.0190795053 * price[bar - 8]
           + 0.0259609206 * price[bar - 9]
           + 0.0502044896 * price[bar - 10]
           + 0.0477818607 * price[bar - 11]
           + 0.0249252327 * price[bar - 12]
           - 0.0047706151 * price[bar - 13]
           - 0.0272432537 * price[bar - 14]
           - 0.0338917071 * price[bar - 15]
           - 0.0244141482 * price[bar - 16]
           - 0.0055774838 * price[bar - 17]
           + 0.0128149838 * price[bar - 18]
           + 0.0226522218 * price[bar - 19]
           + 0.0208778257 * price[bar - 20]
           + 0.0100299086 * price[bar - 21]
           - 0.0036771622 * price[bar - 22]
           - 0.0136744850 * price[bar - 23]
           - 0.0160483392 * price[bar - 24]
           - 0.0108597376 * price[bar - 25]
           - 0.0016060704 * price[bar - 26]
           + 0.0069480557 * price[bar - 27]
           + 0.0110573605 * price[bar - 28]
           + 0.0095711419 * price[bar - 29]
           + 0.0040444064 * price[bar - 30]
           - 0.0023824623 * price[bar - 31]
           - 0.0067093714 * price[bar - 32]
           - 0.0072003400 * price[bar - 33]
           - 0.0047717710 * price[bar - 34]
           + 0.0005541115 * price[bar - 35]
           + 0.0007860160 * price[bar - 36]
           + 0.0130129076 * price[bar - 37]
           + 0.0040364019 * price[bar - 38];
           //--- 
      SATL = 0.0982862174 * price[bar - 0]
            +0.0975682269 * price[bar - 1]
            +0.0961401078 * price[bar - 2]
            +0.0940230544 * price[bar - 3]
            +0.0912437090 * price[bar - 4]
            +0.0878391006 * price[bar - 5]
            +0.0838544303 * price[bar - 6]
            +0.0793406350 * price[bar - 7]
            +0.0743569346 * price[bar - 8]
            +0.0689666682 * price[bar - 9]
            +0.0632381578 * price[bar - 10]
            +0.0572428925 * price[bar - 11]
            +0.0510534242 * price[bar - 12]
            +0.0447468229 * price[bar - 13]
            +0.0383959950 * price[bar - 14]
            +0.0320735368 * price[bar - 15]
            +0.0258537721 * price[bar - 16]
            +0.0198005183 * price[bar - 17]
            +0.0139807863 * price[bar - 18]
            +0.0084512448 * price[bar - 19]
            +0.0032639979 * price[bar - 20]
            -0.0015350359 * price[bar - 21]
            -0.0059060082 * price[bar - 22]
            -0.0098190256 * price[bar - 23]
            -0.0132507215 * price[bar - 24]
            -0.0161875265 * price[bar - 25]
            -0.0186164872 * price[bar - 26]
            -0.0205446727 * price[bar - 27]
            -0.0219739146 * price[bar - 28]
            -0.0229204861 * price[bar - 29]
            -0.0234080863 * price[bar - 30]
            -0.0234566315 * price[bar - 31]
            -0.0231017777 * price[bar - 32]
            -0.0223796900 * price[bar - 33]
            -0.0213300463 * price[bar - 34]
            -0.0199924534 * price[bar - 35]
            -0.0184126992 * price[bar - 36]
            -0.0166377699 * price[bar - 37]
            -0.0147139428 * price[bar - 38]
            -0.0126796776 * price[bar - 39]
            -0.0105938331 * price[bar - 40]
            -0.0084736770 * price[bar - 41]
            -0.0063841850 * price[bar - 42]
            -0.0043466731 * price[bar - 43]
            -0.0023956944 * price[bar - 44]
            -0.0005535180 * price[bar - 45]
            +0.0011421469 * price[bar - 46]
            +0.0026845693 * price[bar - 47]
            +0.0040471369 * price[bar - 48]
            +0.0052380201 * price[bar - 49]
            +0.0062194591 * price[bar - 50]
            +0.0070340085 * price[bar - 51]
            +0.0076266453 * price[bar - 52]
            +0.0080376628 * price[bar - 53]
            +0.0083037666 * price[bar - 54]
            +0.0083694798 * price[bar - 55]
            +0.0082901022 * price[bar - 56]
            +0.0080741359 * price[bar - 57]
            +0.0077543820 * price[bar - 58]
            +0.0073260526 * price[bar - 59]
            +0.0068163569 * price[bar - 60]
            +0.0062325477 * price[bar - 61]
            +0.0056078229 * price[bar - 62]
            +0.0049516078 * price[bar - 63]
            +0.0161380976 * price[bar - 64];
      //--- инициализация ячейки индикаторного буфера полученным значением FATL
      ExtLineBuffer[bar]=(FATL-SATL)/_Point;
     }
//---    
   return(rates_total);
  }
//+------------------------------------------------------------------+
