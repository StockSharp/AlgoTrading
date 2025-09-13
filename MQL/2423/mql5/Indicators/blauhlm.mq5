//+---------------------------------------------------------------------+
//|                                                         BlauHLM.mq5 |
//|                                  Copyright � 2014, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2014, Nikolay Kositsin"
//--- ������ �� ���� ������
#property link "farria@mail.redcom.ru" 
#property description "HLM Oscillator"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ������������ ������ ������
#property indicator_buffers 4
//--- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� 1             |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1  clrBlue,clrPurple
//--- ����������� ����� ����������
#property indicator_label1  "Blau HLM Signal"
//+----------------------------------------------+
//| ��������� ��������� ���������� 2             |
//+----------------------------------------------+
//--- ��������� ���������� � ���� ������������� �����������
#property indicator_type2 DRAW_COLOR_HISTOGRAM
//--- � �������� ������ ����������� ����������� ������������
#property indicator_color2 clrDeepPink,clrOrange,clrGray,clrYellowGreen,clrTeal
//--- ����� ���������� - ��������
#property indicator_style2 STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width2 2
//--- ����������� ����� ����������
#property indicator_label2  "Blau HLM"
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4;
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
input Smooth_Method XMA_Method=MODE_EMA; // ����� ����������
input uint XLength=2;                    // ������ ���������
input uint XLength1=20;                  // ������� ������� ����������
input uint XLength2=5;                   // ������� ������� ����������
input uint XLength3=3;                   // ������� �������� ����������
input uint XLength4=3;                   // ������� ���������� ���������� �����
input int XPhase=15;                     // �������� �����������
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
double UpBuffer[],DnBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2,min_rates_3,min_rates_4;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_1=int(XLength);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_4=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
   min_rates_total=min_rates_4+XMA1.GetStartBars(XMA_Method,XLength4,XPhase);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,IndBuffer,INDICATOR_DATA);
//--- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(3,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"BlauHLM");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---
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
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);
//--- ���������� ��������� ���������� 
   double hmu,lmd,hlm,xhlm,xxhlm,xxxhlm,sign;
   int first,bar;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=min_rates_1; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      hmu=high[bar]-high[bar-(XLength-1)];
      lmd=-(low[bar]-low[bar-(XLength-1)]);
      //---      
      hmu=(hmu>0)?hmu:0;
      lmd=(lmd>0)?lmd:0;
      hlm=hmu-lmd;
      hlm/=_Point;
      //---  
      xhlm=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,hlm,bar,false);
      xxhlm=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xhlm,bar,false);
      xxxhlm=XMA3.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,xxhlm,bar,false);
      sign=XMA4.XMASeries(min_rates_4,prev_calculated,rates_total,XMA_Method,XPhase,XLength4,xxxhlm,bar,false);
      //---
      IndBuffer[bar]=xxxhlm;
      UpBuffer[bar]=xxxhlm;
      DnBuffer[bar]=sign;
     }
//---
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//--- �������� ���� ��������� ���������� Ind
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }
      ColorIndBuffer[bar]=clr;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
