//+------------------------------------------------------------------+
//|                                                   MACDCandle.mq5 |
//|                               Copyright � 2013, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2013, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "MACDCandle Smoothed"
//---- ����� ������ ����������
#property version   "1.00"
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 5
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrDeepPink,clrBlue,clrTeal
//---- ����������� ����� ����������
#property indicator_label1  "MACDCandle Open;MACDCandle High;MACDCandle Low;MACDCandle Close"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum MODE
  {
   MODE_HISTOGRAM=0,    // �����������
   MODE_SIGNAL_LINE=1   // ���������� �����
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint  fast_ema_period=12;             // ������ ������� ������� 
input uint  slow_ema_period=26;             // ������ ��������� ������� 
input uint  signal_period=9;                // ������ ���������� �������� 
input MODE  mode=MODE_SIGNAL_LINE;          // �������� ����� ��� �������
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int MACD_Handle[4];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ���������� 
   min_rates_total=int(MathMax(fast_ema_period,slow_ema_period));
   if(mode==MODE_SIGNAL_LINE) min_rates_total+=int(signal_period);
//---- ��������� ������ ���������� iMACD
   MACD_Handle[0]=iMACD(NULL,0,fast_ema_period,slow_ema_period,signal_period,PRICE_OPEN);
   if(MACD_Handle[0]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMACD["+string(0)+"]!");
      return(INIT_FAILED);
     }
   MACD_Handle[1]=iMACD(NULL,0,fast_ema_period,slow_ema_period,signal_period,PRICE_HIGH);
   if(MACD_Handle[1]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMACD["+string(1)+"]!");
      return(INIT_FAILED);
     }
   MACD_Handle[2]=iMACD(NULL,0,fast_ema_period,slow_ema_period,signal_period,PRICE_LOW);
   if(MACD_Handle[2]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMACD["+string(2)+"]!");
      return(INIT_FAILED);
     }
   MACD_Handle[3]=iMACD(NULL,0,fast_ema_period,slow_ema_period,signal_period,PRICE_CLOSE);
   if(MACD_Handle[3]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMACD["+string(3)+"]!");
      return(INIT_FAILED);
     }
//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������� ��� � ����������
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);
   ArraySetAsSeries(ExtColorBuffer,true);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="MACDCandl";
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
   if(BarsCalculated(MACD_Handle[0])<rates_total
      || BarsCalculated(MACD_Handle[1])<rates_total
      || BarsCalculated(MACD_Handle[2])<rates_total
      || BarsCalculated(MACD_Handle[3])<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
//---- ������� ������������ ���������� ���������� ������ � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
//---
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(MACD_Handle[0],int(mode),0,to_copy,ExtOpenBuffer)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle[1],int(mode),0,to_copy,ExtHighBuffer)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle[2],int(mode),0,to_copy,ExtLowBuffer)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle[3],int(mode),0,to_copy,ExtCloseBuffer)<=0) return(RESET);
//---- �������� ���� ����������� � ����������� ������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double Max=MathMax(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);
      double Min=MathMin(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);
      //---
      ExtHighBuffer[bar]=MathMax(Max,ExtHighBuffer[bar])/_Point;
      ExtLowBuffer[bar]=MathMin(Min,ExtLowBuffer[bar])/_Point;
      //---
      ExtOpenBuffer[bar]/=_Point;
      ExtCloseBuffer[bar]/=_Point;
      //---
      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=2.0;
      else if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else ExtColorBuffer[bar]=1.0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
