//+---------------------------------------------------------------------+
//|                                                        ColorHMA.mq5 |
//|                                  Copyright � 2010, Nikolay Kositsin |
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "2010,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//---- ��������� ���������� � �������� ����
#property indicator_chart_window
//---- ��� ������� � ��������� ���������� ����������� ���� �����
#property indicator_buffers 2
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ����������� ����� ������������
#property indicator_color1  clrGray,clrMediumPurple,clrRed
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  2
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input int HMA_Period=13; // ������ ���������� �������
input int HMA_Shift=0;   // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtLineBuffer[];
double ColorExtLineBuffer[];
//---- ���������� ������������� ����������
int Hma2_Period,Sqrt_Period;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
   Hma2_Period=int(MathFloor(HMA_Period/2));
   Sqrt_Period=int(MathFloor(MathSqrt(HMA_Period)));
//---- ������������� ���������� ������ ������� ������
   min_rates_total=HMA_Period+Sqrt_Period;
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� ������� �� ����������� �� HMAShift
   PlotIndexSetInteger(0,PLOT_SHIFT,HMA_Shift);
//---- ������������� ������ ������ ������� ��������� ���������� HMAPeriod
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);
//--- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="HMA";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name+"("+string(HMA_Period)+")");
  }
//+------------------------------------------------------------------+
//| �������� ������ CMoving_Average                                  |
//+------------------------------------------------------------------+  
#include <SmoothAlgorithms.mqh>
//+------------------------------------------------------------------+ 
//| Moving Average                                                   |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const int begin,          // ����� ������ ������������ ������� �����
                const double &price[])    // ������� ������ ��� ������� ����������
  {
   int begin0=min_rates_total+begin;
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<begin0) return(0);
//---- ���������� ��������� ���������� 
   int first,bar,begin1;
   double lwma1,lwma2,dma;
//----
   begin1=HMA_Period+begin;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated==0) // �������� �� ������ ����� ������� ����������
     {
      first=begin; // ��������� ����� ��� ������� ���� �����      
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,begin0+1);
      for(bar=0; bar<=begin0; bar++) ColorExtLineBuffer[bar]=0;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- ���������� ���������� ������ CMoving_Average �� ����� HMASeries_Cls.mqh
   static CMoving_Average MA1,MA2,MA3;
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      lwma1=MA1.LWMASeries(begin,prev_calculated,rates_total,Hma2_Period,price[bar],bar,false);
      lwma2=MA2.LWMASeries(begin,prev_calculated,rates_total,HMA_Period, price[bar],bar,false);
      dma=2*lwma1-lwma2;
      ExtLineBuffer[bar]=MA3.LWMASeries(begin1,prev_calculated,rates_total,Sqrt_Period,dma,bar,false);
     }
//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=begin0;
//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total; bar++)
     {
      ColorExtLineBuffer[bar]=0;
      if(ExtLineBuffer[bar-1]<ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=1;
      if(ExtLineBuffer[bar-1]>ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
