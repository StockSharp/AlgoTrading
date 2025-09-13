//+---------------------------------------------------------------------+
//|                                                     CoppockHist.mq5 |
//|                                 Based on Coppock.mq4 by Robert Hill |
//|                                     Copyright � 2010, EarnForex.com |
//|                                           http://www.earnforex.com/ |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2010, EarnForex.com"
#property link      "http://www.earnforex.com/"
//---- ����� ������ ����������
#property version   "1.00"
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
#property indicator_color1 clrDeepPink,clrViolet,clrGray,clrDodgerBlue,clrLime
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "Coppock"
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
input uint ROC1Period = 14;
input uint ROC2Period = 11;
input uint SmoothPeriod=3; // ������ ����������� ���������� �����
input ENUM_MA_METHOD MA_Method_=MODE_SMA; // ����� ���������� ���������� �����
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
   min_rates_total=int(MathMax(ROC1Period,ROC2Period)+SmoothPeriod+2);
//---- ����������� ������������� ������� MAMABuffer � ������������ �����
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"Coppock");
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   double price0,price1,price2,diff,ROCSum;
//---- ���������� ������������� ����������
   int first,bar,clr;
   static int startbar1;
//---- ������������� ���������� � ����� OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      first=int(MathMax(ROC1Period,ROC2Period)); // ��������� ����� ��� ������� ���� ����� ������� �����
      startbar1=first;
     }
   else // ��������� ����� ��� ������� ����� �����
     {
      first=prev_calculated-1;
     }
//---- ���������� ���������� ������ Moving_Average
   static CMoving_Average SMOOTH;
//---- �������� ���� ������� ������� ����� ������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ������ ������� PriceSeries ��� ��������� ������� ���� Series
      price0=PriceSeries(AppliedPrice,bar,open,low,high,close);
      price1=PriceSeries(AppliedPrice,bar-ROC1Period,open,low,high,close);
      price2=PriceSeries(AppliedPrice,bar-ROC2Period,open,low,high,close);
      ROCSum=(price0-price1)/price1+(price0-price2)/price2;
      ExtBuffer[bar]=SMOOTH.MASeries(startbar1,prev_calculated,rates_total,SmoothPeriod,MA_Method_,ROCSum,bar,false);
     }

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
