//+------------------------------------------------------------------+ 
//|                                            ColorZerolagJJRSX.mq5 | 
//|                               Copyright � 2011, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//---- ��������� ����������
#property copyright "Copyright � 2011, Nikolay Kositsin"
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
//---- � �������� ������ ������� ���������� ������������ �����-������� ���� � ������� �����
#property indicator_color3  clrDarkTurquoise,clrDeepPink
//---- ����������� ����� ����������
#property indicator_label3 "ZerolagJJRSX"
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
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE;// ������� ���������
//----
input double Factor1=0.05;
input uint    JJRSX_period1=8;
//----
input double Factor2=0.10;
input uint    JJRSX_period2=21;
//----
input double Factor3=0.16;
input uint    JJRSX_period3=34;
//----
input double Factor4=0.26;
input int    JJRSX_period4=55;
//----
input double Factor5=0.43;
input uint    JJRSX_period5=89;
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
int JJRSX1_Handle,JJRSX2_Handle,JJRSX3_Handle,JJRSX4_Handle,JJRSX5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagJJRSX indicator initialization function                   | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ��������
   smoothConst=(smoothing-1.0)/smoothing;
//----
   StartBar=int(3*32)+2;
//---- ��������� ������ ���������� iJJRSX1
   JJRSX1_Handle=iCustom(NULL,0,"JJRSX",JJRSX_period1,Smooth,JPhase,IPC,0);
   if(JJRSX1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJJRSX1");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iJJRSX2
   JJRSX2_Handle=iCustom(NULL,0,"JJRSX",JJRSX_period2,Smooth,JPhase,IPC,0);
   if(JJRSX2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJJRSX2");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iJJRSX3
   JJRSX3_Handle=iCustom(NULL,0,"JJRSX",JJRSX_period3,Smooth,JPhase,IPC,0);
   if(JJRSX3_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJJRSX3");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iJJRSX4
   JJRSX4_Handle=iCustom(NULL,0,"JJRSX",JJRSX_period4,Smooth,JPhase,IPC,0);
   if(JJRSX4_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJJRSX4");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iJJRSX5
   JJRSX5_Handle=iCustom(NULL,0,"JJRSX",JJRSX_period5,Smooth,JPhase,IPC,0);
   if(JJRSX5_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iJJRSX5");
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
   string shortname="ZerolagJJRSX";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagJJRSX iteration function                                  | 
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
   if(BarsCalculated(JJRSX1_Handle)<rates_total
      || BarsCalculated(JJRSX2_Handle)<rates_total
      || BarsCalculated(JJRSX3_Handle)<rates_total
      || BarsCalculated(JJRSX4_Handle)<rates_total
      || BarsCalculated(JJRSX5_Handle)<rates_total
      || rates_total<StartBar)
      return(0);
//---- ���������� ���������� � ��������� ������  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend;
   double JJRSX1[],JJRSX2[],JJRSX3[],JJRSX4[],JJRSX5[];
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
   ArraySetAsSeries(JJRSX1,true);
   ArraySetAsSeries(JJRSX2,true);
   ArraySetAsSeries(JJRSX3,true);
   ArraySetAsSeries(JJRSX4,true);
   ArraySetAsSeries(JJRSX5,true);
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(JJRSX1_Handle,0,0,to_copy,JJRSX1)<=0) return(0);
   if(CopyBuffer(JJRSX2_Handle,0,0,to_copy,JJRSX2)<=0) return(0);
   if(CopyBuffer(JJRSX3_Handle,0,0,to_copy,JJRSX3)<=0) return(0);
   if(CopyBuffer(JJRSX4_Handle,0,0,to_copy,JJRSX4)<=0) return(0);
   if(CopyBuffer(JJRSX5_Handle,0,0,to_copy,JJRSX5)<=0) return(0);
//---- ������ ���������� ������ limit ��� ����� ��������� ����� � ��������� ������������� ����������
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      bar=limit+1;
      Osc1 = Factor1 * JJRSX1[bar];
      Osc2 = Factor2 * JJRSX2[bar];
      Osc3 = Factor2 * JJRSX3[bar];
      Osc4 = Factor4 * JJRSX4[bar];
      Osc5 = Factor5 * JJRSX5[bar];
      //----
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      FastBuffer[bar]=FastBuffer_[bar]=FastTrend;
      SlowBuffer[bar]=SlowBuffer_[bar]=FastTrend/smoothing;
     }
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * JJRSX1[bar];
      Osc2 = Factor2 * JJRSX2[bar];
      Osc3 = Factor2 * JJRSX3[bar];
      Osc4 = Factor4 * JJRSX4[bar];
      Osc5 = Factor5 * JJRSX5[bar];
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
