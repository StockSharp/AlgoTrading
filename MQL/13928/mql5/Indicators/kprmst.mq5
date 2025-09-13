//+------------------------------------------------------------------+
//|                                                       KPrmSt.mq5 |
//|                                         Copyright � 2010, LeMan. |
//|                                                 b-market@mail.ru |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2010, LeMan."
#property link      "b-market@mail.ru"
#property description "��������� ������ ����"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� KPrmSt        |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� �������� ����� ���������� ����������� ���� MediumVioletRed
#property indicator_color1  clrMediumVioletRed
//---- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//---- ����������� ����� ����� ����������
#property indicator_label1  "KPrmSt"
//+----------------------------------------------+
//| ��������� ��������� ���������� �����         |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ���������� ����� ���������� ����������� ���� DodgerBlue
#property indicator_color2  clrDodgerBlue
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ����� ����� ����������
#property indicator_label2  "Signal"
//+----------------------------------------------+
//| ��������� �������� ���� ����������           |
//+----------------------------------------------+
#property indicator_minimum 0
#property indicator_maximum 100
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 80.0
#property indicator_level2 50.0
#property indicator_level3 20.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0       // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint  Per1=14;
input uint  Per2=3;
input uint  Per3=3;
input uint  Per4=5;
input uint  Shift=0; // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double IndBuffer[];
double SignalBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ���������� ����������
int Count[];
double MaxArray[],MinArray[];
//+------------------------------------------------------------------+
//| �������� ������ Moving_Average                                   |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+
//| �������� ������� ������ ������ �������� � �������                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                          int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;
//----
   Max2=Size;
   Max1=Max2-1;
//----
   count--;
   if(count<0) count=Max1;
//----
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(Per1,Per4))+2;
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,Per1);
   ArrayResize(MaxArray,Per1);
   ArrayResize(MinArray,Per1);
//----
   ArrayInitialize(Count,0);
   ArrayInitialize(MaxArray,0.0);
   ArrayInitialize(MinArray,0.0);
//---- ����������� ������������� ������� IndBuffer[] � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);
//---- ����������� ������������� ������� SignalBuffer[] � ������������ �����
   SetIndexBuffer(1,SignalBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SignalBuffer,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"KPrmSt(",Per1,", ",Per2,", ",Per3,", ",Per4,", ",Shift,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//----
   return(0);
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(RESET);
//---- ���������� ��������� ���������� 
   int limit,bar,maxbar,sh,sl,start1,start2;
   double res,Range,ind,sig;
//----
   maxbar=int(rates_total-Per1-1);
   start1=int(maxbar-Per4);
   start2=start1-1;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=int(rates_total-Per4-1); // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
//---- ���������� ���������� ������ CMoving_Average �� ����� MASeries_Cls.mqh
   static CMoving_Average IND,SIG;
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      MaxArray[Count[0]]=high[ArrayMaximum(high,bar,Per4)];
      MinArray[Count[0]]=low [ArrayMinimum(low, bar,Per4)];
      //----
      if(bar>maxbar){Recount_ArrayZeroPos(Count,Per1); continue;}
      //----
      sh = ArrayMaximum(MaxArray,0,WHOLE_ARRAY);
      sl = ArrayMinimum(MinArray,0,WHOLE_ARRAY);
      //----
      Range=MaxArray[sh]-MinArray[sl];
      //----
      if(Range) res=NormalizeDouble((close[bar]-MinArray[sl])/(Range)*100,0);
      else res=50;
      //----
      ind=IND.EMASeries(start1,prev_calculated,rates_total,Per2,res,bar,true);
      sig=SIG.EMASeries(start2,prev_calculated,rates_total,Per3,ind,bar,true);
      //----
      IndBuffer[bar]=NormalizeDouble(ind,0);
      SignalBuffer[bar]=NormalizeDouble(sig,0);
      //----
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,Per1);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
