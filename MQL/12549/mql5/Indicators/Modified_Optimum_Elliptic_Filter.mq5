//+------------------------------------------------------------------+
//|                             Modified_Optimum_Elliptic_Filter.mq5 |
//|                                                                  |
//| Modified Optimum Elliptic Filter                                 |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Coded by Witold Wozniak"
//--- ������ �� ���� ������
#property link      "www.mqlsoft.com"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � �������� ����
#property indicator_chart_window
//--- ��� ������� � ��������� ���������� ����������� ���� �����
#property indicator_buffers 1
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//--- � �������� ����� ����� ���������� ����������� ������� ����
#property indicator_color1  Magenta
//--- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width1  2
//--- ����������� ����� ����������
#property indicator_label1  "Modified Optimum Elliptic Filter"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int Shift=0; // ����� ������� �� ����������� � ����� 
//+----------------------------------------------+
//--- ���������� ������������� �������, ������� � ����������
//--- ����� ����������� � �������� ������������� ������
double ExtLineBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//--- ���������� ���������� ����������
double coef1,coef2,coef3,coef4;
//+------------------------------------------------------------------+
//| ��������� �������� �� ������� ���������                          |
//+------------------------------------------------------------------+   
double Get_Price(const double  &High[],const double  &Low[],int bar)
  {
//---
   return((High[bar]+Low[bar])/2);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=4;
//--- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//--- ������������� ������ ����� �� ����������� �� FATLShift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Modified Optimum Elliptic Filter(",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---
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
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);
//--- ���������� ��������� ���������� 
   int first,bar;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0;                   // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      //--- ������� ��� ���������� �������
      if(bar>min_rates_total) ExtLineBuffer[bar]=
         0.13785*(2*Get_Price(high,low,bar)-Get_Price(high,low,bar-1))
         +0.0007*(2*Get_Price(high,low,bar-1)-Get_Price(high,low,bar-2))
         + 0.13785*(2*Get_Price(high,low,bar-2) - Get_Price(high,low,bar-3))
         + 1.2103 *ExtLineBuffer[bar-1] - 0.4867*ExtLineBuffer[bar-2];
      else ExtLineBuffer[bar]=Get_Price(high,low,bar);
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
