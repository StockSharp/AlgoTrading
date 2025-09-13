//+------------------------------------------------------------------+
//|                                          RangeExpansionIndex.mq5 |
//|                                  Copyright � 2010, EarnForex.com |
//|                                        http://www.earnforex.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2010, EarnForex.com"
#property link      "http://www.earnforex.com"
//--- ����� ������ ����������
#property version   "1.0"
#property description "Calculates Tom DeMark's Range Expansion Index."
#property description "Going above 60 and then dropping below 60 signals price weakness."
#property description "Going below -60 and the rising above -60 signals price strength."
#property description "For more info see The New Science of Technical Analysis."
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ���������� ������������ ������� 2
#property indicator_buffers 2 
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//--- ��������� ���������� � ���� ����������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//--- � �������� ������� ����������� ������������ ���� ������
#property indicator_color1 clrGray,clrLime,clrBlue,clrRed,clrMagenta
//--- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width1 2
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 +60
#property indicator_level2 -60
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
//--- ������� ��������� ����������
input int REI_Period=8;  // ������ ����������
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double ExtBuffer[],ColorExtBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Calculate the Conditional Value                                  |
//+------------------------------------------------------------------+
double SubValue(const int i,const double &High[],const double &Low[],const double &Close[])
  {
   int num_zero1,num_zero2;
//---
   double diff1 = High[i] - High[i - 2];
   double diff2 = Low[i] - Low[i - 2];
//---
   if((High[i-2]<Close[i-7]) && (High[i-2]<Close[i-8]) && (High[i]<High[i-5]) && (High[i]<High[i-6]))
      num_zero1=0;
   else
      num_zero1=1;
//---
   if((Low[i-2]>Close[i-7]) && (Low[i-2]>Close[i-8]) && (Low[i]>Low[i-5]) && (Low[i]>Low[i-6]))
      num_zero2=0;
   else
      num_zero2=1;
//---
   return(num_zero1*num_zero2 *(diff1+diff2));
  }
//+------------------------------------------------------------------+
//| Calculate the Absolute Value                                     |
//+------------------------------------------------------------------+
double AbsValue(const int i,const double &High[],const double &Low[])
  {
   double diff1 = MathAbs(High[i] - High[i - 2]);
   double diff2 = MathAbs(Low[i] - Low[i - 2]);
//---
   return(diff1+diff2);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=REI_Period+8;
//--- ����������� ������������� ������� ExtBuffer � ������������ �����
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� MAPeriod
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Range Expansion Index(",REI_Period,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
                const double& low[],      // ������� ������ ���������  ���� ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total)return(0);
//--- ���������� ��������� ���������� 
   int first1,first2,bar;
   double SubValueSum,AbsValueSum;
//--- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first1=min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      first2=first1+1;
     }
   else
     {
      first1=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
      first2=first1; // ��������� ����� ��� ������� ����� �����
     }
//--- �������� ���� ������� ����������
   for(bar=first1; bar<rates_total; bar++)
     {
      SubValueSum=0;
      AbsValueSum=0;
      //---
      for(int iii=0; iii<REI_Period; iii++)
        {
         SubValueSum += SubValue(bar - iii, high, low, close);
         AbsValueSum += AbsValue(bar - iii, high, low);
        }
      //---
      if(AbsValueSum!=0) ExtBuffer[bar]=SubValueSum/AbsValueSum*100;
      else ExtBuffer[bar]=0;
     }
//--- �������� ���� ��������� ����������
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorExtBuffer[bar]=0;
      //---
      if(ExtBuffer[bar]>0)
        {
         if(ExtBuffer[bar]>ExtBuffer[bar-1]) ColorExtBuffer[bar]=1;
         if(ExtBuffer[bar]<ExtBuffer[bar-1]) ColorExtBuffer[bar]=2;
        }
      //---
      if(ExtBuffer[bar]<0)
        {
         if(ExtBuffer[bar]<ExtBuffer[bar-1]) ColorExtBuffer[bar]=3;
         if(ExtBuffer[bar]>ExtBuffer[bar-1]) ColorExtBuffer[bar]=4;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
