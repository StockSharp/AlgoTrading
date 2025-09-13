//+------------------------------------------------------------------+
//|                                             JBrainTrend1Stop.mq5 |
//|                               Copyright � 2005, BrainTrading Inc |
//|                                      http://www.braintrading.com |
//+------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2005, BrainTrading Inc."
//--- ������ �� ���� ������
#property link      "http://www.braintrading.com/"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ������ ������
#property indicator_buffers 4
//--- ������������ ����� ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ��������� ����� ���������� ����������� Orange ����
#property indicator_color1  clrOrange
//--- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//--- ����������� ����� ����� ����������
#property indicator_label1  "JBrain1 Sell"
//+----------------------------------------------+
//| ��������� ��������� ������ ����������        |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ����� ����� ���������� ����������� SpringGreen ����
#property indicator_color2  clrSpringGreen
//--- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//--- ����������� ��������� ����� ����������
#property indicator_label2 "JBrain1 Buy"
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//--- ��������� ���������� 3 � ���� �������
#property indicator_type3   DRAW_LINE
//--- � �������� ����� ��������� ����� ���������� ����������� Orange ����
#property indicator_color3  clrOrange
//--- ������� ����� ���������� 3 ����� 1
#property indicator_width3  1
//--- ����� ���������� - ��������
#property indicator_style3 STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width3 2
//--- ����������� ����� ����� ����������
#property indicator_label3  "JBrain1 Sell"
//+----------------------------------------------+
//| ��������� ��������� ������ ����������        |
//+----------------------------------------------+
//--- ��������� ���������� 4 � ���� �������
#property indicator_type4   DRAW_LINE
//--- � �������� ����� ����� ����� ���������� ����������� SpringGreen ����
#property indicator_color4  clrSpringGreen
//--- ������� ����� ���������� 4 ����� 1
#property indicator_width4  1
//--- ����� ���������� - ��������
#property indicator_style4 STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width4 2
//--- ����������� ��������� ����� ����������
#property indicator_label4 "JBrain1 Buy"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int ATR_Period=7;                       // ������ ATR
input int STO_Period=9;                       // ������ ����������
input ENUM_MA_METHOD MA_Method = MODE_SMA;    // ����� ����������
input ENUM_STO_PRICE STO_Price = STO_LOWHIGH; // ����� ������� ��� 
input int Stop_dPeriod=3;                     // ���������� ������� ��� �����
input int Length_=7;                          // ������� JMA �����������
input int Phase_=100;                         // �������� JMA �����������
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double SellStopBuffer[];
double BuyStopBuffer[];
double SellStopBuffer_[];
double BuyStopBuffer_[];
//---
double d,s,r,R_;
int p,x1,x2,P_,min_rates_total;
//--- ���������� ����� ���������� ��� ������� �����������
int ATR_Handle,ATR1_Handle,STO_Handle,JH_Handle,JL_Handle,JC_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ������������� ���������� ���������� 
   d=2.3;
   s=1.5;
   x1 = 53;
   x2 = 47;
   min_rates_total=int(MathMax(MathMax(MathMax(ATR_Period,STO_Period),ATR_Period+Stop_dPeriod),30)+2);
//--- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� ATR
   ATR1_Handle=iATR(NULL,0,ATR_Period+Stop_dPeriod);
   if(ATR1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR1");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� Stochastic
   STO_Handle=iStochastic(NULL,0,STO_Period,STO_Period,1,MA_Method,STO_Price);
   if(STO_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Stochastic");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� JMA
   JL_Handle=iCustom(NULL,0,"JMA",Length_,Phase_,4,0,0);
   if(JL_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� JMA");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� JMA
   JC_Handle=iCustom(NULL,0,"JMA",Length_,Phase_,1,0,0);
   if(JC_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� JMA");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� JMA
   JH_Handle=iCustom(NULL,0,"JMA",Length_,Phase_,3,0,0);
   if(JH_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� JMA");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellStopBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,159);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellStopBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyStopBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyStopBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,SellStopBuffer_,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellStopBuffer_,true);
//--- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,BuyStopBuffer_,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyStopBuffer_,true);
//--- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0.0);
//--- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ��� ��� ���� ������ � ����� ��� �������
   string short_name="JBrainTrend1Stop";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(ATR1_Handle)<rates_total
      || BarsCalculated(STO_Handle)<rates_total
      || BarsCalculated(JH_Handle)<rates_total
      || BarsCalculated(JL_Handle)<rates_total
      || BarsCalculated(JC_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double range,range1,val1,val2,val3;
   double value2[],Range[],Range1[],JH[],JL[],JC[],value3,value4,value5;
//--- ������� ������������ ���������� ���������� ������
//--- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=rates_total-min_rates_total;   // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����    
   to_copy=limit+1;
//--- �������� ����� ����������� ������ � �������
   if(CopyBuffer(ATR_Handle,0,0,to_copy,Range)<=0) return(RESET);
   if(CopyBuffer(STO_Handle,0,0,to_copy,value2)<=0) return(RESET);
   if(CopyBuffer(ATR1_Handle,0,0,to_copy,Range1)<=0) return(RESET);
   if(CopyBuffer(JH_Handle,0,0,to_copy,JH)<=0) return(RESET);
   if(CopyBuffer(JL_Handle,0,0,to_copy,JL)<=0) return(RESET);
   if(CopyBuffer(JC_Handle,0,0,to_copy+2,JC)<=0) return(RESET);
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(Range,true);
   ArraySetAsSeries(Range1,true);
   ArraySetAsSeries(value2,true);
   ArraySetAsSeries(JH,true);
   ArraySetAsSeries(JL,true);
   ArraySetAsSeries(JC,true);
//--- ��������������� �������� ����������
   p=P_;
   r=R_;
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0; bar--)
     {
      //--- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==0)
        {
         P_=p;
         R_=r;
        }
      range=Range[bar]/d;
      range1=Range1[bar]*s;

      val1 = 0.0;
      val2 = 0.0;
      val3=MathAbs(NormalizeDouble(JC[bar],_Digits)-NormalizeDouble(JC[bar+2],_Digits));

      SellStopBuffer[bar]=0.0;
      BuyStopBuffer[bar]=0.0;
      SellStopBuffer_[bar]=0.0;
      BuyStopBuffer_[bar]=0.0;

      if(val3>range)
        {
         if(value2[bar]<x2 && p!=1)
           {
            value3=JH[bar]+range1/4;
            val1=value3;
            p = 1;
            r = val1;
            SellStopBuffer[bar]=val1;
            SellStopBuffer_[bar]=val1;
           }

         if(value2[bar]>x1 && p!=2)
           {
            value3=JL[bar]-range1/4;
            val2=value3;
            p = 2;
            r = val2;
            BuyStopBuffer[bar]=val2;
            BuyStopBuffer_[bar]=val2;
           }
        }

      value4 = JH[bar] + range1;
      value5 = JL[bar] - range1;

      if(val1==0 && val2==0)
        {
         if(p==1)
           {
            if(value4<r) r=value4;
            SellStopBuffer[bar]=r;
            SellStopBuffer_[bar]=r;
           }

         if(p==2)
           {
            if(value5>r) r=value5;
            BuyStopBuffer[bar]=r;
            BuyStopBuffer_[bar]=r;
           }
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
