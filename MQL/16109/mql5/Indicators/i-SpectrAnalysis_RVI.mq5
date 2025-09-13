//+------------------------------------------------------------------+ 
//|                                         i-SpectrAnalysis_RVI.mq5 | 
//|                                           Copyright � 2006, klot | 
//|                                                     klot@mail.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2006, klot" 
#property link      "klot@mail.ru" 
//--- ����� ������ ���������� 
#property version   "1.00" 
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+ 
//| ��������� ��������� ����������               | 
//+----------------------------------------------+ 
//---- ��������� ���������� 1 � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����jd ���������� ������������
#property indicator_color1  clrDodgerBlue,clrDarkViolet
//--- ����������� ����� ���������� 
#property indicator_label1  "i-SpectrAnalysis_RVI" 
//+----------------------------------------------+ 
//| ��������� ����������� �������������� ������� | 
//+----------------------------------------------+ 
#property indicator_level1 0.0 
#property indicator_levelcolor clrGray 
#property indicator_levelstyle STYLE_DASHDOTDOT 
//+----------------------------------------------+ 
//| ���������� ��������                          | 
//+----------------------------------------------+ 
#define RESET 0     // ��������� ��� �������� ��������� ������� �� �������� ���������� 
//+----------------------------------------------+ 
//| �������� ���������� dt_FFT.mqh               | 
//+----------------------------------------------+ 
#include <dt_FFT.mqh> 
//+----------------------------------------------+ 
//| ������� ��������� ����������                 | 
//+----------------------------------------------+ 
input uint RVIPeriod=14;                          // averaging period 
input uint N = 7;                                 // number Length 
input uint SS = 20;                               // smoothing factor 
input int Shift=0;                                // The shift indicator in the horizontal bars
//+----------------------------------------------+ 
//---- ���������� ������������ ��������, ������� � ���������� ����� ������������ � �������� ������������ �������
double RVIBuffer[],SignBuffer[];
//--- 
int M,tnn1,ss;
//--- 
double aa[];
//--- ���������� ������������� ���������� ������ ������� ������ 
int min_rates_total;
//--- ���������� ������������� ���������� ��� ������� ����������� 
int Ind_Handle;
//+------------------------------------------------------------------+   
//| i-SpectrAnalysis_RVI indicator initialization function           | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������ 
   tnn1=int(MathPow(2,N));
   M=ArrayResize(aa,tnn1+1);
   ArraySetAsSeries(aa,true);
   ss=int(MathMin(SS,M));
   min_rates_total=int(M);
//--- ��������� ������ ���������� iRVI 
   Ind_Handle=iRVI(Symbol(),PERIOD_CURRENT,RVIPeriod);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iRVI");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� � ������������ ����� 
   SetIndexBuffer(0,RVIBuffer,INDICATOR_DATA);
//--- ����������� ������������� ������� � ������������ ����� 
   SetIndexBuffer(1,SignBuffer,INDICATOR_DATA);
//--- ������������� ������ ���������� 1 �� ����������� 
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ���������� 
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� ������� 
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//--- ���������� ��������� � ������ ��� � ��������� 
   ArraySetAsSeries(RVIBuffer,true);
   ArraySetAsSeries(SignBuffer,true);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ��������� 
   IndicatorSetString(INDICATOR_SHORTNAME,"i-SpectrAnalysis_RVI");
//--- ����������� �������� ����������� �������� ���������� 
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ���������� ������������� 
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| i-SpectrAnalysis_RVI iteration function                          | 
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
   int end=M-1;
//---- ������������� ������ ������ ������� ��������� �����������
   int drawbegin=rates_total-end;
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,drawbegin);
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,drawbegin);
//--- ������ ������ RVI 
   if(CopyBuffer(Ind_Handle,0,0,M,aa)<=0) return(RESET);
   fastcosinetransform(aa,tnn1,false);
   for(int kkk=0; kkk<=end && !IsStopped(); kkk++) if(kkk>=ss) aa[kkk]=0.0;
   fastcosinetransform(aa,tnn1,true);
   for(int rrr=0; rrr<=end && !IsStopped(); rrr++) RVIBuffer[rrr]=aa[rrr];
//--- ������ ������ ���������� ����� 
   if(CopyBuffer(Ind_Handle,1,0,M,aa)<=0) return(RESET);
   fastcosinetransform(aa,tnn1,false);
   for(int kkk=0; kkk<=end && !IsStopped(); kkk++) if(kkk>=ss) aa[kkk]=0.0;
   fastcosinetransform(aa,tnn1,true);
   for(int rrr=0; rrr<=end && !IsStopped(); rrr++) SignBuffer[rrr]=aa[rrr];
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+ 
