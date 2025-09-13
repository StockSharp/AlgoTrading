//+---------------------------------------------------------------------+
//|                                                             CHO.mq5 |
//|                         Copyright � 2007, MetaQuotes Software Corp. |
//|                                           http://www.metaquotes.net |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� xrangeAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 1
#property indicator_buffers 1 
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� DarkTurquoise ����
#property indicator_color1 clrDarkTurquoise
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "CHO"
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
//| ���������� ��������               |
//+-----------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input Smooth_Method XMA_Method=MODE_SMA;     // ����� ����������
input uint FastPeriod=3;                     // ������ �������� ����������
input uint SlowPeriod=10;                    // ����� ���������� ����������
input int XPhase=15;                         // �������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtBuffer[];
//---- ���������� ������������� ���������� ��� �������� ������� �����������
int Ind_Handle;
//---- ���������� ������������� ���������� ������ ������� ������
int  min_rates_,min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_=2;
   int min_rates_1=XMA1.GetStartBars(XMA_Method,FastPeriod,XPhase);
   int min_rates_2=XMA1.GetStartBars(XMA_Method,SlowPeriod,XPhase);
   min_rates_total=min_rates_+int(MathMax(min_rates_1,min_rates_2));
//--- ��������� ������ ���������� iAD
   Ind_Handle=iAD(Symbol(),PERIOD_CURRENT,VolumeType);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iAD");
      return(INIT_FAILED);
     }
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtBuffer,true);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"CHO");
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
   if(rates_total<min_rates_total || BarsCalculated(Ind_Handle)<rates_total) return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar,maxbar;
//---- ���������� ���������� � ��������� ������  
   double AD[],Fast,Slow;
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(AD,true);
//----   
   maxbar=rates_total-min_rates_-1;
//---- ������� ������������ ���������� ���������� ������ �
//---- ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=maxbar; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//----   
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(Ind_Handle,0,0,to_copy,AD)<=0) return(RESET);
//---- ������ ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Fast=XMA1.XMASeries(maxbar,prev_calculated,rates_total,XMA_Method,XPhase,FastPeriod,AD[bar],bar,true);
      Slow=XMA2.XMASeries(maxbar,prev_calculated,rates_total,XMA_Method,XPhase,SlowPeriod,AD[bar],bar,true);
      ExtBuffer[bar]=Fast-Slow;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
