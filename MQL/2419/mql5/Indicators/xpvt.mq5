//+------------------------------------------------------------------+
//|                                                         XPVT.mq5 |
//|                                     Copyright � 2010, Martingeil | 
//+------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2010, Martingeil"
//--- ������ �� ���� ������
#property link ""
#property description "Price and Volume Trend"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� 1             |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//--- � �������� ����� ����� ���������� ����������� Red ����
#property indicator_color1  Red
//--- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//--- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//--- ����������� ����� ����� ����������
#property indicator_label1  "PVT"
//+----------------------------------------------+
//| ��������� ��������� ���������� 2             |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//--- � �������� ����� ����� ���������� ����������� ����� ����
#property indicator_color2  Blue
//--- ����� ���������� 2 - �������������
#property indicator_style2  STYLE_DASHDOTDOT
//--- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//--- ����������� ����� ����� ����������
#property indicator_label2  "Signal PVT"
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA;
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
/*enum Smooth_Method - ������������ ��������� � ����� SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_   //TrendFollow_2 Price 
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK; // �����
input Smooth_Method XMA_Method=MODE_EMA;          // ����� ����������
input int XLength=5;                              // ������� �����������
input int XPhase=15;                              // �������� �����������
input Applied_price_ IPC=PRICE_CLOSE;             // ������� ���������
input int Shift=0;                                // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double PVTBuffer[],SignBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=XMA.GetStartBars(XMA_Method,XLength,XPhase)+1;
//--- ����������� ������������� ������� SignBuffer � ������������ �����
   SetIndexBuffer(0,PVTBuffer,INDICATOR_DATA);
//--- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ���������� 1 �� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ����������� ������������� ������� PVTBuffer � ������������ �����
   SetIndexBuffer(1,SignBuffer,INDICATOR_DATA);
//--- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ���������� 2 �� 1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"Price and Volume Trend");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);
//--- ���������� ��������� ���������� 
   double dCurrentPrice,dPreviousPrice;
   int first,bar;
   long Vol;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=1; // ��������� ����� ��� ������� ���� �����
      if(VolumeType==VOLUME_TICK) PVTBuffer[0]=double(tick_volume[0]);
      else  PVTBuffer[0]=double(volume[0]);
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      if(VolumeType==VOLUME_TICK) Vol=long(tick_volume[bar]);
      else Vol=long(volume[bar]);
      //--- ����� ������� PriceSeries ��� ��������� ������� ���� price_
      dCurrentPrice=PriceSeries(IPC,bar,open,low,high,close);
      dPreviousPrice=PriceSeries(IPC,bar-1,open,low,high,close);
      //---
      PVTBuffer[bar]=PVTBuffer[bar-1]+Vol*(dCurrentPrice-dPreviousPrice)/dPreviousPrice;
      SignBuffer[bar]=XMA.XMASeries(1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,PVTBuffer[bar],bar,false);
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
