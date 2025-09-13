//+---------------------------------------------------------------------+
//|                                                              Go.mq5 |
//|                                Copyright � 2006, Victor Chebotariov |
//|                                         http://www.chebotariov.com/ |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2006, Victor Chebotariov"
#property link      "http://www.chebotariov.com/"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ����������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������� ����������� ������������ ��� �����
#property indicator_color1 clrGray,clrLime,clrRed
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ���������� �����
#property indicator_label1  "Go"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint Period_=174; // ������ ���������� 
input int Shift=0;     // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double ExtBuffer[],ColorExtBuffer[];
//+------------------------------------------------------------------+
// �������� ������� ����������                                       |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ����������� ������������� ������� ExtBuffer � ������������ �����
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Go(",Period_,")");
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- �������� ����� ��� ����������� � ���� ������
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,Period_);
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,Period_);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//----
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
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<int(Period_)+1) return(0);

//---- ���������� ��������� ���������� 
   int first1,first2,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first1=0; // ��������� ����� ��� ������� ���� �����
      first2=int(Period_)+1;
     }
   else
     {
      first1=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
      first2=first1;
     }
     
//---- ���������� ���������� ������� Moving_Average � StdDeviation
   static CMoving_Average MA;

//---- �������� ���� ������� ����������
   for(bar=first1; bar<rates_total; bar++)
      ExtBuffer[bar]=MA.MASeries(0,prev_calculated,rates_total,Period_,MODE_SMA,close[bar]-open[bar],bar,false)/_Point;

//---- �������� ���� ��������� ����������
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorExtBuffer[bar]=0;
      if(ExtBuffer[bar]>0) ColorExtBuffer[bar]=1;
      if(ExtBuffer[bar]<0) ColorExtBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
