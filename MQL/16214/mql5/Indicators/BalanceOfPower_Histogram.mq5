//+------------------------------------------------------------------+
//|                                     BalanceOfPower_Histogram.mq5 |
//|                                         Copyright � 2012, RoboFx |
//|                                            http://www.robofx.org |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2012, RoboFx"
//---- ������ �� ���� ������
#property link      "http://www.robofx.org"
#property description "��������� Balance of Power (BOP), ��������� ������ ��������, �������� ���� ����� � �������� �������� ����������� ��� � ������ ������� ���� �� ����������� ������"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ����������� ������������
#property indicator_color1 clrLime,clrDeepSkyBlue,clrTeal,clrBlue,clrPurple,clrMediumVioletRed,clrMagenta,clrRed
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����� ����������
#property indicator_label1  "BalanceOfPower_Histogram"
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
/*enum Smooth_Method - ������������ ��������� � ����� SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method XMethod=MODE_T3;              // ����� ����������
input uint XLength=15;                            // ������� ����������          
input int XPhase=15;                              // �������� ����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input int HighLevel=+20;                          // ������� ���������������
input int LowLevel=-20;                           // ������� ���������������
input int Shift=0;                                // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � ���������� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=GetStartBars(XMethod,XLength,XPhase)+1;

//---- ����������� ������������� ������� SignBuffer � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"BalanceOfPower_Histogram");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ����������  �������������� ������� ���������� 3  
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,0.0);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- � �������� ������ ����� �������������� ������� ������������ 
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrRed);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_SOLID);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//----
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
   if(rates_total<min_rates_total) return(0);

//---- ���������� ��������� ���������� 
   int first;
   double diff,bop;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(int bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      diff=high[bar]-low[bar];
      if(!diff) diff=_Point;
      bop=(close[bar]-open[bar])/diff;
      IndBuffer[bar]=100*XMA1.XMASeries(0,prev_calculated,rates_total,XMethod,XPhase,XLength,bop,bar,false);
     }
//---- 
   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;
//---- �������� ���� ��������� ����������
   for(int bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=10;
      bop=IndBuffer[bar];
      diff=IndBuffer[bar]-IndBuffer[bar-1];

      if(bop>0)
        {
         if(diff>0.0)
           {
            if(bop>HighLevel) clr=0;
            else clr=2;
           }
         if(diff<0.0)
           {
            if(bop>HighLevel) clr=1;
            else clr=3;
           }
        }

      if(bop<0)
        {
         if(diff<0.0)
           {
            if(bop<LowLevel) clr=7;
            else clr=5;
           }
         if(diff>0.0)
           {
            if(bop<LowLevel) clr=6;
            else clr=4;
           }
        }
      ColorIndBuffer[bar]=clr;
     }
//----              
   return(rates_total);
  }
//+------------------------------------------------------------------+
