//+------------------------------------------------------------------+
//|                                                 KaufWMAcross.mq5 |
//|                                     Copyright � 2007, John Smith |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2007, John Smith"
#property link "http://www.metaquotes.net"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color1  clrSalmon
//--- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//--- ����������� ����� ����� ����������
#property indicator_label1  "KaufWMAcross Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ����� ����� ���������� ����������� ������� ����
#property indicator_color2  clrLime
//--- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//--- ����������� ��������� ����� ����������
#property indicator_label2 "KaufWMAcross Buy"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0          // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
//--- ��������� AMA
input uint ama_period=9;                            // ������ AMA
input uint fast_ma_period=2;                        // ������ ������� ����������
input uint slow_ma_period=30;                       // ������ ��������� ����������
input ENUM_APPLIED_PRICE  AMAPrice=PRICE_CLOSE;     // ���� AMA
//--- ��������� ����������� ��������
input uint  MAPeriod=13;                            // ������ MA
input  ENUM_MA_METHOD   MAType=MODE_LWMA;           // ����� ���������� MA
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;     // ���� MA
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� �����
//--- � ���������� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//--- ���������� ������������� ���������� ��� ������� �����������
int ATR_Handle,AMA_Handle,MA_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ������������� ���������� ���������� 
   int ATR_Period=15;
   min_rates_total=int(MathMax(MathMax(ATR_Period,ama_period+1),MAPeriod+1));
//--- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� iMA
   MA_Handle=iMA(NULL,0,MAPeriod,0,MAType,MAPrice);
   if(MA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMA");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� iAMA
   AMA_Handle=iAMA(NULL,0,ama_period,fast_ma_period,slow_ma_period,0,AMAPrice);
   if(AMA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iAMA");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//--- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="KaufWMAcross";
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
   if(BarsCalculated(AMA_Handle)<rates_total
      || BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double AMA[],MA[],ATR[];
//--- ������� ������������ ���������� ���������� ������ �
//--- ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
   to_copy=limit+2;
//--- �������� ����� ����������� ������ � �������
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);
   if(CopyBuffer(AMA_Handle,0,0,to_copy,AMA)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(MA,true);
   ArraySetAsSeries(AMA,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      if(AMA[bar+1]>MA[bar+1] && AMA[bar]<MA[bar]) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
      if(AMA[bar+1]<MA[bar+1] && AMA[bar]>MA[bar]) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+