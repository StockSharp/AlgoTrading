//+---------------------------------------------------------------------+
//|                                                  AFL_WinnerSign.mq5 | 
//|                                   Copyright � 2011, Andrey Voytenko | 
//|                           https://login.mql5.com/en/users/avoitenko | 
//+---------------------------------------------------------------------+ 
//| ��� ������ ���������� ������� �������� ���� SmoothAlgorithms.mqh    |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2011, Andrey Voytenko"
#property link "https://login.mql5.com/en/users/avoitenko"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ��������� ����� ���������� ����������� Magenta ����
#property indicator_color1  clrMagenta
//--- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//--- ����������� ����� ����� ����������
#property indicator_label1  "NRatioSign Sell"
//+----------------------------------------------+
//| ��������� ��������� ������ ����������        |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ����� ����� ���������� ����������� BlueViolet ����
#property indicator_color2  clrBlueViolet
//--- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//--- ����������� ��������� ����� ����������
#property indicator_label2 "NRatioSign Buy"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//--- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//| ���������� ��������               |
//+-----------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
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
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint iAverage=5;                            // ������ ��� ��������� ������� ������
input uint iPeriod=10;                            // ������ ������ �����������
input Smooth_Method iMA_Method=MODE_SMA;          // ����� ���������� ������� ����������� 
input uint iLength=5;                             // ������� �����������
input int iPhase=15;                              // �������� �����������
//--- iPhase: ��� JJMA ���������� � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- iPhase: ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_WEIGHTED;          // ������� ���������
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK; // �����
input int Shift=0;                                // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double SellBuffer[],BuyBuffer[];
//--- ���������� ���������� ����������
int Count1[],Count2[];
double Value[],Price[];
int ATR_Handle;
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+
//| �������� ������� ������ ������ �������� � �������                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos1(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
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
//| �������� ������� ������ ������ �������� � �������                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos2(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
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
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_1=int(iAverage);
   min_rates_2=min_rates_1+int(iPeriod);
   min_rates_3=min_rates_2+XMA1.GetStartBars(iMA_Method,iLength,iPhase);
   min_rates_total=min_rates_3+XMA1.GetStartBars(iMA_Method,iLength,iPhase);
   int ATR_Period=10;
   min_rates_total=int(MathMax(min_rates_total+1,ATR_Period));
//--- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Length",iLength);
//--- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("Phase",iPhase,iMA_Method);
//--- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }
//--- ������������� ������ ��� ������� ����������  
   ArrayResize(Count1,iPeriod);
   ArrayResize(Value,iPeriod);
   ArrayResize(Count2,iAverage);
   ArrayResize(Price,iAverage);
//---
   ArrayInitialize(Count1,0);
   ArrayInitialize(Value,0.0);
   ArrayInitialize(Count2,0);
   ArrayInitialize(Price,0.0);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,175);
//--- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,175);
//--- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname="AFL_WinnerSign";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+ 
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
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
   if(BarsCalculated(ATR_Handle)<Bars(Symbol(),PERIOD_CURRENT) || rates_total<min_rates_total) return(RESET);
//--- ���������� ���������� � ��������� ������  
   double rsv,scost5,max,min,x1xma,x2xma,ATR[1];
//--- ���������� ������������� ���������� � ��������� ��� ������������ �����
   int first,bar,trend0;
   long svolume5;
   static int trend1;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
      trend1=0;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //--- ����� ������� PriceSeries ��� ��������� ������� ����
      Price[Count2[0]]=PriceSeries(IPC,bar,open,low,high,close);
      if(bar<min_rates_1-1)
        {
         if(bar<rates_total-1) Recount_ArrayZeroPos2(Count2,iAverage);
         continue;
        }
      //--- 
      scost5=0;
      svolume5=0;
      for(int kkk=0; kkk<int(iAverage); kkk++)
        {
         long res;
         if(VolumeType==VOLUME_TICK) res=long(tick_volume[bar-kkk]);
         else res=long(volume[bar-kkk]);
         scost5+=res*Price[Count2[kkk]];
         svolume5+=res;
        }
      svolume5=MathMax(svolume5,1);
      Value[Count1[0]]=scost5/svolume5;
      //---
      if(bar<min_rates_2-1)
        {
         if(bar<rates_total-1)
           {
            Recount_ArrayZeroPos1(Count1,iPeriod);
            Recount_ArrayZeroPos2(Count2,iAverage);
           }
         continue;
        }
      //---
      max=Value[ArrayMaximum(Value,0,iPeriod)];
      min=Value[ArrayMinimum(Value,0,iPeriod)];
      rsv=((Value[Count1[0]]-min)/MathMax(max-min,_Point))*100-50;
      x1xma=XMA1.XMASeries(min_rates_2,prev_calculated,rates_total,iMA_Method,iPhase,iLength,rsv,bar,false);
      x2xma=XMA2.XMASeries(min_rates_3,prev_calculated,rates_total,iMA_Method,iPhase,iLength,x1xma,bar,false);
      //---  
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //---      
      if(x1xma>x2xma)
        {
         if(trend1<=0)
           {
            //--- �������� ����� ����������� ������ � ������
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
           }
         trend0=+1;
        }
      else
        {
         if(trend1>=0)
           {
            //--- �������� ����� ����������� ������ � ������
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            SellBuffer[bar]=high[bar]+ATR[0]*3/8;
           }
         trend0=-1;
        }
      //---
      if(bar<rates_total-1)
        {
         trend1=trend0;
         Recount_ArrayZeroPos1(Count1,iPeriod);
         Recount_ArrayZeroPos2(Count2,iAverage);
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
