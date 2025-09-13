//+---------------------------------------------------------------------+
//|                                                            XRSX.mq5 | 
//|                                Copyright � 2011,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Relative Strength Index"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ �������
#property indicator_buffers 4 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� 1             |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������� �����������
#property indicator_type1   DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ����������� ������������
#property indicator_color1 clrGray,clrGreen,clrBlue,clrRed,clrMagenta
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "XRSX"
//+----------------------------------------------+
//| ��������� ��������� ���������� 2             |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ����������� �����
#property indicator_type2   DRAW_COLOR_LINE
//---- � �������� ����� ����� ���������� ������������
#property indicator_color2 clrGray,clrLime,clrDarkOrange
//---- ����� ���������� - �����
#property indicator_style2  STYLE_DASH
//---- ������� ����� ���������� ����� 2
#property indicator_width2  2
//---- ����������� ����� ����������
#property indicator_label2  "Signal"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1  -50.0
#property indicator_level2  +50.0
#property indicator_levelcolor clrViolet
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA UPXRSX,DNXRSX,XSIGN;
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
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_   //TrendFollow_2 Price 
  };
//---
enum IndStyle //����� ����������� ����������
  {
   COLOR_LINE = DRAW_COLOR_LINE,          //������� �����
   COLOR_HISTOGRAM=DRAW_COLOR_HISTOGRAM,  //������� �����������
   COLOR_ARROW=DRAW_COLOR_ARROW           //������� ������
  };
//---
/*enum Smooth_Method - ��������� � ����� SmoothAlgorithms.mqh
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
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method DSmoothMethod=MODE_JJMA; // ����� ���������� ����
input int DPeriod=15;  // ������ ���������� �������
input int DPhase=100;  // �������� ���������� ���������� �������
                       // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                       // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method SSmoothMethod=MODE_JurX; // ����� ���������� ���������� �����
input int SPeriod=7;  // ������ ���������� �����
input int SPhase=100; // �������� ���������� �����
                      // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                      // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE; // ������� ���������
/* , �� ������� ������������ ������ ���������� ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // ����� ���������� �� ����������� � �����
input IndStyle Style=COLOR_HISTOGRAM; // ����� ����������� XRSX
//+----------------------------------------------+
//---- ���������� ������������� ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double XRSX[],XXRSX[];
double ColorXRSX[],ColorXXRSX[];
//---- ���������� ������������� ���������� ������ ������� ������
int StartBars,StartBarsD,StartBarsS;
//+------------------------------------------------------------------+   
//| XRSX indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   StartBarsD=UPXRSX.GetStartBars(DSmoothMethod,DPeriod,DPhase)+1;
   StartBarsS=StartBarsD+UPXRSX.GetStartBars(SSmoothMethod,SPeriod,SPhase);
   StartBars=StartBarsS;
//---- ��������� ������� �� ������������ �������� ������� ����������
   UPXRSX.XMALengthCheck("DPeriod", DPeriod);
   UPXRSX.XMALengthCheck("SPeriod", SPeriod);
//---- ��������� ������� �� ������������ �������� ������� ����������
   UPXRSX.XMAPhaseCheck("DPhase",DPhase,DSmoothMethod);
   UPXRSX.XMAPhaseCheck("SPhase",SPhase,SSmoothMethod);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,XRSX,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"XRSX");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ��������� ����� ����������� ����������   
   PlotIndexSetInteger(0,PLOT_DRAW_TYPE,Style);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorXRSX,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBarsD+1);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,XXRSX,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"Signal line");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(3,ColorXXRSX,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,StartBars+1);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname,Smooth;
   Smooth=UPXRSX.GetString_MA_Method(DSmoothMethod);
   StringConcatenate(shortname,"Relative Strength Index(",string(DPeriod),",",Smooth,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| XRSX iteration function                                          | 
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
   if(rates_total<StartBars) return(0);
//---- ���������� ���������� � ��������� ������  
   double dprice_,absdprice_,up_xrsx,dn_xrsx,xrsx,xxrsx;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first1,first2,first3,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first1=1; // ��������� ����� ��� ������� ���� �����
      first2=StartBarsD+1;
      first3=StartBars+1;
     }
   else
     {
      first1=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
      first2=first1;
      first3=first1;
     }
//---- �������� ���� ������� ����������
   for(bar=first1; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ���������� ������� ���� dprice_
      dprice_=PriceSeries(IPC,bar,open,low,high,close)-PriceSeries(IPC,bar-1,open,low,high,close);
      absdprice_=MathAbs(dprice_);
      //---- ��� ������ ������� XMASeries
      up_xrsx = UPXRSX.XMASeries(1, prev_calculated, rates_total, DSmoothMethod, DPhase, DPeriod,    dprice_, bar, false);
      dn_xrsx = DNXRSX.XMASeries(1, prev_calculated, rates_total, DSmoothMethod, DPhase, DPeriod, absdprice_, bar, false);
      //---- �������������� ������� �� ���� �� ������ ���������
      if(dn_xrsx==0) xrsx=EMPTY_VALUE;
      else
        {
         xrsx=up_xrsx/dn_xrsx;
         //---- ����������� ���������� ������ � ����� 
         if(xrsx > +1)xrsx = +1;
         if(xrsx < -1)xrsx = -1;
        }
      //---- �������� ����������� �������� � ������������ �����
      XRSX[bar]=xrsx*100;
      xxrsx=XSIGN.XMASeries(StartBarsD,prev_calculated,rates_total,SSmoothMethod,SPhase,SPeriod,XRSX[bar],bar,false);
      //---- �������� ����������� �������� � ������������ �����
      XXRSX[bar]=xxrsx;
     }
//---- �������� ���� ��������� ����������
   for(bar=first2; bar<rates_total && !IsStopped(); bar++)
     {
      ColorXRSX[bar]=0;
      //----
      if(XRSX[bar]>0)
        {
         if(XRSX[bar]>XRSX[bar-1]) ColorXRSX[bar]=1;
         if(XRSX[bar]<XRSX[bar-1]) ColorXRSX[bar]=2;
        }
      //----
      if(XRSX[bar]<0)
        {
         if(XRSX[bar]<XRSX[bar-1]) ColorXRSX[bar]=3;
         if(XRSX[bar]>XRSX[bar-1]) ColorXRSX[bar]=4;
        }
     }
//---- �������� ���� ��������� ���������� �����
   for(bar=first3; bar<rates_total && !IsStopped(); bar++)
     {
      ColorXXRSX[bar]=0;
      if(XRSX[bar]>XXRSX[bar-1]) ColorXXRSX[bar]=1;
      if(XRSX[bar]<XXRSX[bar-1]) ColorXXRSX[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
