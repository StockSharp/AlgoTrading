//+------------------------------------------------------------------+ 
//|                                         ColorZerolagDeMarker.mq5 | 
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
//|  ��������� ��������� ����������   |
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
//|  ��������� ��������� �������      |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������� ����� ����� �������
#property indicator_type3   DRAW_FILLING
//---- � �������� ������ ������� ���������� ������������ DodgerBlue � Red �����
#property indicator_color3  clrDodgerBlue,clrRed
//---- ����������� ����� ����������
#property indicator_label3 "ZerolagDeMarker"
//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input uint    smoothing=15;
//----
input double Factor1=0.05;
input uint    DeMarker_period1=8;
//----
input double Factor2=0.10;
input uint    DeMarker_period2=21;
//----
input double Factor3=0.16;
input uint    DeMarker_period3=34;
//----
input double Factor4=0.26;
input int    DeMarker_period4=55;
//----
input double Factor5=0.43;
input uint    DeMarker_period5=89;
//+-----------------------------------+

//---- ���������� ����� ���������� ������ ������� ������
int StartBar;
//---- ���������� ���������� � ��������� ������
double smoothConst;
//---- ������������ ������
double FastBuffer[];
double SlowBuffer[];
double FastBuffer_[];
double SlowBuffer_[];
//---- ���������� ���������� ��� �������� ������� �����������
int DeMarker1_Handle,DeMarker2_Handle,DeMarker3_Handle,DeMarker4_Handle,DeMarker5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagDeMarker indicator initialization function                | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ��������
   smoothConst=(smoothing-1.0)/smoothing;
//---- 
   uint PeriodBuffer[5];
//---- ������ ���������� ����
   PeriodBuffer[0] = DeMarker_period1;
   PeriodBuffer[1] = DeMarker_period2;
   PeriodBuffer[2] = DeMarker_period3;
   PeriodBuffer[3] = DeMarker_period4;
   PeriodBuffer[4] = DeMarker_period5;
//----
   StartBar=int(3*PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)])+2;

//---- ��������� ������ ���������� iDeMarker1
   DeMarker1_Handle=iDeMarker(NULL,0,DeMarker_period1);
   if(DeMarker1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iDeMarker1");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iDeMarker2
   DeMarker2_Handle=iDeMarker(NULL,0,DeMarker_period2);
   if(DeMarker2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iDeMarker2");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iDeMarker3
   DeMarker3_Handle=iDeMarker(NULL,0,DeMarker_period3);
   if(DeMarker3_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iDeMarker3");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iDeMarker4
   DeMarker4_Handle=iDeMarker(NULL,0,DeMarker_period4);
   if(DeMarker4_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iDeMarker4");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iDeMarker5
   DeMarker5_Handle=iDeMarker(NULL,0,DeMarker_period5);
   if(DeMarker5_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iDeMarker5");
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
   string shortname="ZerolagDeMarker";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagDeMarker iteration function                               | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(DeMarker1_Handle)<rates_total
      || BarsCalculated(DeMarker2_Handle)<rates_total
      || BarsCalculated(DeMarker3_Handle)<rates_total
      || BarsCalculated(DeMarker4_Handle)<rates_total
      || BarsCalculated(DeMarker5_Handle)<rates_total
      || rates_total<StartBar)
      return(0);

//---- ���������� ���������� � ��������� ������  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend;
   double DeMarker1[],DeMarker2[],DeMarker3[],DeMarker4[],DeMarker5[];

//---- ���������� ����� ����������
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
   ArraySetAsSeries(DeMarker1,true);
   ArraySetAsSeries(DeMarker2,true);
   ArraySetAsSeries(DeMarker3,true);
   ArraySetAsSeries(DeMarker4,true);
   ArraySetAsSeries(DeMarker5,true);

//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(DeMarker1_Handle,0,0,to_copy,DeMarker1)<=0) return(0);
   if(CopyBuffer(DeMarker2_Handle,0,0,to_copy,DeMarker2)<=0) return(0);
   if(CopyBuffer(DeMarker3_Handle,0,0,to_copy,DeMarker3)<=0) return(0);
   if(CopyBuffer(DeMarker4_Handle,0,0,to_copy,DeMarker4)<=0) return(0);
   if(CopyBuffer(DeMarker5_Handle,0,0,to_copy,DeMarker5)<=0) return(0);

//---- ������ ���������� ������ limit ��� ����� ��������� ����� � ��������� ������������� ����������
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      bar=limit+1;
      Osc1 = Factor1 * DeMarker1[bar];
      Osc2 = Factor2 * DeMarker2[bar];
      Osc3 = Factor2 * DeMarker3[bar];
      Osc4 = Factor4 * DeMarker4[bar];
      Osc5 = Factor5 * DeMarker5[bar];

      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      FastBuffer[bar]=FastBuffer_[bar]=FastTrend;
      SlowBuffer[bar]=SlowBuffer_[bar]=FastTrend/smoothing;
     }

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * DeMarker1[bar];
      Osc2 = Factor2 * DeMarker2[bar];
      Osc3 = Factor2 * DeMarker3[bar];
      Osc4 = Factor4 * DeMarker4[bar];
      Osc5 = Factor5 * DeMarker5[bar];

      FastTrend = Osc1 + Osc2 + Osc3 + Osc4 + Osc5;
      SlowTrend = FastTrend / smoothing + SlowBuffer[bar + 1] * smoothConst;

      SlowBuffer[bar]=SlowTrend;
      FastBuffer[bar]=FastTrend;

      SlowBuffer_[bar]=SlowTrend;
      FastBuffer_[bar]=FastTrend;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
