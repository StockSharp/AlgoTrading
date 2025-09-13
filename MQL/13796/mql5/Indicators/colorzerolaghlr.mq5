//+------------------------------------------------------------------+ 
//|                                              ColorZerolagHLR.mq5 | 
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//---- ��������� ����������
#property copyright "Copyright � 2015, Nikolay Kositsin"
//---- ������ �� ���� ������
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.02"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 4 
//---- ������������ ��� ����������� ����������
#property indicator_plots   3
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
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
//+----------------------------------------------+
//| ��������� ��������� �������                  |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������� ����� ����� �������
#property indicator_type3   DRAW_FILLING
//---- � �������� ������ ������� ���������� ������������ Teal � DeepPink �����
#property indicator_color3  clrTeal,clrDeepPink
//---- ����������� ����� ����������
#property indicator_label3 "ZerolagHLR"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 80
#property indicator_level2 50
#property indicator_level3 20
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint    smoothing=15;
//----
input double Factor1=0.05;
input uint    HLR_period1=8;
//----
input double Factor2=0.10;
input uint    HLR_period2=21;
//----
input double Factor3=0.16;
input uint    HLR_period3=34;
//----
input double Factor4=0.26;
input int    HLR_period4=55;
//----
input double Factor5=0.43;
input uint    HLR_period5=89;
//+----------------------------------------------+
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
int HLR1_Handle,HLR2_Handle,HLR3_Handle,HLR4_Handle,HLR5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagHLR indicator initialization function                     | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ��������
   smoothConst=(smoothing-1.0)/smoothing;
//---- 
   uint PeriodBuffer[5];
//---- ������ ���������� ����
   PeriodBuffer[0] = HLR_period1;
   PeriodBuffer[1] = HLR_period2;
   PeriodBuffer[2] = HLR_period3;
   PeriodBuffer[3] = HLR_period4;
   PeriodBuffer[4] = HLR_period5;
//----
   StartBar=int(3*PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)])+2+1;
//---- ��������� ������ ���������� HLR 1
   HLR1_Handle=iCustom(NULL,0,"HLR",HLR_period1,0);
   if(HLR1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� HLR 1");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� HLR 2
   HLR2_Handle=iCustom(NULL,0,"HLR",HLR_period2,0);
   if(HLR2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� HLR 2");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� HLR 3
   HLR3_Handle=iCustom(NULL,0,"HLR",HLR_period3,0);
   if(HLR3_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� HLR 3");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� HLR 4
   HLR4_Handle=iCustom(NULL,0,"HLR",HLR_period4,0);
   if(HLR4_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� HLR 4");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� HLR 5
   HLR5_Handle=iCustom(NULL,0,"HLR",HLR_period5,0);
   if(HLR5_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� HLR 5");
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
   string shortname="ZerolagHLR";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagHLR iteration function                                    | 
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
   if(BarsCalculated(HLR1_Handle)<rates_total
      || BarsCalculated(HLR2_Handle)<rates_total
      || BarsCalculated(HLR3_Handle)<rates_total
      || BarsCalculated(HLR4_Handle)<rates_total
      || BarsCalculated(HLR5_Handle)<rates_total
      || rates_total<StartBar)
      return(0);
//---- ���������� ���������� � ��������� ������  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend;
   double HLR1[],HLR2[],HLR3[],HLR4[],HLR5[];
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
   ArraySetAsSeries(HLR1,true);
   ArraySetAsSeries(HLR2,true);
   ArraySetAsSeries(HLR3,true);
   ArraySetAsSeries(HLR4,true);
   ArraySetAsSeries(HLR5,true);
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(HLR1_Handle,0,0,to_copy,HLR1)<=0) return(0);
   if(CopyBuffer(HLR2_Handle,0,0,to_copy,HLR2)<=0) return(0);
   if(CopyBuffer(HLR3_Handle,0,0,to_copy,HLR3)<=0) return(0);
   if(CopyBuffer(HLR4_Handle,0,0,to_copy,HLR4)<=0) return(0);
   if(CopyBuffer(HLR5_Handle,0,0,to_copy,HLR5)<=0) return(0);
//---- ������ ���������� ������ limit ��� ����� ��������� ����� � ��������� ������������� ����������
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      bar=limit+1;
      Osc1 = Factor1 * HLR1[bar];
      Osc2 = Factor2 * HLR2[bar];
      Osc3 = Factor2 * HLR3[bar];
      Osc4 = Factor4 * HLR4[bar];
      Osc5 = Factor5 * HLR5[bar];
      //----
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      FastBuffer[bar]=FastBuffer_[bar]=FastTrend;
      SlowBuffer[bar]=SlowBuffer_[bar]=FastTrend/smoothing;
     }
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * HLR1[bar];
      Osc2 = Factor2 * HLR2[bar];
      Osc3 = Factor2 * HLR3[bar];
      Osc4 = Factor4 * HLR4[bar];
      Osc5 = Factor5 * HLR5[bar];
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
