//+---------------------------------------------------------------------+
//|                                                        TSI_MACD.mq4 |
//|                         Copyright � 2006, MetaQuotes Software Corp. |
//|                                           http://www.metaquotes.net |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2006, MetaQuotes Software Corp."
//--- ������ �� ���� ������
#property link "http://www.metaquotes.net" 
#property description "TSI_MACD"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1  clrDarkTurquoise,clrViolet
//--- ����������� ����� ����������
#property indicator_label1  "TSI_MACD"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 +50
#property indicator_level2   0
#property indicator_level3 -50
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- ���������� ���������� ������� CXMA � CMomentum �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5,XMA6,XMA7;
CMomentum Mom;
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
enum Applied_price_      //��� ���������
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
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_EMA; // ����� ����������
input uint XFast=8;                      // ������� ���������� ����
input uint XSlow=21;                     // ��������� ���������� ���� 
input uint MomPeriod=1;                  // ������ ���������
input uint XLength1=5;                   // ������� ������� ����������
input uint XLength2=8;                   // ������� ������� ����������
input uint XLength3=5;                   // ������� ���������� ���������� �����
input int XPhase=15;                     // �������� �����������
//--- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE;    // ������� ���������
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double UpBuffer[],DnBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2,min_rates_3,min_rates_4;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_1=int(MathMax(XMA1.GetStartBars(XMA_Method,XFast,XPhase),XMA1.GetStartBars(XMA_Method,XSlow,XPhase)));
   min_rates_2=int(MomPeriod);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_4=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total=min_rates_4+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"TSI_MACD");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
   double price,fast,slow,macd,mtm,xmtm,xxmtm,absmtm,xabsmtm,xxabsmtm,tsi,xtsi;
   int first,bar;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      fast=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XFast,price,bar,false);
      slow=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XSlow,price,bar,false);
      macd=fast-slow;
      mtm=Mom.MomentumSeries(min_rates_1,prev_calculated,rates_total,MomPeriod,macd,bar,false);
      absmtm=MathAbs(mtm);
      xmtm=XMA3.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,mtm,bar,false);
      xabsmtm=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,absmtm,bar,false);
      xxmtm=XMA5.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xmtm,bar,false);
      xxabsmtm=XMA6.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xabsmtm,bar,false);
      if(xxabsmtm) tsi=100*xxmtm/xxabsmtm;
      else tsi=0;
      if(!tsi) tsi=0.000000001;
      xtsi=XMA7.XMASeries(min_rates_4,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,tsi,bar,false);
      if(!xtsi) xtsi=0.000000001;
      UpBuffer[bar]=tsi;
      DnBuffer[bar]=xtsi;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
