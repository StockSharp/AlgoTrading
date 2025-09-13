//+---------------------------------------------------------------------+
//|                                                  ColorHMA_StDev.mq5 |
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
//---- ��� ������� � ��������� ���������� ������������ ����� �������
#property indicator_buffers 6
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   5
//+----------------------------------------------+
//|  ��������� ��������� �����  ����������       |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ���������� ����� ������������
#property indicator_color1  clrGray,clrMediumPurple,clrRed
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  2
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color2  clrMagenta
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ��������� ����� ����������
#property indicator_label2  "Dn_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ����� ����
#property indicator_color3  clrBlue
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ����� ����� ����������
#property indicator_label3  "Up_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� �������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color4  clrMagenta
//---- ������� ����� ���������� 4 ����� 4
#property indicator_width4  4
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Dn_Signal 2"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 5 � ���� �������
#property indicator_type5   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ����� ����
#property indicator_color5  clrBlue
//---- ������� ����� ���������� 5 ����� 4
#property indicator_width5  4
//---- ����������� ����� ����� ����������
#property indicator_label5  "Up_Signal 2"
//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input uint HMA_Period=13; // ������ �������
input double dK1=1.5;  //����������� 1 ��� ������������� �������
input double dK2=2.5;  //����������� 2 ��� ������������� �������
input uint std_period=9; //������ ������������� �������
input int PriceShift=0; // c���� ����� �� ��������� � �������
input int Shift=0; // ����� ������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtLineBuffer[],ColorExtLineBuffer[];
double BearsBuffer1[],BullsBuffer1[];
double BearsBuffer2[],BullsBuffer2[];
//---- ���������� ����� ����������
int Hma2_Period,Sqrt_Period;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates;
double dPriceShift,dHMA[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//----  
  Hma2_Period=int(MathFloor(HMA_Period/2));
  Sqrt_Period=int(MathFloor(MathSqrt(HMA_Period)));
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
  
//---- ������������� ���������� ������ ������� ������
   min_rates=int(HMA_Period)+Sqrt_Period;
   min_rates_total=min_rates+int(std_period);
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(dHMA,std_period);
   
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ���������� ������� �� ����������� �� HMAShift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� HMAPeriod
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(2,BearsBuffer1,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(3,BullsBuffer1,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� �����������
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(4,BearsBuffer2,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(5,BullsBuffer2,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� �����������
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(4,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,EMPTY_VALUE);
      
//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="ColorHMA_StDev";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name+"("+string(HMA_Period)+")");
//----
  }
//+------------------------------------------------------------------+
// �������� ������ CMoving_Average                                   | 
//+------------------------------------------------------------------+  
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+ 
//|  Moving Average                                                  |
//+------------------------------------------------------------------+
int OnCalculate
(
 const int rates_total,// ���������� ������� � ����� �� ������� ����
 const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
 const int begin,// ����� ������ ������������ ������� �����
 const double &price[]// ������� ������ ��� ������� ����������
 )
  { 
   int begin0=min_rates_total+begin; 
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<begin0) return(0);

//---- ���������� ��������� ���������� 
   int first,bar,begin1;
   double lwma1,lwma2,dma,hma;
   double SMAdif,Sum,StDev,dstd,BEARS1,BULLS1,BEARS2,BULLS2,Filter1,Filter2;  
   begin1=int(HMA_Period)+begin;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated==0) // �������� �� ������ ����� ������� ����������
     {
      first=begin; // ��������� ����� ��� ������� ���� �����      
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,begin0+1);
      PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,begin0+1);
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- ���������� ���������� ������ CMoving_Average �� ����� HMASeries_Cls.mqh
   static CMoving_Average MA1,MA2,MA3;

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
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
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      ColorExtLineBuffer[bar]=0;
      if(ExtLineBuffer[bar-1]<ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=1;
      if(ExtLineBuffer[bar-1]>ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=2;
     }
     
//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first= begin0;
//---- �������� ���� ������� ���������� ����������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ��������� ���������� ���������� � ������ ��� ������������� ����������
      for(int iii=0; iii<int(std_period); iii++) dHMA[iii]=ExtLineBuffer[bar-iii]-ExtLineBuffer[bar-iii-1];

      //---- ������� ������� ������� ���������� ����������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dHMA[iii];
      SMAdif=Sum/std_period;

      //---- ������� ����� ��������� ��������� ���������� � ��������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dHMA[iii]-SMAdif,2);

      //---- ���������� �������� �������� ������������������� ���������� StDev �� ���������� ����������
      StDev=MathSqrt(Sum/std_period);

      int dig=_Digits+2;
      //---- ������������� ����������
      dstd=NormalizeDouble(dHMA[0],dig);
      Filter1=NormalizeDouble(dK1*StDev,dig);
      Filter2=NormalizeDouble(dK2*StDev,dig);
      BEARS1=EMPTY_VALUE;
      BULLS1=EMPTY_VALUE;
      BEARS2=EMPTY_VALUE;
      BULLS2=EMPTY_VALUE;
      hma=ExtLineBuffer[bar];

      //---- ���������� ������������ ��������
      if(dstd<-Filter1 && dstd>=-Filter2) BEARS1=hma; //���� ���������� �����
      if(dstd<-Filter2) BEARS2=hma; //���� ���������� �����
      if(dstd>+Filter1 && dstd<=+Filter2) BULLS1=hma; //���� ���������� �����
      if(dstd>+Filter2) BULLS2=hma; //���� ���������� �����

      //---- ������������� ����� ������������ ������� ����������� ���������� 
      BullsBuffer1[bar]=BULLS1;
      BearsBuffer1[bar]=BEARS1;
      BullsBuffer2[bar]=BULLS2;
      BearsBuffer2[bar]=BEARS2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
