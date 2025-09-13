//+------------------------------------------------------------------+ 
//|                                              ColorZerolagRVI.mq5 | 
//|                               Copyright � 2011, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//---- ��������� ����������
#property copyright "Copyright � 2011, Nikolay Kositsin"
//---- ������ �� ���� ������
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.00"
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
//---- � �������� ������ ������� ���������� ������������ ��������� ���� � ������� �����
#property indicator_color3  clrSpringGreen,clrRed
//---- ����������� ����� ����������
#property indicator_label3 "ZerolagRVI"
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint   smoothing=15;
//----
input double Factor1=0.05;
input int    RVI_period1=8;
//----
input double Factor2=0.10;
input int    RVI_period2=21;
//----
input double Factor3=0.16;
input int    RVI_period3=34;
//----
input double Factor4=0.26;
input int    RVI_period4=55;
//----
input double Factor5=0.43;
input int    RVI_period5=89;
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
int RVI1_Handle,RVI2_Handle,RVI3_Handle,RVI4_Handle,RVI5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagRVI indicator initialization function                     | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ��������
   smoothConst=(smoothing-1.0)/smoothing;
//----
   int PeriodBuffer[5];
//---- ������ ���������� ����
   PeriodBuffer[0] = RVI_period1;
   PeriodBuffer[1] = RVI_period2;
   PeriodBuffer[2] = RVI_period3;
   PeriodBuffer[3] = RVI_period4;
   PeriodBuffer[4] = RVI_period5;
//----
   StartBar=PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)]+2;
//---- ��������� ������ ���������� iRVI1
   RVI1_Handle=iRVI(NULL,0,RVI_period1);
   if(RVI1_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� iRVI1");
//---- ��������� ������ ���������� iRVI2
   RVI2_Handle=iRVI(NULL,0,RVI_period2);
   if(RVI2_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� iRVI2");
//---- ��������� ������ ���������� iRVI3
   RVI3_Handle=iRVI(NULL,0,RVI_period3);
   if(RVI3_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� iRVI3");
//---- ��������� ������ ���������� iRVI4
   RVI4_Handle=iRVI(NULL,0,RVI_period4);
   if(RVI4_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� iRVI4");
//---- ��������� ������ ���������� iRVI5
   RVI5_Handle=iRVI(NULL,0,RVI_period5);
   if(RVI5_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� iRVI5");
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
   string shortname="ZerolagRVI";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,4);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| ZerolagRVI iteration function                                    | 
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
   if(BarsCalculated(RVI1_Handle)<rates_total
      || BarsCalculated(RVI2_Handle)<rates_total
      || BarsCalculated(RVI3_Handle)<rates_total
      || BarsCalculated(RVI4_Handle)<rates_total
      || BarsCalculated(RVI5_Handle)<rates_total
      || rates_total<StartBar)
      return(0);
//---- ���������� ���������� � ��������� ������  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend;
   double RVI1[],RVI2[],RVI3[],RVI4[],RVI5[];
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
   ArraySetAsSeries(RVI1,true);
   ArraySetAsSeries(RVI2,true);
   ArraySetAsSeries(RVI3,true);
   ArraySetAsSeries(RVI4,true);
   ArraySetAsSeries(RVI5,true);
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(RVI1_Handle,0,0,to_copy,RVI1)<=0) return(0);
   if(CopyBuffer(RVI2_Handle,0,0,to_copy,RVI2)<=0) return(0);
   if(CopyBuffer(RVI3_Handle,0,0,to_copy,RVI3)<=0) return(0);
   if(CopyBuffer(RVI4_Handle,0,0,to_copy,RVI4)<=0) return(0);
   if(CopyBuffer(RVI5_Handle,0,0,to_copy,RVI5)<=0) return(0);
//---- ������� ������������ ���������� ���������� ������,
//---- ���������� ������ limit ��� ����� ��������� �����
//---- � ��������� ������������� ����������
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      bar=limit+1;
      Osc1 = Factor1 * RVI1[bar];
      Osc2 = Factor2 * RVI2[bar];
      Osc3 = Factor2 * RVI3[bar];
      Osc4 = Factor4 * RVI4[bar];
      Osc5 = Factor5 * RVI5[bar];
      //---
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      FastBuffer[bar]=FastBuffer_[bar]=FastTrend;
      SlowBuffer[bar]=SlowBuffer_[bar]=FastTrend/smoothing;
     }
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * RVI1[bar];
      Osc2 = Factor2 * RVI2[bar];
      Osc3 = Factor2 * RVI3[bar];
      Osc4 = Factor4 * RVI4[bar];
      Osc5 = Factor5 * RVI5[bar];
      //---
      FastTrend = Osc1 + Osc2 + Osc3 + Osc4 + Osc5;
      SlowTrend = FastTrend / smoothing + SlowBuffer[bar + 1] * smoothConst;
      //---
      SlowBuffer[bar]=SlowTrend;
      FastBuffer[bar]=FastTrend;

      SlowBuffer_[bar]=SlowTrend;
      FastBuffer_[bar]=FastTrend;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
