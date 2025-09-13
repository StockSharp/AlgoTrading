//+------------------------------------------------------------------+
//|                                    Instantaneous_TrendFilter.mq5 |
//|                         Copyright � 2006, Luis Guilherme Damiani |
//|                                      http://www.damianifx.com.br |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2006, Luis Guilherme Damiani"
//---- ��������� ����������
#property link      "http://www.damianifx.com.br"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � �������� ����
#property indicator_chart_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ���������� ITrend        |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ������ ���������� ������������
#property indicator_color1  clrMagenta,clrBlue
//---- ����������� ����� ����������
#property indicator_label1  "Instantaneous_TrendFilter"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input double Alpha=0.07; // ����������� ����������
input int Shift=0;       // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ITrendBuffer[];
double TriggerBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ���������� ����������
double K0,K1,K2,K3,K4;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=4;
//---- ������������� ����������
   double A2=Alpha*Alpha;
   K0=Alpha-A2/4.0;
   K1=0.5*A2;
   K2=Alpha-0.75*A2;
   K3=2.0 *(1.0 - Alpha);
   K4=MathPow((1.0 - Alpha),2);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,ITrendBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Instantaneous_TrendFilter(",Alpha,", ",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
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
   double price0,price1,price2;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=min_rates_total+begin; // ��������� ����� ��� ������� ���� �����
      //---- ������������� ������ ������ ������� ��������� ����������
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
      //----
      for(bar=0; bar<first && !IsStopped(); bar++) ITrendBuffer[bar]=price[bar];
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price0=price[bar];
      price1=price[bar-1];
      price2=price[bar-2];
      //----
      if(bar<min_rates_total) ITrendBuffer[bar]=(price0+2.0*price1+price2)/4.0;
      else ITrendBuffer[bar]=K0*price0+K1*price1-K2*price2+K3*ITrendBuffer[bar-1]-K4*ITrendBuffer[bar-2];
      //----
      TriggerBuffer[bar]=2.0*ITrendBuffer[bar]-ITrendBuffer[bar-2];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
