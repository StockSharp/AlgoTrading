//+---------------------------------------------------------------------+
//|                                                    i-BandsPrice.mq5 | 
//|                                             Copyright � 2008, Talex |
//|                                                    tan@gazinter.net |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2008, Talex"
#property link      "tan@gazinter.net"
#property description "i-BandsPrice"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������������ �����������
#property indicator_type1   DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ����������� ������������
#property indicator_color1  clrMagenta,clrMaroon,clrGray,clrTeal,clrLime
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "i-BandsPrice"
//+-----------------------------------+
//| �������� ������� ����������       |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������� CXMA � CStdDeviation �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
CStdDeviation STD;
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
input Smooth_Method XMA_Method=MODE_SMA; // ����� ������� ����������
input uint XLength=100; // �������  ������� �����������
input int XPhase=15; // �������� ������� ����������
                     // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                     // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input uint BandsPeriod=100; // ������ ���������� BB
input double BandsDeviation=2.0; // ��������
input uint Smooth=5; // ������ ����������� ����������
input Applied_price_ IPC=PRICE_CLOSE; // ������� ���������
input int UpLevel=+25; // ������� �������
input int DnLevel=-25; // ������ �������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//--- ���������� ������������ � �������� ������������ ������� ������� �����������
double IndBuffer[],ColorIndBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2;
//+------------------------------------------------------------------+   
//| X2MA BBx3 indicator initialization function                      | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=XMA1.GetStartBars(XMA_Method,XLength,XPhase)+1;
   min_rates_2=min_rates_1+int(BandsPeriod);
   min_rates_total=min_rates_2+30;
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("XLength",XLength);
   XMA2.XMALengthCheck("BandsPeriod", BandsPeriod);
   XMA1.XMALengthCheck("Smooth",Smooth);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"i-BandsPrice");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ����������  �������������� ������� ���������� 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,UpLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,0);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,DnLevel);
//---- � �������� ������ ����� �������������� ������� ������������ ����� � ������� �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrBlue);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| X2MA BBx3 iteration function                                     | 
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
   double price_,xma,stdev,res,jres,dwband;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar,clr;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price_=PriceSeries(IPC,bar,open,low,high,close);
      xma=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price_,bar,false);
      stdev=STD.StdDevSeries(min_rates_1,prev_calculated,rates_total,BandsPeriod,BandsDeviation,price_,xma,bar,false);
      dwband=xma-stdev;
      if(!stdev) stdev=1.0;
      res=100*(price_-dwband)/(2*stdev)-50;
      jres=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,MODE_JJMA,100,Smooth,res,bar,false);
      IndBuffer[bar]=jres;
      //----
      clr=2;
      //----
      if(jres>UpLevel) clr=4; else if(jres>0) clr=3;
      if(jres<DnLevel) clr=0; else if(jres<0) clr=1;
      //----
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
