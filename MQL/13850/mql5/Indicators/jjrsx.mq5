//+---------------------------------------------------------------------+ 
//|                                                           JJRSX.mq5 | 
//|                                Copyright � 2010,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| ��� ������ ���������� ���� SmoothAlgorithms.mqh ������� ��������    |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+ 
#property copyright "Copyright � 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ���������-����� ����
#property indicator_color1 clrBlueViolet
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "JJRSX"
//---- ��������� �������������� ������� ����������
#property indicator_level1  0.5
#property indicator_level2 -0.5
#property indicator_level3  0.0
#property indicator_levelcolor clrMagenta
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
enum Applied_price_      // ��� ���������
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price 
  };
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint JLength=8;   // �������  �����������
input uint Smooth = 8;  // ������� JJMA ����������
input int JPhase = 100; // �������� JJMA ����������
                        // ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
input Applied_price_ IPC=PRICE_CLOSE_; // ������� ���������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ������������ ������
double JJRSX[];
//+------------------------------------------------------------------+
//| �������� ������� iPriceSeries                                    |
//| �������� ������ CJurX                                            |
//| �������� ������ CJJMA                                            |
//+------------------------------------------------------------------+  
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| JJRSX indicator initialization function                          | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,JJRSX,INDICATOR_DATA);
//---- ������������� ������ ���������� �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,32);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"JJRSX( Length = ",JLength,
                     ", Smooth = ",Smooth,", Phase = ",JPhase,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- ���������� ���������� ������ CJJMA �� ����� JJMASeries_Cls.mqh
   CJJMA JMA;
//---- ��������� ������� �� ������������ �������� ������� ����������
   JMA.JJMALengthCheck("Length",JLength);
   JMA.JJMALengthCheck("Smooth",Smooth);
   JMA.JJMAPhaseCheck ("Phase",JPhase);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| JJRSX iteration function                                         | 
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
   if(rates_total<32) return(0);
//---- ���������� ���������� � ��������� ������  
   double dprice,udprice,up_jrsx,dn_jrsx,jrsx;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=1; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- ���������� ���������� ������ JurX �� ����� JurXSeries_Cls.mqh
   static CJurX Jur1,Jur2;
//---- ���������� ���������� ������ CJJMA �� ����� JJMASeries_Cls.mqh
   static CJJMA JMA;
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ���������� ������� ���� dprice
      dprice=PriceSeries(IPC,bar,open,low,high,close)-PriceSeries(IPC,bar-1,open,low,high,close);
      //----
      udprice=MathAbs(dprice);
      //---- ��� ������ ������� JurXSeries
      up_jrsx = Jur1.JurXSeries(1,prev_calculated,rates_total,0,JLength,dprice,bar,false);
      dn_jrsx = Jur2.JurXSeries(1,prev_calculated,rates_total,0,JLength,udprice,bar,false);
      //---- �������������� ������� �� ���� �� ������ ���������
      if(!dn_jrsx) jrsx=EMPTY_VALUE;
      else
        {
         jrsx=up_jrsx/dn_jrsx;
         //---- ����������� ���������� ������ � �����
         jrsx=MathMax(MathMin(jrsx,+1),-1);
        }
      //---- ���� ����� ������� JJMASeries
      JJRSX[bar]=JMA.JJMASeries(1,prev_calculated,rates_total,0,JPhase,Smooth,jrsx,bar,false);
     }
//----
   return(rates_total);
  }
//+------------------------------------------------------------------+
