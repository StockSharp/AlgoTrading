//+---------------------------------------------------------------------+
//|                                                       ForexLine.mq5 | 
//|                                             Copyright � 2015, 3rjfx | 
//|                                 https://www.mql5.com/en/users/3rjfx | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2015, 3rjfx"
#property link "https://www.mql5.com/en/users/3rjfx"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������������ �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ����������� ����� ������������
#property indicator_color1  clrLimeGreen,clrGray,clrMagenta
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "IndBuffer"

//+-----------------------------------+
//|  �������� ������ CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4;
//+-----------------------------------+
//|  ���������� ������������          |
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
//|  ���������� ������������          |
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
//|  ������� ��������� ����������     |
//+-----------------------------------+
input Smooth_Method MA_Method11=MODE_LWMA; //����� ���������� ������� ����������� ������� 1
input int Length11=5; //�������  ������� �����������  ������� 1                   
input int Phase11=15; //�������� ������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method12=MODE_LWMA; //����� ���������� ������� ����������� 
input int Length12=10; //�������  ������� �����������  ������� 1
input int Phase12=15;  //�������� ������� �����������  ������� 1,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC1=PRICE_CLOSE;//������� ��������� ������� 1
//----
input Smooth_Method MA_Method21=MODE_LWMA; //����� ���������� ������� ����������� ������� 2
input int Length21=20; //�������  ������� ����������� ������� 2                   
input int Phase21=15; //�������� ������� ����������� ������� 2,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method22=MODE_LWMA; //����� ���������� ������� ����������� ������� 2
input int Length22=20; //�������  ������� ����������� ������� 2
input int Phase22=15;  //�������� ������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC2=PRICE_CLOSE;//������� ��������� ������� 2
//----
input int Shift=0; // ����� ���������� �� ����������� � �����
input int PriceShift=0; // ����� ���������� �� ��������� � �������
//+-----------------------------------+

//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double IndBuffer[];
double ColorIndBuffer[];

//---- ���������� ���������� �������� ������������� ������ �������
double dPriceShift;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=GetStartBars(MA_Method11,Length11,Phase11);
   min_rates_2=GetStartBars(MA_Method21,Length21,Phase21);
   
   int min_rates_12=GetStartBars(MA_Method12,Length12,Phase12);
   int min_rates_22=GetStartBars(MA_Method22,Length22,Phase22);
   
   min_rates_total=MathMax(min_rates_1+min_rates_12,min_rates_2+min_rates_22);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Length11",Length11);
   XMA2.XMALengthCheck("Length12",Length12);
   XMA3.XMALengthCheck("Length21",Length21);
   XMA4.XMALengthCheck("Length22",Length22);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("Phase11", Phase11,MA_Method11);
   XMA2.XMAPhaseCheck("Phase12", Phase12,MA_Method12);
   XMA3.XMAPhaseCheck("Phase21", Phase21,MA_Method21);
   XMA4.XMAPhaseCheck("Phase22", Phase22,MA_Method22);

//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);

//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"ForexLine(",Length11,", ",Length12,", ",Length21,", ",Length21,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
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
   double price,x1xma,x2xma,x3xma,x4xma;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar,clr;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC1,bar,open,low,high,close);
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method11,Phase11,Length11,price,bar,false);
      x2xma=XMA2.XMASeries(min_rates_1,prev_calculated,rates_total,MA_Method12,Phase12,Length12,x1xma,bar,false);
      //----
      price=PriceSeries(IPC2,bar,open,low,high,close);
      x3xma=XMA3.XMASeries(0,prev_calculated,rates_total,MA_Method21,Phase21,Length21,price,bar,false);
      x4xma=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,MA_Method22,Phase22,Length22,x3xma,bar,false);
      //----      
      IndBuffer[bar]=x4xma+dPriceShift;
      //---- ������������ ���������
      clr=1;
      if(x2xma>x4xma) clr=0;
      if(x2xma<x4xma) clr=2;
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
