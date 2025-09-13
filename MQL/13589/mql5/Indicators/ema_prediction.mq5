//+------------------------------------------------------------------+
//|                                               EMA_Prediction.mq5 |
//|                                     Copyright � 2008, Codersguru |
//|                                         http://www.forex-tsd.com |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2008, Codersguru"
//---- ������ �� ���� ������
#property link      "http://www.forex-tsd.com"
//---- ����� ������ ����������
#property version   "1.01"
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
//---- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color1  clrMagenta
//---- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//---- ����������� ��������� ����� ����������
#property indicator_label1  "EMA_Prediction Sell"
//+----------------------------------------------+
//| ��������� ��������� ������� ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ������ ����� ���������� ����������� ������� ����
#property indicator_color2  clrLime
//---- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//---- ����������� ������ ����� ����������
#property indicator_label2 "EMA_Prediction Buy"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint               FastMAPeriod=1;
input  ENUM_MA_METHOD    FastMAType=MODE_EMA;
input ENUM_APPLIED_PRICE FastMAPrice=PRICE_CLOSE;
input uint               SlowMAPeriod=2;
input  ENUM_MA_METHOD    SlowMAType=MODE_EMA;
input ENUM_APPLIED_PRICE SlowMAPrice=PRICE_CLOSE;
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int ATR_Handle,FsMA_Handle,SlMA_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ����������
   int ATR_Period=12;
   min_rates_total=int(MathMax(MathMax(FastMAPeriod,SlowMAPeriod),ATR_Period))+1;
//---- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iATR!");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� Fast iMA
   FsMA_Handle=iMA(NULL,0,FastMAPeriod,0,FastMAType,FastMAPrice);
   if(FsMA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Fast iMA");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� Slow iMA
   SlMA_Handle=iMA(NULL,0,SlowMAPeriod,0,SlowMAType,SlowMAPrice);
   if(SlMA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Slow iMA");
      return(INIT_FAILED);
     }
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"EMA_Prediction Sell");
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"EMA_Prediction Buy");
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="EMA_Prediction";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---- ���������� �������������
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(FsMA_Handle)<rates_total
      || BarsCalculated(SlMA_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double ATR[],FsMA[],SlMA[];
//---- ������� ������������ ���������� ���������� ������ �
//---- ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
//----
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   to_copy++;
   if(CopyBuffer(FsMA_Handle,0,0,to_copy,FsMA)<=0) return(RESET);
   if(CopyBuffer(SlMA_Handle,0,0,to_copy,SlMA)<=0) return(RESET);
//---- ���������� ��������� � �������� ��� � ����������
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(FsMA,true);
   ArraySetAsSeries(SlMA,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //----
      if(FsMA[bar+1]<SlMA[bar+1] && FsMA[bar]>SlMA[bar] && open[bar]<close[bar]) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
      if(FsMA[bar+1]>SlMA[bar+1] && FsMA[bar]<SlMA[bar] && open[bar]>close[bar]) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
