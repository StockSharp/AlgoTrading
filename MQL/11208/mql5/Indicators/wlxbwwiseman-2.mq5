//+------------------------------------------------------------------+
//|                                               wlxBWWiseMan-2.mq5 |
//|                                          Copyright � 2005, wellx |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2005, wellx"
//--- ������ �� ���� ������
#property link      "http://www.metaquotes.net"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color1  clrMagenta
//--- ������� ����� ���������� 1 ����� 3
#property indicator_width1  4
//--- ����������� ����� ����� ����������
#property indicator_label1  "wlxBWWiseMan-2 Sell"
//+----------------------------------------------+
//| ��������� ��������� ������ ����������        |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ����� ����� ���������� ����������� ������� ����
#property indicator_color2  clrLimeGreen
//--- ������� ����� ���������� 2 ����� 3
#property indicator_width2  4
//--- ����������� ��������� ����� ����������
#property indicator_label2 "wlxBWWiseMan-2 Buy"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint updown=10;
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//--- ���������� ������������� ���������� ��� ������� �����������
int ATR_Handle,AO_Handle;
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ������������� ���������� ���������� 
   int ATR_Period=15;
   min_rates_total=int(MathMax(ATR_Period,33+5));
//--- ��������� ������ ���������� ATR
   ATR_Handle=iATR(Symbol(),PERIOD_CURRENT,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� Awesome Oscillator 
   AO_Handle=iAO(Symbol(),PERIOD_CURRENT);
   if(AO_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Awesome Oscillator");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,93);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,93);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//--- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="wlxBWWiseMan-2";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//--- ���������� �������������
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
//--- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(AO_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double AO[],ATR[];
//--- ������� ������������ ���������� ���������� ������ �
//--- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
   to_copy=limit+1;
//--- �������� ����� ����������� ������ � ������� AO[] � ATR[]
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   to_copy+=4;
   if(CopyBuffer(AO_Handle,0,0,to_copy,AO)<=0) return(RESET);
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(AO,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      if(AO[bar+4]>0.0 && AO[bar+3]>0.0 && AO[bar+4]<AO[bar+3] && AO[bar+3]>AO[bar+2] && AO[bar+2]>AO[bar+1] && AO[bar+1]>AO[bar]) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
      if(AO[bar+4]<0.0 && AO[bar+3]<0.0 && AO[bar+4]>AO[bar+3] && AO[bar+3]<AO[bar+2] && AO[bar+2]<AO[bar+1] && AO[bar+1]<AO[bar]) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
