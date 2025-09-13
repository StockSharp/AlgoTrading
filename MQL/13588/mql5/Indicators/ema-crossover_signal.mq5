//+------------------------------------------------------------------+
//|                                         EMA-Crossover_Signal.mq5 |
//|         Copyright � 2005, Jason Robinson (jnrtrading)            |
//|                   http://www.jnrtading.co.uk                     |
//+------------------------------------------------------------------+
/*
  +------------------------------------------------------------------+
  | Allows you to enter two ema periods and it will then show you at |
  | Which point they crossed over. It is more usful on the shorter   |
  | periods that get obscured by the bars / candlesticks and when    |
  | the zoom level is out. Also allows you then to remove the emas   |
  | from the chart. (emas are initially set at 5 and 6)              |
  +------------------------------------------------------------------+
*/
#property copyright "Copyright � 2005, Jason Robinson (jnrtrading)"
#property link "http://www.jnrtading.co.uk"
#property description "EMA-Crossover_Signal"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color1  clrMagenta
//---- ������� ���������� 1 ����� 1
#property indicator_width1  1
//---- ����������� ��������� ����� ����������
#property indicator_label1  "EMA-Crossover_Signal Sell"
//+----------------------------------------------+
//| ��������� ��������� ������� ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ����� ����
#property indicator_color2  clrBlue
//---- ������� ���������� 2 ����� 1
#property indicator_width2  1
//---- ����������� ������ ����� ����������
#property indicator_label2 "EMA-Crossover_Signal Buy"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint FasterMA=5;
input uint SlowerMA=6;
input  ENUM_MA_METHOD   MAType=MODE_LWMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ��� ������� �����������
int FsMA_Handle,SlMA_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ����������   
   min_rates_total=int(MathMax(FasterMA,SlowerMA)+3);
//---- ��������� ������ ���������� iMA
   FsMA_Handle=iMA(NULL,0,FasterMA,0,MAType,MAPrice);
   if(FsMA_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iMA");
//---- ��������� ������ ���������� iMA
   SlMA_Handle=iMA(NULL,0,SlowerMA,0,MAType,MAPrice);
   if(SlMA_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iMA");
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,119);
//---- ���������� ��������� � ������, ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,119);
//---- ���������� ��������� � ������, ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);
//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="EMA-Crossover_Signal";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(FsMA_Handle)<rates_total
      || BarsCalculated(SlMA_Handle)<rates_total
      || rates_total<min_rates_total)return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double AvgRange,Range,FsMA[],SlMA[];
//---- ������� ������������ ���������� ���������� ������ �
//---- ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(FsMA,true);
   ArraySetAsSeries(SlMA,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//----
   to_copy=limit+3;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(FsMA_Handle,0,0,to_copy,FsMA)<=0) return(RESET);
   if(CopyBuffer(SlMA_Handle,0,0,to_copy,SlMA)<=0) return(RESET);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      AvgRange=0.0;
      for(int count=bar+9; count>=bar; count--) AvgRange+=MathAbs(high[count]-low[count]);
      Range=AvgRange/10;
      //----
      SellBuffer[bar]=0.0;
      BuyBuffer[bar]=0.0;
      //----
      if(FsMA[bar+1]>SlMA[bar+1] && FsMA[bar+2]<SlMA[bar+2] && FsMA[bar]>SlMA[bar]) BuyBuffer[bar]=low[bar]-Range*0.5;
      else if(FsMA[bar+1]<SlMA[bar+1] && FsMA[bar+2]>SlMA[bar+2] && FsMA[bar]<SlMA[bar]) SellBuffer[bar]=high[bar]+Range*0.5;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
