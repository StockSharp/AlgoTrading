//+------------------------------------------------------------------+ 
//|                                                  CCI_Woodies.mq5 | 
//|                                        Copyright � 2013, Woodies | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
//---- ��������� ����������
#property copyright "Copyright � 2013, Woodies"
//---- ��������� ����������
#property link      ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ������ ������
#property indicator_color1  clrLime,clrPlum
//---- ����������� ����� ����������
#property indicator_label1  "CCI_Woodies"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint FastPeriod=6;                              // ������ �������� CCI ����������
input ENUM_APPLIED_PRICE FastPrice=PRICE_MEDIAN;      // ������� ��������� �������� CCI ����������
input uint SlowPeriod=14;                             // ������ ���������� CCI ����������
input ENUM_APPLIED_PRICE SlowPrice=PRICE_MEDIAN;      // ������� ��������� ���������� CCI ����������
input int Shift=0;                                    // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double FastBuffer[];
double SlowBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ����� ���������� ��� �������� ������� �����������
int Fast_Handle,Slow_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(FastPeriod,SlowPeriod));
   
//--- ��������� ������ ���������� Fast iCCI
   Fast_Handle=iCCI(Symbol(),PERIOD_CURRENT,FastPeriod,FastPrice);
   if(Fast_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Fast iCCI");
      return(INIT_FAILED);
     }
     
//--- ��������� ������ ���������� Slow iCCI
   Slow_Handle=iCCI(Symbol(),PERIOD_CURRENT,SlowPeriod,SlowPrice);
   if(Slow_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Slow iCCI");
      return(INIT_FAILED);
     }
    
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,FastBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,SlowBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Instantaneous Trendline(",FastPeriod,", ",SlowPeriod,", ",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- ����������  �������������� ������� ���������� 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,+100);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,0);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,-100);
//---- � �������� ������ ����� �������������� ������� ������������ �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrMagenta);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASH);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
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
   if(rates_total<min_rates_total
   || BarsCalculated(Fast_Handle)<rates_total
   || BarsCalculated(Slow_Handle)<rates_total) return(RESET);

//---- ���������� ��������� ���������� 
   int to_copy;
   
//---- ������� ������������ ���������� ���������� ������
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      to_copy=rates_total; // ��������� ����� ��� ������� ���� �����
     }
   else to_copy=rates_total-prev_calculated+1; // ��������� ����� ��� ������� ����� �����

//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(Fast_Handle,0,0,to_copy,FastBuffer)<=0) return(RESET);
   if(CopyBuffer(Slow_Handle,0,0,to_copy,SlowBuffer)<=0) return(RESET);
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
