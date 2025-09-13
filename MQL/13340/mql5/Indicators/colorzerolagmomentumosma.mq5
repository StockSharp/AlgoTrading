//+------------------------------------------------------------------+ 
//|                                     ColorZerolagMomentumOSMA.mq5 | 
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
#property indicator_color1 clrOrange,clrYellow,clrGray,clrAqua,clrRoyalBlue
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "ColorZerolagMomentumOSMA"
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint    smoothing1=15;
input uint    smoothing2=15;
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // ������� ���������
//----
input double Factor1=0.43;
input uint    Momentum_period1=8;
//----
input double Factor2=0.26;
input uint    Momentum_period2=21;
//----
input double Factor3=0.16;
input uint    Momentum_period3=34;
//----
input double Factor4=0.10;
input int    Momentum_period4=55;
//----
input double Factor5=0.05;
input uint    Momentum_period5=89;
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
int Momentum1_Handle,Momentum2_Handle,Momentum3_Handle,Momentum4_Handle,Momentum5_Handle;
//+------------------------------------------------------------------+    
//| ZerolagMomentum indicator initialization function                | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ��������
   smoothConst1=(smoothing1-1.0)/smoothing1;
   smoothConst2=(smoothing2-1.0)/smoothing2;
//---- 
   uint PeriodBuffer[5];
//---- ������ ���������� ����
   PeriodBuffer[0] = Momentum_period1;
   PeriodBuffer[1] = Momentum_period2;
   PeriodBuffer[2] = Momentum_period3;
   PeriodBuffer[3] = Momentum_period4;
   PeriodBuffer[4] = Momentum_period5;
//----
   min_rates_total=int(3*PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)])+2;
//---- ��������� ������ ���������� iMomentum1
   Momentum1_Handle=iMomentum(NULL,0,Momentum_period1,IPC);
   if(Momentum1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMomentum1");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iMomentum2
   Momentum2_Handle=iMomentum(NULL,0,Momentum_period2,IPC);
   if(Momentum2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMomentum2");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iMomentum3
   Momentum3_Handle=iMomentum(NULL,0,Momentum_period3,IPC);
   if(Momentum3_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMomentum3");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iMomentum4
   Momentum4_Handle=iMomentum(NULL,0,Momentum_period4,IPC);
   if(Momentum4_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMomentum4");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� iMomentum5
   Momentum5_Handle=iMomentum(NULL,0,Momentum_period5,IPC);
   if(Momentum5_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMomentum5");
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
   string shortname="ColorZerolagMomentumOSMA";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| ZerolagMomentum iteration function                                   | 
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
   if(BarsCalculated(Momentum1_Handle)<rates_total
      || BarsCalculated(Momentum2_Handle)<rates_total
      || BarsCalculated(Momentum3_Handle)<rates_total
      || BarsCalculated(Momentum4_Handle)<rates_total
      || BarsCalculated(Momentum5_Handle)<rates_total
      || rates_total<min_rates_total)
      return(0);
//---- ���������� ���������� � ��������� ������  
   double Osc1,Osc2,Osc3,Osc4,Osc5,FastTrend,SlowTrend,OSMA,diff;
   double Momentum1[],Momentum2[],Momentum3[],Momentum4[],Momentum5[];
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
   ArraySetAsSeries(Momentum1,true);
   ArraySetAsSeries(Momentum2,true);
   ArraySetAsSeries(Momentum3,true);
   ArraySetAsSeries(Momentum4,true);
   ArraySetAsSeries(Momentum5,true);
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(Momentum1_Handle,0,0,to_copy,Momentum1)<=0) return(0);
   if(CopyBuffer(Momentum2_Handle,0,0,to_copy,Momentum2)<=0) return(0);
   if(CopyBuffer(Momentum3_Handle,0,0,to_copy,Momentum3)<=0) return(0);
   if(CopyBuffer(Momentum4_Handle,0,0,to_copy,Momentum4)<=0) return(0);
   if(CopyBuffer(Momentum5_Handle,0,0,to_copy,Momentum5)<=0) return(0);
//---- ������ ���������� ������ limit ��� ����� ��������� ����� � ��������� ������������� ����������
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      bar=limit+1;
      Osc1 = Factor1 * Momentum1[bar];
      Osc2 = Factor2 * Momentum2[bar];
      Osc3 = Factor2 * Momentum3[bar];
      Osc4 = Factor4 * Momentum4[bar];
      Osc5 = Factor5 * Momentum5[bar];

      FastTrend=Osc1+Osc2+Osc3+Osc4+Osc5;
      SlowTrend1=FastTrend/smoothing1;
      OSMA1=(FastTrend-SlowTrend1)/smoothing2;
     }
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Osc1 = Factor1 * Momentum1[bar];
      Osc2 = Factor2 * Momentum2[bar];
      Osc3 = Factor2 * Momentum3[bar];
      Osc4 = Factor4 * Momentum4[bar];
      Osc5 = Factor5 * Momentum5[bar];
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
