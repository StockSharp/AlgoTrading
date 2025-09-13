//+------------------------------------------------------------------+
//|                                                       Stalin.mq5 |
//|                   Copyright © 2011, Andrey Vassiliev (MoneyJinn) |
//|                                         http://www.vassiliev.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Andrey Vassiliev (MoneyJinn)"
#property link      "http://www.vassiliev.ru/"
//---- номер версии индикатора
#property version   "1.00"
//---- отрисовка индикатора в главном окне
#property indicator_chart_window 
//---- для расчета и отрисовки индикатора использовано два буфера
#property indicator_buffers 2
//---- использовано всего два графических построения
#property indicator_plots   2
//+----------------------------------------------+
//|  объявление констант                         |
//+----------------------------------------------+
#define RESET 0 // константа для возврата терминалу команды на пересчёт индикатора
//+----------------------------------------------+
//|  Параметры отрисовки медвежьего индикатора   |
//+----------------------------------------------+
//---- отрисовка индикатора 1 в виде символа
#property indicator_type1   DRAW_ARROW
//---- в качестве цвета медвежьей линии индикатора использован LightPink цвет
#property indicator_color1  LightPink
//---- толщина линии индикатора 1 равна 4
#property indicator_width1  4
//---- отображение бычьей метки индикатора
#property indicator_label1  "Silver Sell"
//+----------------------------------------------+
//|  Параметры отрисовки бычьего индикатора      |
//+----------------------------------------------+
//---- отрисовка индикатора 2 в виде символа
#property indicator_type2   DRAW_ARROW
//---- в качестве цвета бычьей линии индикатора использован LightSkyBlue цвет
#property indicator_color2  LightSkyBlue
//---- толщина линии индикатора 2 равна 4
#property indicator_width2  4
//---- отображение медвежьей метки индикатора
#property indicator_label2 "Silver Buy"

//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input ENUM_MA_METHOD MAMethod=MODE_EMA;
input int    MAShift=0;
input int    Fast=14;
input int    Slow=21;
input int    RSI=17;
input int    Confirm=0.0;
input int    Flat=0.0;
input bool   SoundAlert=false;
input bool   EmailAlert=false;
//+----------------------------------------------+

//---- объявление динамических массивов, которые будут в 
// дальнейшем использованы в качестве индикаторных буферов
double SellBuffer[];
double BuyBuffer[];
//----
double IUP,IDN,E1,E2,Confirm2,Flat2;
//---- Объявление целых переменных начала отсчета данных
int StartBars;
//---- Объявление целых переменных для хранения хендлов индикаторов
int SLMA_Handle,FSMA_Handle,RSI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void BU(int i,const double &Low[],const datetime &Time[])
  {
//----
   if(Low[i]>=(E1+Flat2) || Low[i]<=(E1-Flat2))
     {
      BuyBuffer[i]=Low[i];
      E1=BuyBuffer[i];
      Alerts(i,"UP "+Symbol()+" "+TimeToString(Time[i]));
     }
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void BD(int i,const double &High[],const datetime &Time[])
  {
//----
   if(High[i]>=(E2+Flat2) || High[i]<=(E2-Flat2))
     {
      SellBuffer[i]=High[i];
      E2=SellBuffer[i];
      Alerts(i,"DN "+Symbol()+" "+TimeToString(Time[i]));
     }
//---- 
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void Alerts(int pos,string txt)
  {
//----
   if(SoundAlert==true&&pos==1){PlaySound("alert.wav");}
   if(EmailAlert==true&&pos==1){SendMail("Stalin alert signal: "+txt,txt);}
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- Инициализация переменных начала отсчета данных
   StartBars=MathMax(RSI,MathMax(Slow,Fast));

//---- Инициализация переменных
   IUP=0;
   IDN=0;
   E1=0;
   E2=0;

   if(_Digits==3 || _Digits==5)
     {
      double Point10=10*_Point;
      Confirm2=Point10;
      Flat2=Flat*Point10;
     }
   else
     {
      Confirm2=Confirm*_Point;
      Flat2=Flat*_Point;
     }

//---- получение хендла индикатора iRSI
   if(RSI)
     {
      RSI_Handle=iRSI(NULL,0,RSI,PRICE_CLOSE);
      if(RSI_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iRSI");
      Print("RSI=",RSI);
     }
//---- получение хендла индикатора iMA
   SLMA_Handle=iMA(NULL,0,Slow,MAShift,MAMethod,PRICE_CLOSE);
   if(SLMA_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iMA");
//---- получение хендла индикатора iMA
   FSMA_Handle=iMA(NULL,0,Fast,MAShift,MAMethod,PRICE_CLOSE);
   if(FSMA_Handle==INVALID_HANDLE) Print(" Не удалось получить хендл индикатора iMA");

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- создание метки для отображения в DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Stalin Sell");
//---- символ для индикатора
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(SellBuffer,true);

//---- превращение динамического массива в индикаторный буфер
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- осуществление сдвига начала отсчета отрисовки индикатора 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//---- создание метки для отображения в DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Stalin Buy");
//---- символ для индикатора
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- установка значений индикатора, которые не будут видимы на графике
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- индексация элементов в буфере, как в таймсерии
   ArraySetAsSeries(BuyBuffer,true);

//---- Установка формата точности отображения индикатора
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- имя для окон данных и метка для подокон 
   string short_name="Stalin";
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
   if(RSI && BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(SLMA_Handle)<rates_total
      || BarsCalculated(FSMA_Handle)<rates_total
      || rates_total<StartBars)
      return(RESET);

//---- объявления локальных переменных 
   int to_copy,limit;
   double RSI_[],SLMA_[],FSMA_[];

//---- расчеты необходимого количества копируемых данных и
//стартового номера limit для цикла пересчета баров
   if(prev_calculated>rates_total || prev_calculated<=0)// проверка на первый старт расчета индикатора
     {
      limit=rates_total-StartBars; // стартовый номер для расчета всех баров
      to_copy=rates_total; // расчетное количество всех баров
     }
   else
     {
      limit=rates_total-prev_calculated; // стартовый номер для расчета новых баров
      to_copy=limit+2; // расчетное количество только новых баров
     }

//---- индексация элементов в массивах, как в таймсериях
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(time,true);
   ArraySetAsSeries(SLMA_,true);
   ArraySetAsSeries(FSMA_,true);

//---- копируем вновь появившиеся данные в массивы
   if(CopyBuffer(SLMA_Handle,0,0,to_copy,SLMA_)<=0) return(RESET);
   if(CopyBuffer(FSMA_Handle,0,0,to_copy,FSMA_)<=0) return(RESET);

   if(RSI)
     {
      ArraySetAsSeries(RSI_,true);
      if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI_)<=0) return(RESET);
     }

//---- основной цикл расчета индикатора
   for(int bar=limit; bar>=0; bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(!RSI||FSMA_[bar+1]<SLMA_[bar+1]&&FSMA_[bar]>SLMA_[bar]&&(RSI_[bar]>50)){if(!Confirm2)BU(bar,low, time); else{IUP=low[bar]; IDN=0;}}
      if(!RSI||FSMA_[bar+1]>SLMA_[bar+1]&&FSMA_[bar]<SLMA_[bar]&&(RSI_[bar]<50)){if(!Confirm2)BD(bar,high,time); else{IDN=high[bar];IUP=0;}}
      if(IUP && high[bar]-IUP>=Confirm2 && close[bar]<=high[bar]){BU(bar,low,time); IUP=0;}
      if(IDN && IDN-low[bar]>=Confirm2 && open[bar]>=close[bar]){BD(bar,high,time);IDN=0;}
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
