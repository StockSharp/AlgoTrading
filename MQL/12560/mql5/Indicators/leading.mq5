//+------------------------------------------------------------------+
//|                                                      Leading.mq5 |
//|                                                                  |
//| Leading                                                          |
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
//--- ��������� ����������
#property link      "www.mqlsoft.com"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//--- � �������� ����� ����� ����� ���������� ����������� ����� ����
#property indicator_color1  clrBlue
//--- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//--- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//--- ����������� ����� ����� ����������
#property indicator_label1  "Lead"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 2            |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//--- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color2  clrRed
//--- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//--- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//--- ����������� ��������� ����� ����������
#property indicator_label2  "EMA"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input double Alpha1 = 0.25;//1 ����������� ����������
input double Alpha2 = 0.33;//2 ����������� ���������� 
input int Shift=0; // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//--- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//--- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double NetLeadBuffer[],EMABuffer[];
//+------------------------------------------------------------------+
//|  ��������� �������� �� ������� ���������                         |
//+------------------------------------------------------------------+   
double Get_Price(const double  &High[],const double  &Low[],int bar)
// Get_Price(high, low, bar)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
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
   min_rates_total=2;

//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,NetLeadBuffer,INDICATOR_DATA);
//--- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,EMABuffer,INDICATOR_DATA);
//--- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,
                     "Leading(",DoubleToString(Alpha1,4),", ",DoubleToString(Alpha2,4),", ",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
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
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//--- ���������� ��������� ���������� 
   int first,bar;
   double Lead;

//--- ���������� ����������� ���������� ��� �������� �������������� �������� ������������
   static double Lead_;

//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
    {
      first=0; // ��������� ����� ��� ������� ���� �����
    }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//--- ��������������� �������� ����������
   Lead=Lead_;

//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      //--- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==rates_total-1)
         Lead_=Lead;

      if(bar>min_rates_total)
        {
         Lead=2.0*Get_Price(high,low,bar)+(Alpha1-2.0)*Get_Price(high,low,bar-1)+(1.0-Alpha1)*Lead;
         NetLeadBuffer[bar] = Alpha2 * Lead + (1 - Alpha2) * NetLeadBuffer[bar-1];
         EMABuffer[bar]=0.5 * Get_Price(high,low,bar) + 0.5 * EMABuffer[bar-1];
        }
      else
        {
         Lead=Get_Price(high,low,bar);
         NetLeadBuffer[bar]=Lead;
         EMABuffer[bar]=Lead;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
