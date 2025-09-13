//+------------------------------------------------------------------+
//|                                          ColorMETRO_DeMarker.mq5 | 
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
//+--------------------------------------------------+
//| ��������� ��������� ������  StepDeMarker cloud   |
//+--------------------------------------------------+
//---- ��������� ���������� 1 � ���� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����� ���������� ������������ ����� DodgerBlue,Red
#property indicator_color1  clrDodgerBlue,clrRed
//---- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "StepDeMarker cloud"
//+--------------------------------------------------+
//| ��������� ��������� ���������� DeMarker          |
//+--------------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ���� SlateBlue
#property indicator_color2  clrSlateBlue
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//---- ����������� ����� ����������
#property indicator_label2  "DeMarker"
//+--------------------------------------------------+
//| ��������� ����������� �������������� �������     |
//+--------------------------------------------------+
#property indicator_level1  70
#property indicator_level2  50
#property indicator_level3  30
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+--------------------------------------------------+
//| ����������� �������� ������ ���� ����������      |
//+--------------------------------------------------+
#property indicator_minimum   0
#property indicator_maximum 100
//+--------------------------------------------------+
//| ������� ��������� ����������                     |
//+--------------------------------------------------+
input uint PeriodDeMarker=7;                              // ������ ����������
input int StepSizeFast=5;                                 // ������� ���
input int StepSizeSlow=15;                                // ��������� ���
input int Shift=0;                                        // ����� ���������� �� ����������� � ����� 
//+--------------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double Line1Buffer[];
double Line2Buffer[];
double Line3Buffer[];
//---- ���������� ������������� ���������� ��� ������� �����������
int DeMarker_Handle;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(PeriodDeMarker);
//---- ��������� ������ ���������� DeMarker
   DeMarker_Handle=iDeMarker(NULL,0,PeriodDeMarker);
   if(DeMarker_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� DeMarker");
      return(INIT_FAILED);
     }
//---- ����������� ������������� ������� Line1Buffer[] � ������������ �����
   SetIndexBuffer(0,Line1Buffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(Line1Buffer,true);

//---- ����������� ������������� ������� Line2Buffer[] � ������������ �����
   SetIndexBuffer(1,Line2Buffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(Line2Buffer,true);

//---- ����������� ������������� ������� Line3Buffer[] � ������������ �����
   SetIndexBuffer(2,Line3Buffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� ����������� �� Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 3 �� min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(Line3Buffer,true);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"METRO_DeMarker(",PeriodDeMarker,", ",StepSizeFast,", ",StepSizeSlow,", ",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
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
   if(BarsCalculated(DeMarker_Handle)<rates_total || rates_total<min_rates_total) return(0);
//---- ���������� ��������� ���������� 
   int limit,to_copy,bar,ftrend,strend;
   double fmin0,fmax0,smin0,smax0,DeMarker0;
   static double fmax1,fmin1,smin1,smax1;
   static int ftrend_,strend_;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1; // ��������� ����� ��� ������� ���� �����
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
   if(CopyBuffer(DeMarker_Handle,0,0,to_copy,Line3Buffer)<=0) return(0);
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
      DeMarker0=Line3Buffer[bar]=Line3Buffer[bar]*100;
      //----
      fmax0=DeMarker0+2*StepSizeFast;
      fmin0=DeMarker0-2*StepSizeFast;
      //----
      if(DeMarker0>fmax1)  ftrend=+1;
      if(DeMarker0<fmin1)  ftrend=-1;
      //----
      if(ftrend>0 && fmin0<fmin1) fmin0=fmin1;
      if(ftrend<0 && fmax0>fmax1) fmax0=fmax1;
      //----
      smax0=DeMarker0+2*StepSizeSlow;
      smin0=DeMarker0-2*StepSizeSlow;
      //----
      if(DeMarker0>smax1)  strend=+1;
      if(DeMarker0<smin1)  strend=-1;
      //----
      if(strend>0 && smin0<smin1) smin0=smin1;
      if(strend<0 && smax0>smax1) smax0=smax1;
      //----
      if(ftrend>0) Line1Buffer[bar]=fmin0+StepSizeFast;
      if(ftrend<0) Line1Buffer[bar]=fmax0-StepSizeFast;
      if(strend>0) Line2Buffer[bar]=smin0+StepSizeSlow;
      if(strend<0) Line2Buffer[bar]=smax0-StepSizeSlow;
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
