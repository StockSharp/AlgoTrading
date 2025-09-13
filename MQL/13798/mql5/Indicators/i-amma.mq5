//+------------------------------------------------------------------+ 
//|                                                       i-AMMA.mq5 | 
//|                                          Copyright � 2007, RickD |
//|                                                   www.e2e-fx.net |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2007, RickD"
//---- ������ �� ���� ������
#property link      "www.e2e-fx.net"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� Orange ����
#property indicator_color1 clrOrange
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "i-AMMA"
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint MA_Period=25; // ������� �����������
input int Shift=0; // ����� ���������� �� ����������� � �����
input int PriceShift=0; // ����� ���������� �� ��������� � �������
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double IndBuffer[];
//---- 
double dPriceShift;
//---- ���������� ������������� ���������� ������ ������� ������
int  min_rates_total;
//+------------------------------------------------------------------+    
//| i-AMMA indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=2;
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"i-AMMA(",MA_Period,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| i-AMMA iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const int begin,          // ����� ������ ������������ ������� �����
                const double &price[])    // ������� ������ ��� ������� ����������
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total+begin) return(0);
//---- ���������� ��������� ����������
   int first,bar;
   double AMMA;
//----
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      first=min_rates_total+begin; // ��������� ����� ��� ������� ���� �����
      IndBuffer[first-1]=price[first-1];
      //---- ������������� ������ ������ ������� ��������� ����������
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      AMMA=((MA_Period-1)*(IndBuffer[bar-1]-dPriceShift)+price[bar])/MA_Period;
      IndBuffer[bar]=AMMA+dPriceShift;
     }
//----
   return(rates_total);
  }
//+------------------------------------------------------------------+
