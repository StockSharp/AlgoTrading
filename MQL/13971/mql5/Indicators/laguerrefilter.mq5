//+------------------------------------------------------------------+
//|                                              Laguerre Filter.mq5 |
//|                                                                  |
//| Laguerre Filter                                                  |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Coded by Witold Wozniak"
//---- ��������� ����������
#property link      "www.mqlsoft.com"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� 1             |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ������� ����
#property indicator_color1  Red
//---- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "Laguerre Filter"
//+----------------------------------------------+
//| ��������� ��������� ���������� 2             |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ����� ����
#property indicator_color2  Blue
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//---- ����������� ����� ����������
#property indicator_label2  "FIR"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input double Gamma=0.7; // ����������� ����������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ��������� �������
double LaguerreBuffer[],FirBuffer[];
//+------------------------------------------------------------------+
//| ��������� �������� �� ������� ���������                          |
//+------------------------------------------------------------------+   
double Get_Price(const double  &High[],const double  &Low[],int bar)
  {
//----
   return((High[bar]+Low[bar])/2);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=4;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,LaguerreBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,FirBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Laguerre Filter(",DoubleToString(Gamma,4),", ",Shift,")");
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
   if(rates_total<min_rates_total) return(0);
//---- ���������� ��������� ���������� 
   int first,bar;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A;
//---- ���������� ����������� ���������� ��� �������� �������������� �������� ������������
   static double L0_,L1_,L2_,L3_,L0A_,L1A_,L2A_,L3A_;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- ��������������� �������� ����������
   L0 = L0_;
   L1 = L1_;
   L2 = L2_;
   L3 = L3_;
   L0A = L0A_;
   L1A = L1A_;
   L2A = L2A_;
   L3A = L3A_;
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==rates_total-1)
        {
         L0_ = L0;
         L1_ = L1;
         L2_ = L2;
         L3_ = L3;
         L0A_ = L0A;
         L1A_ = L1A;
         L2A_ = L2A;
         L3A_ = L3A;
        }
      //----
      L0A = L0;
      L1A = L1;
      L2A = L2;
      L3A = L3;
      //----
      L0 = (1 - Gamma) * Get_Price(high, low, bar) + Gamma * L0A;
      L1 = - Gamma * L0 + L0A + Gamma * L1A;
      L2 = - Gamma * L1 + L1A + Gamma * L2A;
      L3 = - Gamma * L2 + L2A + Gamma * L3A;
      //----
      if(bar>min_rates_total)
        {
         LaguerreBuffer[bar]=(L0+2.0*L1+2.0*L2+L3)/6.0;
         FirBuffer[bar]=
                        (
                        1.0*Get_Price(high,low,bar-0)
                        +2.0*Get_Price(high,low,bar-1)
                        +2.0*Get_Price(high,low,bar-2)
                        +1.0*Get_Price(high,low,bar-3)
                        )
                        /6.0;
        }
      else
        {
         L0_=Get_Price(high,low,bar);
         L1_ = L0_;
         L2_ = L0_;
         L3_ = L0_;
         LaguerreBuffer[bar]=L0_;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
