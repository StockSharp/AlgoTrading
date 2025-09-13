//+------------------------------------------------------------------+
//|                                               LeManTrendHist.mq5 |
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
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0                        // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ �������������� ����������� ������������
#property indicator_color1 clrMagenta,clrPurple,clrGray,clrOliveDrab,clrLime
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "LeManTrend"
//+----------------------------------------------+
//| �������� ������ CMoving_Average              |
//+----------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
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
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int Min       = 13;
input int Midle     = 21;
input int Max       = 34;
input int PeriodEMA = 3;               // ������ ����������
input Smooth_Method XMethod=MODE_JJMA; // ����� �����������  ����������
input int XLength=5;                   // ������� ����������� ����������
input int XPhase=100;                  // �������� ����������� ����������
input int Shift=0;                     // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=MathMax(MathMax(Min,Midle),Max)+1;
   min_rates_2=GetStartBars(XMethod,XLength,XPhase);
   min_rates_total=min_rates_1+min_rates_2+1;
//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorIndBuffer,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"LeManTrend(",Min,", ",Midle,", ",Max,", ",PeriodEMA,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   int limit,bar,maxbar1,maxbar2,clr;
   double High1,High2,High3,Low1,Low2,Low3,HH,LL,Bulls,Bears,Range,XRange;
//---- ������ ���������� ������
   maxbar1=rates_total-min_rates_1-1;
   maxbar2=maxbar1-min_rates_2;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=maxbar1; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- ���������� ���������� ������ CMoving_Average �� ����� SmoothAlgorithms.mqh
   static CMoving_Average BULLS,BEARS;
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
   static CXMA SMOOTH;
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
      Bulls=BULLS.MASeries(maxbar1,prev_calculated,rates_total,PeriodEMA,MODE_EMA,HH,bar,true);
      Bears=BEARS.MASeries(maxbar1,prev_calculated,rates_total,PeriodEMA,MODE_EMA,LL,bar,true);
      Range=Bulls-Bears;
      XRange=SMOOTH.XMASeries(maxbar1,prev_calculated,rates_total,XMethod,XPhase,XLength,Range,bar,true);
      IndBuffer[bar]=XRange;
      clr=2;
      if(XRange>0)
        {
         if(XRange>IndBuffer[bar+1]) clr=4;
         if(XRange<IndBuffer[bar+1]) clr=3;
        }
      //----
      if(XRange<0)
        {
         if(XRange<IndBuffer[bar+1]) clr=0;
         if(XRange>IndBuffer[bar+1]) clr=1;
        }
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
