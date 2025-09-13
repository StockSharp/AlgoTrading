//+---------------------------------------------------------------------+
//|                                                            XCCX.mq5 | 
//|                                Copyright � 2013,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Commodity Chanel Index"
//---- ����� ������ ����������
#property version   "1.10"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� �����-��������� ����
#property indicator_color1 clrDarkOrange
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "XCCX"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1       -50.0
#property indicator_level2        50.0
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMAD,XMAH,XMAL;
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum Applied_price //��� ���������
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
input int DPhase=15;   // �������� ����������
                       // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                       // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MSmoothMethod=MODE_T3; // ����� ���������� ����������
input int MPeriod=15; // ������ �������� ����������
input int MPhase=15;  // �������� ����������
                      // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                      // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price IPC=PRICE_TYPICAL; // ������� ���������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������� �������, ������� ����� � 
//---- ���������� ����������� � �������� ������������� ������
double XCCX[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_D,min_rates_M;
//+------------------------------------------------------------------+   
//| XCCX indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_D=XMAD.GetStartBars(DSmoothMethod,DPeriod,DPhase);
   min_rates_M=XMAD.GetStartBars(MSmoothMethod,MPeriod,MPhase);
   min_rates_total=min_rates_D+min_rates_M;
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMAD.XMALengthCheck("DPeriod", DPeriod);
   XMAD.XMALengthCheck("MPeriod", MPeriod);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMAD.XMAPhaseCheck("DPhase",DPhase,DSmoothMethod);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,XCCX,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"XCCX");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname,SmoothD,SmoothM;
   SmoothD=XMAD.GetString_MA_Method(DSmoothMethod);
   SmoothM=XMAD.GetString_MA_Method(MSmoothMethod);
   StringConcatenate(shortname,"Commodity Chanel Index(",string(DPeriod),",",string(MPeriod),",",SmoothD,",",SmoothM,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| XCCX iteration function                                          | 
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
   double price,xma,upccx,dnccx,xupccx,xdnccx;
//---- ���������� ������������� ����������
   int first,bar;
//---- ������ ���������� ������ first ��� ����� ��������� ����� � ������������� ����������
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price
      price=PriceSeries(IPC,bar,open,low,high,close);
      //---- ���� ����� ������� XMASeries 
      xma=XMAD.XMASeries(0,prev_calculated,rates_total,DSmoothMethod,DPhase,DPeriod,price,bar,false);
      upccx=price-xma;
      dnccx=MathAbs(upccx);
      //---- ��� ������ ������� XMASeries  
      xupccx=XMAH.XMASeries(min_rates_D,prev_calculated,rates_total,MSmoothMethod,MPhase,MPeriod,upccx,bar,false);
      xdnccx=XMAL.XMASeries(min_rates_D,prev_calculated,rates_total,MSmoothMethod,MPhase,MPeriod,dnccx,bar,false);
      //---- ������������� ������������� ������
      if(xupccx) // ������ ������� �� ����!
         XCCX[bar]=100*xupccx/xdnccx;
      else XCCX[bar]=EMPTY_VALUE;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
