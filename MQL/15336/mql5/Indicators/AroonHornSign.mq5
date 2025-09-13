//+------------------------------------------------------------------+
//|                                                AroonHornSign.mq5 |
//|                                        Copyright � 2011, tonyc2a | 
//|                                         mailto:tonyc2a@yahoo.com | 
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2011, tonyc2a"
//---- ������ �� ���� ������
#property link "mailto:tonyc2a@yahoo.com"
//---- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0       // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ��������� ����� ���������� ����������� Crimson ����
#property indicator_color1  clrCrimson
//--- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//--- ����������� ����� ����� ����������
#property indicator_label1  "AroonHornSign Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ����� ����� ���������� ����������� LimeGreen ����
#property indicator_color2  clrLimeGreen
//--- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//--- ����������� ��������� ����� ����������
#property indicator_label2 "AroonHornSign Buy"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint AroonPeriod= 9; // ������ ���������� 
input int AroonShift = 0; // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double BullsAroonBuffer[];
double BearsAroonBuffer[];
//---- ���������� ������������� ���������� ��� ������� �����������
int ATR_Handle;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������   
   int ATR_Period=10;
   min_rates_total=int(MathMax(AroonPeriod,ATR_Period));
//---- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     } 
//---- ����������� ������������� ������� BullsAroonBuffer � ������������ �����
   SetIndexBuffer(0,BearsAroonBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� AroonShift
   PlotIndexSetInteger(0,PLOT_SHIFT,AroonShift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� AroonPeriod
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,AroonPeriod);

//---- ����������� ������������� ������� BearsAroonBuffer � ������������ �����
   SetIndexBuffer(1,BullsAroonBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� AroonShift
   PlotIndexSetInteger(1,PLOT_SHIFT,AroonShift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� AroonPeriod
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,AroonPeriod);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"AroonHornSign(",AroonPeriod,", ",AroonShift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- ���������� ��������� ���������� 
   int first,bar,trend;
   static int trend_prev;
   double BULLS,BEARS,ATR[1];

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      trend_prev=0;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
   

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int barx=rates_total-bar-1;
      //---- ���������� ������������ ��������
      BULLS=NormalizeDouble(100-(ArrayMaximum(high,barx,AroonPeriod)-barx+0.5)*100/AroonPeriod,0);
      BEARS=NormalizeDouble(100-(ArrayMinimum(low,barx,AroonPeriod)-barx+0.5)*100/AroonPeriod,0);
      BullsAroonBuffer[bar]=0;
      BearsAroonBuffer[bar]=0;
      trend=trend_prev;
      if(BULLS>BEARS && BULLS>=50) trend=+1;
      if(BULLS<BEARS && BEARS>=50) trend=-1;
      if(trend_prev<0 && trend>0)
       {
         //---- �������� ����� ����������� ������ � ������
         if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
         BullsAroonBuffer[bar]=low[barx]-ATR[0]*3/8;
       }
      if(trend_prev>0 && trend<0)
       {         
         //---- �������� ����� ����������� ������ � ������
         if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
         BearsAroonBuffer[bar]=high[barx]+ATR[0]*3/8;
       }
       
      if(bar<rates_total-1) trend_prev=trend;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
