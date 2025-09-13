//+---------------------------------------------------------------------+ 
//|                                                  XKRI_Histogram.mq5 | 
//|                                  Copyright � 2015, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� ����������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ����������� ����������� ������������
#property indicator_color1 clrGray,clrOliveDrab,clrDodgerBlue,clrDeepPink,clrMagenta
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "XKRI"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
//---- ���������� ���������� ������ Moving_Average �� ����� SmoothAlgorithms.mqh
CMoving_Average MA;
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
enum ENUM_PRICE_TYPE //��� ���������
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
input uint KRIPeriod=20; // ������ ����������
input ENUM_MA_METHOD MA_Method_=MODE_SMA; // ����� ����������
input double Ratio=1.0;
input ENUM_PRICE_TYPE IPC=PRICE_CLOSE_; // ������� ���������
input Smooth_Method XMA_Method=MODE_JJMA; // ����� ����������
input uint XLength=7; // ������� �����������
input int XPhase=15;  // �������� �����������
//--- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input int Shift=0; // ����� ���������� �� ����������� � �����
input double HighLevel=+1; // ������� ���������������
input double LowLevel=-1;  // ������� ���������������
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_1,min_rates_total;
//+------------------------------------------------------------------+    
//| XKRI indicator initialization function                           | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ��������
   min_rates_1=int(KRIPeriod+1);
   min_rates_total=min_rates_1+GetStartBars(XMA_Method,XLength,XPhase);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"XKRI( KRIPeriod = ",KRIPeriod,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ����������  �������������� ������� ���������� 2   
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,LowLevel);
//---- � �������� ������ ����� �������������� ������� ������������ �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrPurple);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| XKRI iteration function                                          | 
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
   double price,KRI,mov;
//---- ���������� ������������� ����������
   int first,bar,clr;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated==0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
     }
   else // ��������� ����� ��� ������� ����� �����
     {
      first=prev_calculated-1;
     }
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� Series
      price=PriceSeries(IPC,bar,open,low,high,close);
      mov=MA.MASeries(0,prev_calculated,rates_total,KRIPeriod,MA_Method_,price,bar,false);
      KRI=100*(price-mov)/mov;
      IndBuffer[bar]=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,KRI,bar,false);
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;
//---- �������� ���� ��������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      clr=0;
      //----
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=2;
        }
      //----
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
        }
      ColorIndBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
