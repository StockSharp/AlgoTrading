//+------------------------------------------------------------------+ 
//|                                         ColorZerolagTriXOSMA.mq5 | 
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//---- ��������� ����������
#property copyright "Copyright � 2015, Nikolay Kositsin"
//---- ������ �� ���� ������
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ �������������� ����������� ������������
#property indicator_color1 clrOrange,clrYellow,clrGray,clrYellowGreen,clrTeal
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "ColorZerolagTriXOSMA"
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint    smoothing1=15;
input uint    smoothing2=7;
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // ������� ���������
//----
input double Factor1=0.05;
input uint    TriX_period1=8;
//----
input double Factor2=0.10;
input uint    TriX_period2=21;
//----
input double Factor3=0.16;
input uint    TriX_period3=34;
//----
input double Factor4=0.26;
input int    TriX_period4=55;
//----
input double Factor5=0.43;
input uint    TriX_period5=89;
//+-----------------------------------+
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ���������� � ��������� ������
double smoothConst1,smoothConst2;
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtBuffer[];
double ColorExtBuffer[];
//---- ���������� ���������� ��� �������� ������� �����������
int TriX1_Handle,TriX2_Handle,TriX3_Handle,TriX4_Handle,TriX5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagTriX indicator initialization function                    | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ��������
   smoothConst1=(smoothing1-1.0)/smoothing1;
   smoothConst2=(smoothing2-1.0)/smoothing2;
//---- 
   uint PeriodBuffer[5];
//---- ������ ���������� ����
   PeriodBuffer[0] = TriX_period1;
   PeriodBuffer[1] = TriX_period2;
   PeriodBuffer[2] = TriX_period3;
   PeriodBuffer[3] = TriX_period4;
   PeriodBuffer[4] = TriX_period5;
//----
   min_rates_total=int(3*PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)])+2;
//---- ��������� ������ ���������� iTriX1
   TriX1_Handle=iTriX(NULL,0,TriX_period1,IPC);
   if(TriX1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iTriX1");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iTriX2
   TriX2_Handle=iTriX(NULL,0,TriX_period2,IPC);
   if(TriX2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iTriX2");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iTriX3
   TriX3_Handle=iTriX(NULL,0,TriX_period3,IPC);
   if(TriX3_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iTriX3");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iTriX4
   TriX4_Handle=iTriX(NULL,0,TriX_period4,IPC);
   if(TriX4_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iTriX4");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iTriX5
   TriX5_Handle=iTriX(NULL,0,TriX_period5,IPC);
   if(TriX5_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iTriX5");
      return(INIT_FAILED);
     }
//---- ����������� ������������� ������� MAMABuffer � ������������ �����
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtBuffer,true);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorExtBuffer,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname="ColorZerolagTriXOSMA";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,6);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagTriX iteration function                                   | 
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
   if(BarsCalculated(TriX1_Handle)<rates_total
      || BarsCalculated(TriX2_Handle)<rates_total
      || BarsCalculated(TriX3_Handle)<rates_total
      || BarsCalculated(TriX4_Handle)<rates_total
      || BarsCalculated(TriX5_Handle)<rates_total
      || rates_total<min_rates_total)
      return(0);
//---- ���������� ���������� � ��������� ������  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend,OSMA,diff;
   double TriX1[],TriX2[],TriX3[],TriX4[],TriX5[];
//---- ���������� ����� ����������
   int limit,to_copy,bar,clr;
   static double SlowTrend1,OSMA1;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-2; // ��������� ����� ��� ������� ���� �����
      to_copy=limit+2;
     }
   else // ��������� ����� ��� ������� ����� �����
     {
      limit=rates_total-prev_calculated;  // ��������� ����� ��� ������� ������ ����� �����
      to_copy=limit+1;
     }
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(TriX1,true);
   ArraySetAsSeries(TriX2,true);
   ArraySetAsSeries(TriX3,true);
   ArraySetAsSeries(TriX4,true);
   ArraySetAsSeries(TriX5,true);
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(TriX1_Handle,0,0,to_copy,TriX1)<=0) return(0);
   if(CopyBuffer(TriX2_Handle,0,0,to_copy,TriX2)<=0) return(0);
   if(CopyBuffer(TriX3_Handle,0,0,to_copy,TriX3)<=0) return(0);
   if(CopyBuffer(TriX4_Handle,0,0,to_copy,TriX4)<=0) return(0);
   if(CopyBuffer(TriX5_Handle,0,0,to_copy,TriX5)<=0) return(0);
//---- ������ ���������� ������ limit ��� ����� ��������� ����� � ��������� ������������� ����������
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      bar=limit+1;
      Osc1 = Factor1 * TriX1[bar];
      Osc2 = Factor2 * TriX2[bar];
      Osc3 = Factor2 * TriX3[bar];
      Osc4 = Factor4 * TriX4[bar];
      Osc5 = Factor5 * TriX5[bar];
      //----
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      SlowTrend1=FastTrend/smoothing1;
      OSMA1=(FastTrend-SlowTrend1)/smoothing2;
     }
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * TriX1[bar];
      Osc2 = Factor2 * TriX2[bar];
      Osc3 = Factor2 * TriX3[bar];
      Osc4 = Factor4 * TriX4[bar];
      Osc5 = Factor5 * TriX5[bar];
      //----
      FastTrend = Osc1 + Osc2 + Osc3 + Osc4 + Osc5;
      SlowTrend = FastTrend / smoothing1 + SlowTrend1 * smoothConst1;
      //----
      OSMA=(FastTrend-SlowTrend)/smoothing2+OSMA1*smoothConst2;
      ExtBuffer[bar]=OSMA;
      if(bar)
        {
         SlowTrend1=SlowTrend;
         OSMA1=OSMA;
        }
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit--;
//---- �������� ���� ��������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      clr=2;
      diff=ExtBuffer[bar]-ExtBuffer[bar+1];
      //----
      if(ExtBuffer[bar]>0)
        {
         if(diff>0) clr=4;
         if(diff<0) clr=3;
        }
      //----
      if(ExtBuffer[bar]<0)
        {
         if(diff<0) clr=0;
         if(diff>0) clr=1;
        }
      //----
      ColorExtBuffer[bar]=clr;
     }
//----
   return(rates_total);
  }
//+------------------------------------------------------------------+
