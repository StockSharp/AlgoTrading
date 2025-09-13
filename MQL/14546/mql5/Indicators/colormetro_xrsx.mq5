//+------------------------------------------------------------------+
//|                                              ColorMETRO_XRSX.mq5 | 
//|                           Copyright � 2005, TrendLaboratory Ltd. |
//|                                       E-mail: igorad2004@list.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2005, TrendLaboratory Ltd."
#property link      "E-mail: igorad2004@list.ru"
#property description "METRO"
//---- ����� ������ ����������
#property version   "1.10"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 3
#property indicator_buffers 3 
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� StepXRSX      |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ������ ������ ���������� ������������
#property indicator_color1  clrLime,clrDeepPink
//---- ����������� ����� ����������
#property indicator_label1  "StepXRSX Cloud"
//+----------------------------------------------+
//| ��������� ��������� ���������� XRSX          |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ���� Blue
#property indicator_color2  clrBlue
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 3
#property indicator_width2  3
//---- ����������� ����� ����������
#property indicator_label2  "XRSX"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1  70
#property indicator_level2  50
#property indicator_level3  30
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA UPXRSX,DNXRSX,XSIGN;
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
//| ���������� ������������                      |
//+----------------------------------------------+
/*enum Smooth_Method - ��������� � ����� SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method DSmoothMethod=MODE_JJMA; // ����� ���������� ����
input int DPeriod=15;  // ������ �������
input int DPhase=100;  // �������� ���������� �������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input int StepSizeFast=5;                             // ������� ���
input int StepSizeSlow=15;                            // ��������� ���
input Applied_price_ IPC=PRICE_CLOSE; // ������� ���������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double Line1Buffer[];
double Line2Buffer[];
double Line3Buffer[];
//---- ���������� ������������� ���������� ��� ������� �����������
int XRSX_Handle;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=GetStartBars(DSmoothMethod,DPeriod,DPhase)+1;
//---- ��������� ������ ���������� XRSX
   XRSX_Handle=iCustom(NULL,0,"XRSX",DSmoothMethod,DPeriod,DPhase,1,1,1,IPC,0,1);
   if(XRSX_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� XRSX");
      return(INIT_FAILED);
     }
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,Line2Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(Line2Buffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,Line3Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(Line3Buffer,true);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,Line1Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(Line1Buffer,true);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);

//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"ColorMETRO_XRSX");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(XRSX_Handle)<rates_total || rates_total<min_rates_total) return(0);
//---- ���������� ��������� ���������� 
   int limit,to_copy,bar,ftrend,strend;
   double fmin0,fmax0,smin0,smax0,XRSX0,XRSX[];
   static double fmax1,fmin1,smin1,smax1;
   static int ftrend_,strend_;
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(XRSX,true);
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1; // ��������� ����� ��� ������� ���� �����
      //----
      fmin1=+999999;
      fmax1=-999999;
      smin1=+999999;
      smax1=-999999;
      ftrend_=0;
      strend_=0;
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//----
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � ������
   if(CopyBuffer(XRSX_Handle,0,0,to_copy,XRSX)<=0) return(0);
//---- ��������������� �������� ����������
   ftrend = ftrend_;
   strend = strend_;
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==0)
        {
         ftrend_=ftrend;
         strend_=strend;
        }
      //----
      XRSX0=(XRSX[bar]+100)/2.0;
      //----
      fmax0=XRSX0+2*StepSizeFast;
      fmin0=XRSX0-2*StepSizeFast;
      //----
      if(XRSX0>fmax1)  ftrend=+1;
      if(XRSX0<fmin1)  ftrend=-1;
      //----
      if(ftrend>0 && fmin0<fmin1) fmin0=fmin1;
      if(ftrend<0 && fmax0>fmax1) fmax0=fmax1;
      //----
      smax0=XRSX0+2*StepSizeSlow;
      smin0=XRSX0-2*StepSizeSlow;
      //----
      if(XRSX0>smax1)  strend=+1;
      if(XRSX0<smin1)  strend=-1;
      //----
      if(strend>0 && smin0<smin1) smin0=smin1;
      if(strend<0 && smax0>smax1) smax0=smax1;
      //----
      Line1Buffer[bar]=XRSX0;
      //----
      if(ftrend>0) Line2Buffer[bar]=fmin0+StepSizeFast;
      if(ftrend<0) Line2Buffer[bar]=fmax0-StepSizeFast;
      if(strend>0) Line3Buffer[bar]=smin0+StepSizeSlow;
      if(strend<0) Line3Buffer[bar]=smax0-StepSizeSlow;
      //----
      if(bar>0)
        {
         fmin1=fmin0;
         fmax1=fmax0;
         smin1=smin0;
         smax1=smax0;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
