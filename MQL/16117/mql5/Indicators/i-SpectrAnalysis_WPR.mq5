//+------------------------------------------------------------------+
//|                                         i-SpectrAnalysis_WPR.mq5 |
//|                                           Copyright � 2006, klot |
//|                                                     klot@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, klot"
#property link      "klot@mail.ru"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ���������� ������������ �������
#property indicator_buffers 1 
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//--- � �������� ����� ����� ���������� ����������� DarkOrchid ����
#property indicator_color1 clrDarkOrchid
//--- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width1  2
//--- ����������� ����� ����������
#property indicator_label1  "i-SpectrAnalysis_WPR"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 -20.0
#property indicator_level2 -50.0
#property indicator_level3 -80.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0                // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| �������� ���������� dt_FFT.mqh               |
//+----------------------------------------------+
#include <dt_FFT.mqh> 
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint WPRPeriod=14;
input uint N = 7;   // ����� ����
input uint SS = 20; // ����������� �����������
input int Shift=0;  // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//--- ���������� ������������� �������, ������� � ����������
//--- ����� ����������� � �������� ������������� ������
double IndBuffer[];
//---
int M,tnn1,ss;
//---
double aa[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//--- ���������� ������������� ���������� ��� ������� �����������
int Ind_Handle;
//+------------------------------------------------------------------+   
//| i-SpecktrAnalis_WPR indicator initialization function            | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   tnn1=int(MathPow(2,N));
   M=ArrayResize(aa,tnn1+1);
   ArraySetAsSeries(aa,true);
   ss=int(MathMin(SS,M));
   min_rates_total=int(M+WPRPeriod);
//--- ��������� ������ ���������� iWPR
   Ind_Handle=iWPR(Symbol(),PERIOD_CURRENT,WPRPeriod);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iWPR");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//--- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"i-SpecktrAnalis_WPR");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| i-SpecktrAnalis_WPR iteration function                           | 
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
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total || BarsCalculated(Ind_Handle)<rates_total) return(RESET);
//---
   for(int bar=rates_total-1; bar>=prev_calculated && !IsStopped(); bar--) IndBuffer[bar]=0.0;
//--- �������� ����� ����������� ������ � ������
   if(CopyBuffer(Ind_Handle,0,0,M,aa)<=0) return(RESET);
//---
   int end=M-1;
   fastcosinetransform(aa,tnn1,false);
   for(int kkk=0; kkk<=end && !IsStopped(); kkk++) if(kkk>=ss) aa[kkk]=0.0;
   fastcosinetransform(aa,tnn1,true);
   for(int rrr=0; rrr<=end && !IsStopped(); rrr++) IndBuffer[rrr]=aa[rrr];
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
