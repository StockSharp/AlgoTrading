//+---------------------------------------------------------------------+
//|                                                   ColorBearsGap.mq5 | 
//|                                  Copyright � 2015, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.02"
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
//---- � �������� ������ ����������� ����������� ������������
#property indicator_color1  clrDodgerBlue,clrGray,clrDeepPink
//---- ����������� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "Bears"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4;
//+-----------------------------------+
//| ���������� ������������           |
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
input Smooth_Method MA_Method1=MODE_SMA; // ����� ���������� ����������
input uint Length1=12; // ������� ���������� ����������
input int Phase1=15;   // �������� ���������� ����������
//--- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method2=MODE_JJMA; // ����� ����������� ��������� ����������
input uint Length2=5; // ������� ����������� ��������� ����������
input int Phase2=15;  // �������� ����������� ��������� ����������
//--- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double IndBuffer[];
double ColorIndBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1;
//+------------------------------------------------------------------+   
//| Bears indicator initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=GetStartBars(MA_Method1,Length1,Phase1);
   int min_rates_2=GetStartBars(MA_Method2,Length2,Phase2);
   min_rates_total=min_rates_1+min_rates_2+2;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(MA_Method1);
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   StringConcatenate(shortname,"ColorBearsGap(",Length1,", ",Smooth1,", ",Length2,", ",Smooth2,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| Bears iteration function                                         | 
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
   double xmaC,bullsC,xbullsC,xmaO,bullsO,xbullsO;
   static double xbullsC1,xbullsO1;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar,clr;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      xmaC=XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,close[bar],bar,false);
      bullsC=high[bar]-xmaC;
      xbullsC=XMA2.XMASeries(min_rates_1,prev_calculated,rates_total,MA_Method2,Phase2,Length2,bullsC,bar,false);
      xbullsC/=_Point;
      //----
      xmaO=XMA3.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,open[bar],bar,false);
      bullsO=high[bar]-xmaO;
      xbullsO=XMA4.XMASeries(min_rates_1,prev_calculated,rates_total,MA_Method2,Phase2,Length2,bullsO,bar,false);
      xbullsO/=_Point;
      //----
      IndBuffer[bar]=xbullsO-xbullsC1;
      //----
      if(bar<rates_total-1)
        {
         xbullsC1=xbullsC;
         xbullsO1=xbullsO;
        }
     }
//---- ������������� �������� ���������� first
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total; // ��������� ����� ��� ������� ���� �����
//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      clr=1;
      if(IndBuffer[bar]>0) clr=0;
      if(IndBuffer[bar]<0) clr=2;
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
