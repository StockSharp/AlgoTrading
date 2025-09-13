//+---------------------------------------------------------------------+
//|                                                             BnB.mq5 |
//|                                           Copyright � 2012, Zhaslan |
//|                                                                     |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2012, Zhaslan"
//--- ������ �� ���� ������
#property link "" 
#property description "BnB"
//--- ����� ������ ����������
#property version   "1.01"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1  clrMediumOrchid,clrDodgerBlue
//--- ����������� ����� ����������
#property indicator_label1  "BnB"
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//--- ���������� ���������� ������� CXMA � CMomentum �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ���������� ������������                     |
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
input Smooth_Method XMA_Method=MODE_T3;           // ����� ����������
input uint XLength=14;                            // ������� ����������
input int XPhase=15;                              // �������� �����������
//--- XPhase: ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- XPhase: ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK; // �����
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� �����
//--- � ���������� ������������ � �������� ������������ �������
double UpBuffer[],DnBuffer[];
//--- ���������� ����� ���������� ��� �������� ������� �����������
int Ind_Handle;
//--- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=XMA1.GetStartBars(XMA_Method,XLength,XPhase);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"BnB");
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
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(RESET);
//--- ���������� ���������� � ��������� ������  
   double tic,diff,bears,bulls;
//--- ���������� ������������� ����������
   int first,bar;
   long vol;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      if(VolumeType==VOLUME_TICK) vol=tick_volume[bar];
      else vol=volume[bar];
      if(!vol) vol=1;
      tic=(high[bar]-low[bar])/vol;
      diff=0.0;
      if(open[bar]>close[bar]) diff=((high[bar]-low[bar])-(open[bar]-close[bar]))/(2*tic);
      if(open[bar]<close[bar]) diff=((high[bar]-low[bar])-(close[bar]-open[bar]))/(2*tic);
      //---
      if(open[bar]>close[bar]) bulls=(open[bar]-close[bar])/tic+diff;
      else bulls=diff;
      //---
      if(open[bar]<close[bar]) bears=(close[bar]-open[bar])/tic+diff;
      else bears=diff;
      //---
      UpBuffer[bar]=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,bulls,bar,false);
      DnBuffer[bar]=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,bears,bar,false);
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
