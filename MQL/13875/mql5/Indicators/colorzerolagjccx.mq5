//+------------------------------------------------------------------+ 
//|                                             ColorZerolagJCCX.mq5 | 
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//---- ��������� ����������
#property copyright "Copyright � 2015, Nikolay Kositsin"
//---- ������ �� ���� ������
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 4 
//---- ������������ ��� ����������� ����������
#property indicator_plots   3
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ����-���������� ����
#property indicator_color1 clrBlueViolet
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1 "FastTrendLine"
//---- ��������� ���������� � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ����-���������� ����
#property indicator_color2 clrBlueViolet
//---- ����� ���������� - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width2  1
//---- ����������� ����� ����������
#property indicator_label2 "SlowTrendLine"
//+-----------------------------------+
//| ��������� ��������� �������       |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������� ����� ����� �������
#property indicator_type3   DRAW_FILLING
//---- � �������� ������ ������� ���������� ������������ �����
#property indicator_color3  clrDeepSkyBlue,clrHotPink
//---- ����������� ����� ����������
#property indicator_label3 "ZerolagJCCX"
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
input uint    smoothing=15;
input uint Smooth = 8;  // ������� JJMA ���������� 
input int JPhase = 100; // �������� JJMA ����������
//---- ������������ � �������� -100 ... +100,
//---- ������ �� �������� ����������� ��������;
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // ������� ���������
//----
input double Factor1=0.05;
input uint    JCCX_period1=8;
//----
input double Factor2=0.10;
input uint    JCCX_period2=21;
//----
input double Factor3=0.16;
input uint    JCCX_period3=34;
//----
input double Factor4=0.26;
input int    JCCX_period4=55;
//----
input double Factor5=0.43;
input uint    JCCX_period5=89;
//+-----------------------------------+
//---- ���������� ������������� ���������� ������ ������� ������
int StartBar;
//---- ���������� ���������� � ��������� ������
double smoothConst;
//---- ������������ ������
double FastBuffer[];
double SlowBuffer[];
double FastBuffer_[];
double SlowBuffer_[];
//---- ���������� ���������� ��� �������� ������� �����������
int JCCX1_Handle,JCCX2_Handle,JCCX3_Handle,JCCX4_Handle,JCCX5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagJCCX indicator initialization function                    | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ��������
   smoothConst=(smoothing-1.0)/smoothing;
//----
   StartBar=int(3*32)+2;
//---- ��������� ������ ���������� iJCCX1
   JCCX1_Handle=iCustom(NULL,0,"JCCX",JCCX_period1,JPhase,Smooth,IPC,0);
   if(JCCX1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJCCX1");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iJCCX2
   JCCX2_Handle=iCustom(NULL,0,"JCCX",JCCX_period2,JPhase,Smooth,IPC,0);
   if(JCCX2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJCCX2");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iJCCX3
   JCCX3_Handle=iCustom(NULL,0,"JCCX",JCCX_period3,JPhase,Smooth,IPC,0);
   if(JCCX3_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJCCX3");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iJCCX4
   JCCX4_Handle=iCustom(NULL,0,"JCCX",JCCX_period4,JPhase,Smooth,IPC,0);
   if(JCCX4_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJCCX4");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iJCCX5
   JCCX5_Handle=iCustom(NULL,0,"JCCX",JCCX_period5,JPhase,Smooth,IPC,0);
   if(JCCX5_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJCCX5");
      return(INIT_FAILED);
     }
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,FastBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBar);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"FastTrendLine");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(FastBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,SlowBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBar);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"SlowTrendLine");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SlowBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,FastBuffer_,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(FastBuffer_,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,SlowBuffer_,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SlowBuffer_,true);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBar);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"FastTrendLine");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname="ZerolagJCCX";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagJCCX iteration function                                   | 
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
   if(BarsCalculated(JCCX1_Handle)<rates_total
      || BarsCalculated(JCCX2_Handle)<rates_total
      || BarsCalculated(JCCX3_Handle)<rates_total
      || BarsCalculated(JCCX4_Handle)<rates_total
      || BarsCalculated(JCCX5_Handle)<rates_total
      || rates_total<StartBar)
      return(0);
//---- ���������� ���������� � ��������� ������  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend;
   double JCCX1[],JCCX2[],JCCX3[],JCCX4[],JCCX5[];
//---- ���������� ������������� ����������
   int limit,to_copy,bar;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-StartBar-2; // ��������� ����� ��� ������� ���� �����
      to_copy=limit+2;
     }
   else // ��������� ����� ��� ������� ����� �����
     {
      limit=rates_total-prev_calculated;  // ��������� ����� ��� ������� ������ ����� �����
      to_copy=limit+1;
     }
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(JCCX1,true);
   ArraySetAsSeries(JCCX2,true);
   ArraySetAsSeries(JCCX3,true);
   ArraySetAsSeries(JCCX4,true);
   ArraySetAsSeries(JCCX5,true);
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(JCCX1_Handle,0,0,to_copy,JCCX1)<=0) return(0);
   if(CopyBuffer(JCCX2_Handle,0,0,to_copy,JCCX2)<=0) return(0);
   if(CopyBuffer(JCCX3_Handle,0,0,to_copy,JCCX3)<=0) return(0);
   if(CopyBuffer(JCCX4_Handle,0,0,to_copy,JCCX4)<=0) return(0);
   if(CopyBuffer(JCCX5_Handle,0,0,to_copy,JCCX5)<=0) return(0);
//---- ������ ���������� ������ limit ��� ����� ��������� ����� � ��������� ������������� ����������
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      bar=limit+1;
      Osc1 = Factor1 * JCCX1[bar];
      Osc2 = Factor2 * JCCX2[bar];
      Osc3 = Factor2 * JCCX3[bar];
      Osc4 = Factor4 * JCCX4[bar];
      Osc5 = Factor5 * JCCX5[bar];
      //----
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      FastBuffer[bar]=FastBuffer_[bar]=FastTrend;
      SlowBuffer[bar]=SlowBuffer_[bar]=FastTrend/smoothing;
     }
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * JCCX1[bar];
      Osc2 = Factor2 * JCCX2[bar];
      Osc3 = Factor2 * JCCX3[bar];
      Osc4 = Factor4 * JCCX4[bar];
      Osc5 = Factor5 * JCCX5[bar];
      //----
      FastTrend = Osc1 + Osc2 + Osc3 + Osc4 + Osc5;
      SlowTrend = FastTrend / smoothing + SlowBuffer[bar + 1] * smoothConst;
      //----
      SlowBuffer[bar]=SlowTrend;
      FastBuffer[bar]=FastTrend;
      //----
      SlowBuffer_[bar]=SlowTrend;
      FastBuffer_[bar]=FastTrend;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
