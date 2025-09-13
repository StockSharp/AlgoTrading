//+------------------------------------------------------------------+
//|                                           ElderImpulseSystem.mq5 |
//|                             Copyright � 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Elder Impuls System"
//--- ����� ������ ����������
#property version   "1.00"
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 5
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//--- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1  Blue,Red,Green
//--- ����������� ����� ����������
#property indicator_label1  "Open; High; Low; Close"
//+-----------------------------------+
//| ���������� ��������               |
//+-----------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input int ma_period=13;          // ������ �������
input int fast_ema_period = 12;  // ������� ������ MACD 
input int slow_ema_period = 26;  // ��������� ������ MACD
input int signal_period=9;       // ���������� ������ MACD
//+-----------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//--- ���������� ������������� ���������� ��� ������� �����������
int MA_Handle,MACD_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=MathMax(ma_period,signal_period+1)+2;
//--- ��������� ������ ���������� iMA
   MA_Handle=iMA(NULL,0,ma_period,0,MODE_EMA,PRICE_CLOSE);
   if(MA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMA");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� iMACD
   MACD_Handle=iMACD(NULL,0,fast_ema_period,slow_ema_period,signal_period,PRICE_CLOSE);
   if(MACD_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMACD");
      return(INIT_FAILED);
     }
//--- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);
   ArraySetAsSeries(ExtColorBuffer,true);
//--- ����������� ������������� ������� ExtColorBuffer[] � �������� ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ��� ��� ���� ������ � ����� ��� �������
   string short_name;
   StringConcatenate(short_name,"Elder Impuls System(",
                     ma_period,", ",fast_ema_period,", ",slow_ema_period,", ",signal_period,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---
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
   if(BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(MACD_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double MA[],MACDM[],MACDS[];
   double dma,dmacd0,dmacd1;
//--- ������� ������������ ���������� ���������� ������ � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1-min_rates_total; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated;   // ��������� ����� ��� ������� ����� �����
     }
   to_copy=limit+2;
//--- �������� ����� ����������� ������ � �������
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle,0,0,to_copy,MACDM)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle,1,0,to_copy,MACDS)<=0) return(RESET);
//--- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(MA,true);
   ArraySetAsSeries(MACDM,true);
   ArraySetAsSeries(MACDS,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ExtOpenBuffer [bar]=open[bar];
      ExtCloseBuffer[bar]=close[bar];
      ExtHighBuffer [bar]=high[bar];
      ExtLowBuffer  [bar]=low[bar];
      //---
      dma=MA[bar]-MA[bar+1];
      dmacd0=MACDM[bar]-MACDS[bar];
      dmacd1=MACDM[bar+1]-MACDS[bar+1];
      //---
      if(dma>0 && dmacd0 > dmacd1 && dmacd0>0) ExtColorBuffer[bar]=2;
      if(dma<0 && dmacd0 < dmacd1 && dmacd0<0) ExtColorBuffer[bar]=1;
      //---
      if(MA[bar]<=MA[bar+1] && dmacd0>0 || dma<=0 && dmacd0>dmacd1 || dma>=0 && dmacd0<0 || dma>=0 && dmacd0<dmacd1)
         ExtColorBuffer[bar]=0;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
