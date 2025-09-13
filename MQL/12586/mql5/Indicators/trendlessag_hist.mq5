//+---------------------------------------------------------------------+ 
//|                                                TrendlessAG_Hist.mq5 | 
//|                                          Copyright � 2012, Barmaley | 
//|                                                                     |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2012, Barmaley"
//--- ������ �� ���� ������
#property link ""
#property description "���������� �������������� ������� � ������������ � ���������,"
#property description "����������� � ����� ��� �������� \"�������� � ����������� ������� ��������\"."
#property description "������������� ������������ ����������� ��������� ����������." 
//--- ����� ������ ����������
#property version   "1.01"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//--- ���������� ������������ ������� 2
#property indicator_buffers 2 
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//--- � �������� ������ �������������� ����������� ������������
#property indicator_color1 clrMagenta,clrDeepPink,clrGray,clrDodgerBlue,clrOliveDrab
//--- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width1 2
//--- ����������� ����� ����������
#property indicator_label1 "TrendlessAG_Hist"
//+----------------------------------------------+
//|  �������� ������� ����������                 |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+----------------------------------------------+
//| ���������� ������������                      |
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
//| ���������� ������������                      |
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
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 +100
#property indicator_level2 +80
#property indicator_level3 +60
#property indicator_level4  0
#property indicator_level5 -60
#property indicator_level6 -80
#property indicator_level7 -100
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method1=MODE_EMA;  // ����� ����������  � ������� �����������
input int XLength1=7;                      // ������ ���������� ������� � ������� �����������                 
input int XPhase1=15;                      // �������� ���������� ������� � ������� �����������,
//XPhase1: ��� JJMA ���������� � �������� -100 ... +100, ������ �� �������� ����������� �������� � ������� �����������
//XPhase1: ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE;      // ������� ���� ���������� ������� � ������� �����������
input uint PointsCount=600;                // ���������� ����� ��� ������� ����������. ������-���� ����������� ����� ��� ���������
input uint In100=90;                       // ������� % ����� ���������� ������ ������� � �������� +-100%
input Smooth_Method XMA_Method2=MODE_JJMA; // ����� ����������� ����������
input int XLength2=5;                      // ������� �����������                    
input int XPhase2=100;                     // �������� �����������
//XPhase2: ��� JJMA ���������� � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//XPhase2: ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
//+----------------------------------------------+
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2;
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
//--- ���������� ���������� ����������
int Count[],Start;
double Value[],Sort[];
//+------------------------------------------------------------------+
//| �������� ������� ������ ������ �������� � �������                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                          int Size)
  {
//---
   int numb,Max1,Max2;
   static int count=1;
//---
   Max2=Size;
   Max1=Max2-1;
//---
   count--;
   if(count<0) count=Max1;
//---
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//---
  }
//+------------------------------------------------------------------+    
//| TrendlessAG_Hist indicator initialization function               | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_1=XMA1.GetStartBars(XMA_Method1,XLength1,XPhase1);
   min_rates_2=int(PointsCount+min_rates_1);
   min_rates_total=min_rates_2+XMA1.GetStartBars(XMA_Method2,XLength2,XPhase2)+2;  
   Start=int(PointsCount*In100/100);
//--- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,PointsCount);
   ArrayResize(Value,PointsCount);
   ArrayResize(Sort,PointsCount);
//---
   ArrayInitialize(Count,0);
   ArrayInitialize(Value,0.0);
//--- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- ����������� ������������� ������� � �������� ��������� �����
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"TrendlessAG_Hist");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| TrendlessAG_Hist iteration function                              | 
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
//--- ���������� ���������� � ��������� ������  
   double price,x1xma;
//--- ���������� ������������� ���������� � ��������� ��� ������������ �����
   int first,bar;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0;                   // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method1,XPhase1,XLength1,price,bar,false);      
      double Res=price-x1xma;
      Value[Count[0]]=MathAbs(Res);     
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,PointsCount);      
      if(bar<min_rates_2) continue;      
      ArrayCopy(Sort,Value,0,0,WHOLE_ARRAY);
      ArraySort(Sort);      
      double level_100=Sort[Start];
      if(level_100) Res*=100/(level_100);
      else Res=EMPTY_VALUE;       
      IndBuffer[bar]=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method2,XPhase2,XLength2,Res,bar,false);
     }
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//--- �������� ���� ��������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      ColorIndBuffer[bar]=2;
//---
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) ColorIndBuffer[bar]=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) ColorIndBuffer[bar]=3;
        }
//---
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) ColorIndBuffer[bar]=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) ColorIndBuffer[bar]=1;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
