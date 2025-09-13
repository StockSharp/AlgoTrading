//+------------------------------------------------------------------+
//|                                                   LeManTrend.mq5 |
//|                                         Copyright � 2009, LeMan. | 
//|                                                 b-market@mail.ru | 
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2009, LeMan."
//---- ������ �� ���� ������
#property link "b-market@mail.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ������� ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ������ ����� ���������� ����������� ������� ����
#property indicator_color1  Lime
//---- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//---- ����������� ������ ����� ����������
#property indicator_label1  "LeManTrend Bulls"
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color2  Red
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//---- ����������� ��������� ����� ����������
#property indicator_label2  "LeManTrend Bears"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int Min       = 13;
input int Midle     = 21;
input int Max       = 34;
input int PeriodEMA = 3; // ������ ����������
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double BullsBuffer[];
double BearsBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,start;
//+------------------------------------------------------------------+
//| �������� ������ CMoving_Average                                  |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   start=MathMax(MathMax(Min,Midle),Max);
   min_rates_total=start+PeriodEMA;
//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(0,BullsBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BullsBuffer,true);
//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(1,BearsBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BearsBuffer,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"LeManTrend(",Min,", ",Midle,", ",Max,", ",PeriodEMA,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   if(rates_total<min_rates_total) return(0);
//---- ���������� ��������� ���������� 
   int limit,bar,maxbar;
   double High1,High2,High3,Low1,Low2,Low3,HH,LL;
//---- ������ ���������� ������ maxbar ��� ������� MASeries()
   maxbar=rates_total-1-start;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=maxbar; // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- ���������� ���������� ������ CMoving_Average �� ����� SmoothAlgorithms.mqh
   static CMoving_Average BULLS,BEARS;
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      High1=high[ArrayMaximum(high,bar+1,Min)];
      High2=high[ArrayMaximum(high,bar+1,Midle)];
      High3=high[ArrayMaximum(high,bar+1,Max)];
      HH=((high[bar]-High1)+(high[bar]-High2)+(high[bar]-High3));
      //----
      Low1=low[ArrayMinimum(low,bar+1,Min)];
      Low2=low[ArrayMinimum(low,bar+1,Midle)];
      Low3=low[ArrayMinimum(low,bar+1,Max)];
      LL=((Low1-low[bar])+(Low2-low[bar])+(Low3-low[bar]));
      //----
      BullsBuffer[bar]=BULLS.MASeries(maxbar,prev_calculated,rates_total,PeriodEMA,MODE_EMA,HH,bar,true);
      BearsBuffer[bar]=BEARS.MASeries(maxbar,prev_calculated,rates_total,PeriodEMA,MODE_EMA,LL,bar,true);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
