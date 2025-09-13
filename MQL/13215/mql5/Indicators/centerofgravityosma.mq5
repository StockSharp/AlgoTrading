//+---------------------------------------------------------------------+
//|                                             CenterOfGravityOSMA.mq5 |
//|                         Copyright � 2007, MetaQuotes Software Corp. |
//|                                           http://www.metaquotes.net |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---- ����� ������ ����������
#property version   "1.11"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ �������������� ����������� ������������
#property indicator_color1 clrMagenta,clrViolet,clrGray,clrDodgerBlue,clrAqua
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "CenterOfGravityOSMA"
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //PRICE_CLOSE
   PRICE_OPEN_,          //PRICE_OPEN
   PRICE_HIGH_,          //PRICE_HIGH
   PRICE_LOW_,           //PRICE_LOW
   PRICE_MEDIAN_,        //PRICE_MEDIAN
   PRICE_TYPICAL_,       //PRICE_TYPICAL
   PRICE_WEIGHTED_,      //PRICE_WEIGHTED
   PRICE_SIMPL_,         //PRICE_SIMPL_
   PRICE_QUARTER_,       //PRICE_QUARTER_
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint Period_=10; // ������ ���������� ����������
input uint SmoothPeriod1=3; // ������ ����������� ���������� �����
input ENUM_MA_METHOD MA_Method_1=MODE_SMA; // ����� ���������� ���������� �����
input uint SmoothPeriod2=3; // ������ ����������� ���������� �����
input ENUM_MA_METHOD MA_Method_2=MODE_SMA; // ����� ���������� ���������� �����
input Applied_price_ AppliedPrice=PRICE_CLOSE_;// ������� ���������
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtBuffer[];
double ColorExtBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| �������� ������� iPriceSeries                                    |
//| �������� ������ Moving_Average                                   | 
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ��������
   min_rates_total=int(Period_+1+SmoothPeriod1+SmoothPeriod2+2);
//---- ����������� ������������� ������� MAMABuffer � ������������ �����
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Center of Gravity OSMA(",Period_,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
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
   if(rates_total<min_rates_total) return(0);
//---- ���������� ���������� � ��������� ������  
   double price,sma,lwma,res1,res2,res3,diff;
//---- ���������� ������������� ����������
   int first,bar,clr;
   static int startbar1,startbar2;
//---- ������������� ���������� � ����� OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� ����� ������� �����
      startbar1=int(Period_+1);
      startbar2=startbar1+int(SmoothPeriod1);
     }
   else // ��������� ����� ��� ������� ����� �����
     {
      first=prev_calculated-1;
     }
//---- ���������� ���������� ������ Moving_Average
   static CMoving_Average MA,LWMA,SIGN,SMOOTH;
//---- �������� ���� ������� ������� ����� ������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� Series
      price=PriceSeries(AppliedPrice,bar,open,low,high,close);
      //----
      sma=MA.MASeries(0,prev_calculated,rates_total,Period_,MODE_SMA,price,bar,false);
      lwma=LWMA.MASeries(0,prev_calculated,rates_total,Period_,MODE_LWMA,price,bar,false);
      //----
      res1=sma*lwma/_Point;
      res2=SIGN.MASeries(startbar1,prev_calculated,rates_total,SmoothPeriod1,MA_Method_1,res1,bar,false);
      res3=res1-res2;
      ExtBuffer[bar]=SMOOTH.MASeries(startbar2,prev_calculated,rates_total,SmoothPeriod2,MA_Method_2,res3,bar,false);
     }
//---
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      first=min_rates_total;
//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      clr=2;
      diff=ExtBuffer[bar]-ExtBuffer[bar-1];
//---
      if(ExtBuffer[bar]>0)
        {
         if(diff>0) clr=4;
         if(diff<0) clr=3;
        }
//---
      if(ExtBuffer[bar]<0)
        {
         if(diff<0) clr=0;
         if(diff>0) clr=1;
        }
//---
      ColorExtBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
