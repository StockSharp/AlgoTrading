//+------------------------------------------------------------------+ 
//|                                           DiNapoliStochastic.mq5 | 
//|                                      Copyright � 2010, LenIFCHIK |
//|                                                                  |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2010, LenIFCHIK"
#property link      ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� Stochastic    |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� �������� ����� ���������� ����������� ���� DarkOrange
#property indicator_color1  clrDarkOrange
//---- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//---- ����������� ����� ����� ����������
#property indicator_label1  "Stochastic"
//+----------------------------------------------+
//| ��������� ��������� ���������� Signal        |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ���������� ����� ���������� ����������� ���� BlueViolet
#property indicator_color2  clrBlueViolet
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//---- ����������� ����� ����� ����������
#property indicator_label2  "Signal"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level3 70.0
#property indicator_level2 50.0
#property indicator_level1 30.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0       // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint FastK=8;    // ������� ������ %K
input uint SlowK=3;    // ��������� ������ %K
input uint SlowD=3;    // ��������� ������ %D
input int Shift=0;     // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double StoBuffer[];
double SigBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(FastK);
//---- ����������� ������������� ������� StoBuffer[] � ������������ �����
   SetIndexBuffer(0,StoBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(StoBuffer,true);
//---- ����������� ������������� ������� SignalBuffer[] � ������������ �����
   SetIndexBuffer(1,SigBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SigBuffer,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"DiNapoliStochastic(",FastK,", ",SlowK,", ",SlowD,", ",Shift,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//----
   return(0);
//----
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
   if(rates_total<min_rates_total) return(RESET);
//---- ���������� ���������� � ��������� ������  
   double HH,LL,Range,Res;
//---- ���������� ������������� ����������
   int limit;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      StoBuffer[limit+1]=50.0;
      SigBuffer[limit+1]=50.0;
     }
   else limit=rates_total-prev_calculated;  // ��������� ����� ��� ������� ������ ����� �����
//---- ���������� ��������� � �������� ��� � ����������
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- �������� ���� ������� ����������
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      HH=high[ArrayMaximum(high,bar,FastK)];
      LL=low [ArrayMinimum(low, bar,FastK)];
      Range=MathMax(HH-LL,1*_Point);
      Res=100*(close[bar]-LL)/Range;
      StoBuffer[bar]=StoBuffer[bar+1]+(Res-StoBuffer[bar+1])/SlowK;            //������ �������������� �����
      SigBuffer[bar]=SigBuffer[bar+1]+(StoBuffer[bar]-SigBuffer[bar+1])/SlowD; //������ ���������� �����
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
