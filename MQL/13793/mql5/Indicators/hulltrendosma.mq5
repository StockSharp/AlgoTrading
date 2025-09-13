//+---------------------------------------------------------------------+
//|                                                   HullTrendOSMA.mq5 |
//|                                        Copyright � 2005, adoleh2000 |
//|                                                adoleh2000@yahoo.com |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property  copyright "Copyright � 2005, adoleh2000."
#property  link      "adoleh2000@yahoo.com"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ �������������� ����������� ������������
#property indicator_color1 clrRed,clrMagenta,clrGray,clrDodgerBlue,clrLime
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1  "HullTrendOSMA"
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4;
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
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint XLength=20;                           // ������ ����������
input Applied_price_ IPC=PRICE_CLOSE;            // ���� ����������
input Smooth_Method XMA_Method=MODE_LWMA;        // ����� ����������
input int XPhase=15;                             // �������� ����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
input uint XLength1=5;                           // ������ ����������� ����������
input Smooth_Method XMA_Method1=MODE_JJMA;       // ����� �����������
input int XPhase1=100;                           // �������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int  min_rates_1,min_rates_2,min_rates_total;
//----
int XLength2,SqrXLength;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ����������
   XLength2=int(XLength/2);
   SqrXLength=int(MathFloor(MathSqrt(XLength)));
//---- ������������� ���������� ������ ������� ������
   min_rates_1=GetStartBars(XMA_Method,XLength,XPhase);
   min_rates_2=min_rates_1+GetStartBars(XMA_Method,SqrXLength,XPhase);
   min_rates_total=min_rates_2+GetStartBars(XMA_Method1,XLength1,XPhase1)+1;
//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"HullTrendOSMA");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
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
   if(rates_total<min_rates_total) return(RESET);
//---- ���������� ��������� ���������� 
   int first,bar,clr;
//---- ���������� ���������� � ��������� ������  
   double price,xma,xma2,hma,xhma,osma,xosma,diff;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      xma2=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,price,bar,false);
      xma=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price,bar,false);
      hma=2*xma2-xma;
      xhma=XMA3.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,SqrXLength,hma,bar,false);
      osma=hma-xhma;
      xosma=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method1,XPhase1,XLength1,osma,bar,false);
      xosma/=_Point;
      IndBuffer[bar]=xosma;
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;
//---- �������� ���� ��������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      clr=2;
      xosma=IndBuffer[bar];
      diff=xosma-IndBuffer[bar-1];
      //----
      if(xosma>0)
        {
         if(diff>0) clr=4;
         if(diff<0) clr=3;
        }
      //----
      if(xosma<0)
        {
         if(diff<0) clr=0;
         if(diff>0) clr=1;
        }
      //----
      ColorIndBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
