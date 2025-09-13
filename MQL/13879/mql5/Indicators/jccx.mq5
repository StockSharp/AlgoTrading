//+---------------------------------------------------------------------+ 
//|                                                            JCCX.mq5 | 
//|                                Copyright � 2010,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| ��� ������ ���������� ���� SmoothAlgorithms.mqh ������� ��������    |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+ 
#property copyright "Copyright � 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.02"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ������� ����
#property indicator_color1 clrDodgerBlue
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "JCCX"
//---- ��������� �������������� ������� ����������
#property indicator_level1  0.5
#property indicator_level2 -0.5
#property indicator_level3  0.0
#property indicator_levelcolor clrDeepPink
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //PRICE_CLOSE
   PRICE_OPEN_,          //PRICE_OPEN
   PRICE_HIGH_,          //PRICE_HIGH
   PRICE_LOW_,           //PRICE_LOW
   PRICE_MEDIAN_,        //PRICE_MEDIAN
   PRICE_TYPICAL_,       //PRICE_TYPICAL
   PRICE_WEIGHTED_,      //PRICE_WEIGHTED
   PRICE_SIMPL_,         //PRICE_SIMPL_
   PRICE_QUARTER_,       //PRICE_QUARTER_
   PRICE_TRENDFOLLOW0_,  //PRICE_TRENDFOLLOW0_
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint JMALength=8; // �������  JJMA ����������� ������� ����
input int JMAPhase=100; // �������� JJMA ����������
                        // ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
input uint JurXLength=8; // ������� JurX ���������� ����������
input Applied_price_ IPC=PRICE_CLOSE_; // ������� ���������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ������������ ������
double JCCX[];
//+------------------------------------------------------------------+
//| �������� ������� CJJMA, CJurX � ������� PriceSeries()            |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| JCCX indicator initialization function                           | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,JCCX,INDICATOR_DATA);
//---- ������������� ������ ���������� �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,30);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"JCCX");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"JCCX( JMALength = ",JMALength,", JMAPhase = ",JMAPhase,", JurXLength = ",JurXLength,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- ���������� ���������� ������ CJMA �� ����� SmoothAlgorithms.mqh
   CJJMA JMA;
//---- ��������� ������� �� ������������ �������� ������� ����������
   JMA.JJMALengthCheck("JMALength",  JMALength );
   JMA.JJMALengthCheck("JurXLength", JurXLength);
   JMA.JJMAPhaseCheck ("JMAPhase",   JMAPhase  );
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| JCCX iteration function                                          | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,// ���������� ������� � ����� �� ������� ����
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
   if(rates_total<0) return(0);
//---- ���������� ���������� � ��������� ������  
   double price_,jma,up_cci,dn_cci,up_jccx,dn_jccx,jccx;
//---- ���������� ������������� ����������
   int first,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
        first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- ���������� ���������� ������ CJurX �� ����� SmoothAlgorithms.mqh
   static CJurX Jur1,Jur2;
//---- ���������� ���������� ������ CJMA �� ����� SmoothAlgorithms.mqh
   static CJJMA JMA;
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price_
      price_=PriceSeries(IPC,bar,open,low,high,close);
      //---- ���� ����� ������� JJMASeries ��� ��������� ������� JMA
      jma=JMA.JJMASeries(0,prev_calculated,rates_total,0,JMAPhase,JMALength,price_,bar,false);
      //---- ���������� ���������� ���� �� �������� �������
      up_cci = price_ - jma;
      dn_cci = MathAbs(up_cci);
      //---- ��� ������ ������� JurXSeries
      up_jccx = Jur1.JurXSeries(30, prev_calculated, rates_total, 0, JurXLength, up_cci, bar, false);
      dn_jccx = Jur2.JurXSeries(30, prev_calculated, rates_total, 0, JurXLength, dn_cci, bar, false);
      //---- �������������� ������� �� ���� �� ������ ���������
      if(dn_jccx==0) jccx=EMPTY_VALUE;
      else
        {
         jccx=up_jccx/dn_jccx;
         //---- ����������� ���������� ������ � ����� 
         if(jccx > +1)jccx = +1;
         if(jccx < -1)jccx = -1;
        }
      //---- �������� ����������� �������� � ������������ �����
      JCCX[bar]=jccx;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
