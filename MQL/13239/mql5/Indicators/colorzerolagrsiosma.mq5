//+------------------------------------------------------------------+ 
//|                                          ColorZerolagRSIOSMA.mq5 | 
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
#property indicator_color1 clrDeepPink,clrOrange,clrGray,clrYellowGreen,clrDodgerBlue
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "ColorZerolagRSIOSMA"
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint    smoothing1=15;
input uint    smoothing2=7;
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // ������� ���������
//----
input double Factor1=0.05;
input uint    RSI_period1=8;
//----
input double Factor2=0.10;
input uint    RSI_period2=21;
//----
input double Factor3=0.16;
input uint    RSI_period3=34;
//----
input double Factor4=0.26;
input int    RSI_period4=55;
//----
input double Factor5=0.43;
input uint    RSI_period5=89;
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
int RSI1_Handle,RSI2_Handle,RSI3_Handle,RSI4_Handle,RSI5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagRSI indicator initialization function                    | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ��������
   smoothConst1=(smoothing1-1.0)/smoothing1;
   smoothConst2=(smoothing2-1.0)/smoothing2;
//---- 
   uint PeriodBuffer[5];
//---- ������ ���������� ����
   PeriodBuffer[0] = RSI_period1;
   PeriodBuffer[1] = RSI_period2;
   PeriodBuffer[2] = RSI_period3;
   PeriodBuffer[3] = RSI_period4;
   PeriodBuffer[4] = RSI_period5;
//----
   min_rates_total=int(3*PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)])+2;
//---- ��������� ������ ���������� iRSI1
   RSI1_Handle=iRSI(NULL,0,RSI_period1,IPC);
   if(RSI1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iRSI1");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iRSI2
   RSI2_Handle=iRSI(NULL,0,RSI_period2,IPC);
   if(RSI2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iRSI2");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iRSI3
   RSI3_Handle=iRSI(NULL,0,RSI_period3,IPC);
   if(RSI3_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iRSI3");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iRSI4
   RSI4_Handle=iRSI(NULL,0,RSI_period4,IPC);
   if(RSI4_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iRSI4");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iRSI5
   RSI5_Handle=iRSI(NULL,0,RSI_period5,IPC);
   if(RSI5_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iRSI5");
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
   string shortname="ColorZerolagRSIOSMA";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagRSI iteration function                                   | 
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
   if(BarsCalculated(RSI1_Handle)<rates_total
      || BarsCalculated(RSI2_Handle)<rates_total
      || BarsCalculated(RSI3_Handle)<rates_total
      || BarsCalculated(RSI4_Handle)<rates_total
      || BarsCalculated(RSI5_Handle)<rates_total
      || rates_total<min_rates_total)
      return(0);
//---- ���������� ���������� � ��������� ������  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend,OSMA,diff;
   double RSI1[],RSI2[],RSI3[],RSI4[],RSI5[];
//---- ���������� ������������� ����������
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
   ArraySetAsSeries(RSI1,true);
   ArraySetAsSeries(RSI2,true);
   ArraySetAsSeries(RSI3,true);
   ArraySetAsSeries(RSI4,true);
   ArraySetAsSeries(RSI5,true);
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(RSI1_Handle,0,0,to_copy,RSI1)<=0) return(0);
   if(CopyBuffer(RSI2_Handle,0,0,to_copy,RSI2)<=0) return(0);
   if(CopyBuffer(RSI3_Handle,0,0,to_copy,RSI3)<=0) return(0);
   if(CopyBuffer(RSI4_Handle,0,0,to_copy,RSI4)<=0) return(0);
   if(CopyBuffer(RSI5_Handle,0,0,to_copy,RSI5)<=0) return(0);
//---- ������ ���������� ������ limit ��� ����� ��������� ����� � ��������� ������������� ����������
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      bar=limit+1;
      Osc1 = Factor1 * RSI1[bar];
      Osc2 = Factor2 * RSI2[bar];
      Osc3 = Factor2 * RSI3[bar];
      Osc4 = Factor4 * RSI4[bar];
      Osc5 = Factor5 * RSI5[bar];
      //---
      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      SlowTrend1=FastTrend/smoothing1;
      OSMA1=(FastTrend-SlowTrend1)/smoothing2;
     }
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * RSI1[bar];
      Osc2 = Factor2 * RSI2[bar];
      Osc3 = Factor2 * RSI3[bar];
      Osc4 = Factor4 * RSI4[bar];
      Osc5 = Factor5 * RSI5[bar];
      //---
      FastTrend = Osc1 + Osc2 + Osc3 + Osc4 + Osc5;
      SlowTrend = FastTrend / smoothing1 + SlowTrend1 * smoothConst1;
      //---
      OSMA=(FastTrend-SlowTrend)/smoothing2+OSMA1*smoothConst2;
      ExtBuffer[bar]=OSMA;
      if(bar)
        {
         SlowTrend1=SlowTrend;
         OSMA1=OSMA;
        }
     }
//---
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit--;
//---- �������� ���� ��������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      clr=2;
      diff=ExtBuffer[bar]-ExtBuffer[bar+1];
      //---
      if(ExtBuffer[bar]>0)
        {
         if(diff>0) clr=4;
         if(diff<0) clr=3;
        }
      //---
      if(ExtBuffer[bar]<0)
        {
         if(diff<0) clr=0;
         if(diff>0) clr=1;
        }
      //---
      ColorExtBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
