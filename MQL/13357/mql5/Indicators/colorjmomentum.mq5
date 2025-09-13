//+---------------------------------------------------------------------+
//|                                                  ColorJMomentum.mq5 | 
//|                                Copyright � 2010,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.10"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ����� ������ ����������� ����������
#property indicator_plots   4
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ����� ����
#property indicator_color1 clrGray
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "JMomentum"
//+----------------------------------------------+
//| ��������� ��������� ������� ����������       |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ��������� ����
#property indicator_color2 clrSpringGreen
//---- ������� ����� ���������� ����� 3
#property indicator_width2 3
//---- ����������� ������ ����� ����������
#property indicator_label2 "Up_Signal"
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� �����-������� ����
#property indicator_color3  clrDeepPink
//---- ������� ����� ���������� ����� 3
#property indicator_width3 3
//---- ����������� ��������� ����� ����������
#property indicator_label3 "Dn_Signal"
//+----------------------------------------------+
//| ��������� ��������� ������������� ���������� |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ������������� ���������� ����������� �����
#property indicator_color4  clrGray
//---- ������� ����� ���������� ����� 3
#property indicator_width4 3
//---- ����������� ������������ ����� ����������
#property indicator_label4 "No_Signal"
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price 
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int MLength=8;  // ������ ���������� Momentum 
input int JMLength=8; // ������� JMA ����������� ���������� Momentum                  
input int JPhase=100; // �������� JMA �����������
                      // ������������ � �������� -100 ... +100,
                      // ������ �� �������� ����������� ��������
input Applied_price_ IPC=PRICE_CLOSE; // ������� ���������
/* , �� ������� ������������ ������ ���������� ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ������������ ������
double JMomentum[];
double UpBuffer[];
double DnBuffer[];
double FlBuffer[];
//----
int start;
//+------------------------------------------------------------------+
//| �������� ������� iPriceSeries                                    |
//| �������� ������� iPriceSeriesAlert                               |
//| �������� ������ CMomentum                                        |
//+------------------------------------------------------------------+
#include <SmoothAlgorithms.mqh>  
//+------------------------------------------------------------------+   
//| JMomentum indicator initialization function                      | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- 
   start=MLength+31;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,JMomentum,INDICATOR_DATA);
//---- ������������� ������ ���������� �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,MLength);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"JMomentum");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,UpBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,start);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Up Signal");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ����� ������� ��� ���������
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,DnBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,start);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"Dn Signal");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ����� ������� ��� ���������
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,FlBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,start);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"No Signal");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ����� ������� ��� ���������
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"JMomentum( MLength = ",MLength,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� ���������� ������ CMomentum �� ����� SmoothAlgorithms.mqh
   CMomentum Mom;
//---- ���������� ���������� ������ CJJMA �� ����� SmoothAlgorithms.mqh
   CJJMA JMA;
//---- ��������� ������� �� ������������ �������� ������� ����������
   Mom.MALengthCheck("MLength",MLength);
//---- ��������� ������� �� ������������ �������� ������� ����������
   JMA.JJMALengthCheck("JMLength",JMLength);
//---- ��������� ������� �� ������������ �������� ������� ����������
   JMA.JJMAPhaseCheck("JPhase",JPhase);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| JMomentum iteration function                                     | 
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
   if(rates_total<start) return(0);
//---- ���������� ���������� � ��������� ������  
   double price,momentum,jmomentum,dmomentum;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated==0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- ���������� ���������� ������� CMomentum � CJJMA �� ����� SmoothAlgorithms.mqh
   static CMomentum Mom;
   static CJJMA JMA;
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ���������� ������� ���� dprice_
      price=PriceSeries(IPC,bar,open,low,high,close);
      //---- ��� ������ ������� MomentumSeries
      momentum=Mom.MomentumSeries(0,prev_calculated,rates_total,MLength,price,bar,false);
      //---- ���� ����� ������� JJMASeries. 
      //---- ��������� Phase � MLength �� �������� �� ������ ���� (Din = 0) 
      jmomentum=JMA.JJMASeries(MLength+1,prev_calculated,rates_total,0,JPhase,JMLength,momentum,bar,false);
      //---- �������� ����������� �������� � ������������ �����
      JMomentum[bar]=jmomentum/_Point;
      //---- ������������� ����� ������������ ������� ������
      UpBuffer[bar] = EMPTY_VALUE;
      DnBuffer[bar] = EMPTY_VALUE;
      FlBuffer[bar] = EMPTY_VALUE;
      //----
      if(bar<start) continue;
      //---- ������������� ����� ������������ ������� ����������� ���������� 
      dmomentum=NormalizeDouble(JMomentum[bar]-JMomentum[bar-1],0);
      if(dmomentum>0) UpBuffer[bar] = JMomentum[bar]; //���� ���������� �����
      if(dmomentum<0) DnBuffer[bar] = JMomentum[bar]; //���� ���������� �����
      if(dmomentum==0) FlBuffer[bar]= JMomentum[bar]; //��� ������
     }
//----     
   return(rates_total);
  }
//+X----------------------+ <<< The End >>> +-----------------------X+
