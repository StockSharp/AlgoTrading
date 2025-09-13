//+---------------------------------------------------------------------+
//|                                                     ColorXdinMA.mq5 | 
//|                                          Copyright � 2011,   dimeon | 
//|                                                                     | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2011, dimeon"
#property link ""
//---- ����� ������ ����������
#property version   "1.03"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������������ �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ����������� ����� ������������
#property indicator_color1  clrYellow,clrBlue,clrRed
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "XdinMA"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
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
//+-----------------------------------+
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
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_SMA; // ����� ����������
input int Length_main=10; // ������� main ����������
input int Length_plus=20; // ������� plus ����������                  
input int PhaseX=15;      // �������� ����������
                          // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                          // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE; // ������� ���������
input int Shift=0;      // ����� ���������� �� ����������� � �����
input int PriceShift=0; // ����� ���������� �� ��������� � �������
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double XdinMA[];
double ColorXdinMA[];
//---- ���������� ���������� �������� ������������� ������ ���������� �������
double dPriceShift;
//---- ���������� ������������� ���������� ������ ������� ������
int  min_rates_total;
//+------------------------------------------------------------------+   
//| XdinMA indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   int StartBars1=XMA1.GetStartBars(MA_Method1, Length_main, PhaseX);
   int StartBars2=XMA2.GetStartBars(MA_Method1, Length_plus, PhaseX);
   min_rates_total=MathMax(StartBars1,StartBars2)+1;
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Length_main", Length_main);
   XMA2.XMALengthCheck("Length_plus", Length_plus);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("PhaseX",PhaseX,MA_Method1);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,XdinMA,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorXdinMA,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(MA_Method1);
   StringConcatenate(shortname,"XdinMA(",Length_main,", ",Length_plus,", ",Smooth,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| XdinMA iteration function                                        | 
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
   double price_,ma_main,ma_plus;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first1,first2,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first1=0; // ��������� ����� ��� ������� ���� �����
      first2=min_rates_total;
     }
   else
     {
      first1=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
      first2=first1;
     }
//---- �������� ���� ������� ����������
   for(bar=first1; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price_
      price_=PriceSeries(IPC,bar,open,low,high,close);
      //---- ��� ������ ������� XMASeries
      ma_main = XMA1.XMASeries(0, prev_calculated, rates_total, MA_Method1, PhaseX, Length_main, price_, bar, false);
      ma_plus = XMA2.XMASeries(0, prev_calculated, rates_total, MA_Method1, PhaseX, Length_plus, price_, bar, false);
      //----       
      XdinMA[bar]=ma_main*2-ma_plus+dPriceShift;
     }
//---- �������� ���� ��������� ����������
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorXdinMA[bar]=0;
      if(XdinMA[bar-1]<XdinMA[bar]) ColorXdinMA[bar]=1;
      if(XdinMA[bar-1]>XdinMA[bar]) ColorXdinMA[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
