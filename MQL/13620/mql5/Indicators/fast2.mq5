//+---------------------------------------------------------------------+
//|                                                           Fast2.mq5 | 
//|                                             Copyright � 2008, xrust | 
//|                                                                     | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2008, xrust"
#property link ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 3
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   3
//+-----------------------------------+
//| ��������� ��������� ���������� 1  |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����������
#property indicator_type1 DRAW_HISTOGRAM
//---- � �������� ������ ����������� ������������
#property indicator_color1 clrBlueViolet
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "Fast2 HISTOGRAM"
//+-----------------------------------+
//| ��������� ��������� ���������� 2  |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type2 DRAW_LINE
//---- � �������� ����� ����� �����������
#property indicator_color2 clrTeal
//---- ����� ���������� - �������� ������
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width2 1
//---- ����������� ����� ���������� �����
#property indicator_label2  "Fast Signal"
//+-----------------------------------+
//| ��������� ��������� ���������� 3  |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type3 DRAW_LINE
//---- � �������� ����� ����� �����������
#property indicator_color3 clrRed
//---- ����� ���������� - �������� ������
#property indicator_style3 STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width3 1
//---- ����������� ����� ���������� �����
#property indicator_label3  "Slow Signal"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
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
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_LWMA; // ����� ���������� ������� �����������
input uint Length1=3; // �������  ������� �����������
input int  Phase1=15; // �������� ������� �����������
                      // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                      // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method2=MODE_LWMA; // ����� ���������� ������� �����������
input uint Length2=9; // �������  ������� �����������
input int  Phase2=15; // �������� ������� �����������
                      // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                      // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double HistBuffer[],Sign1Buffer[],Sign2Buffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   int min_rates_1=XMA1.GetStartBars(MA_Method1,Length1,Phase1);
   int min_rates_2=XMA2.GetStartBars(MA_Method2,Length2,Phase2);
   min_rates_total=min_rates_1+min_rates_2+2;
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Length1",Length1);
   XMA2.XMALengthCheck("Length2",Length2);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("Phase1",Phase1,MA_Method1);
   XMA2.XMAPhaseCheck("Phase2",Phase2,MA_Method2);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,HistBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,Sign1Buffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,Sign2Buffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(MA_Method1);
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   StringConcatenate(shortname,"Fast2(",Length1,", ",Length2,", ",Smooth1,", ",Smooth2,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
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
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=2; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
     }
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      HistBuffer[bar]=(close[bar]-open[bar]+((close[bar-1]-open[bar-1])/MathSqrt(2))+((close[bar-2]-open[bar-2])/MathSqrt(3)))/_Point;
      //---- ��� ������ ������� XMASeries. 
      Sign1Buffer[bar]=XMA1.XMASeries(2,prev_calculated,rates_total,MA_Method1,Phase1,Length1,HistBuffer[bar],bar,false);
      Sign2Buffer[bar]=XMA2.XMASeries(2,prev_calculated,rates_total,MA_Method2,Phase2,Length2,HistBuffer[bar],bar,false);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
