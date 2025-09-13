//+---------------------------------------------------------------------+
//|                                                    AFL_WinnerV2.mq5 | 
//|                                   Copyright � 2016, Andrey Voytenko | 
//|                           https://login.mql5.com/en/users/avoitenko | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2016, Andrey Voytenko"
#property link "https://login.mql5.com/en/users/avoitenko"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ �������
#property indicator_buffers 3 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//--- ������ � ������� ����������� ����� ���������� ���� ����������
#property indicator_maximum +60
#property indicator_minimum -60
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������������ �����������
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- � �������� ������ ���������� ������������
#property indicator_color1  clrRed,clrViolet,clrPaleTurquoise,clrLime
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "AFL_Winner"

//+-----------------------------------+
//|  �������� ������ CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//|  ���������� ������������          |
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
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//|  ���������� ������������          |
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
//|  ������� ��������� ����������     |
//+-----------------------------------+
input uint iAverage=5; //������ ��� ��������� ������� ������
input uint iPeriod=10; //������ ������ �����������
input Smooth_Method iMA_Method=MODE_SMA_; //����� ���������� ������� ����������� 
input uint iLength=5; //�������  �����������                    
input int iPhase=15; //�������� �����������,
                     //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_WEIGHTED_;  // ������� ���������
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  //�����
input int Shift=0; // ����� ���������� �� ����������� � �����
input int HighLevel=+40;                          // ������� ���������������
input int LowLevel=-40;                           // ������� ���������������
//+-----------------------------------+

//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double UpIndBuffer[],DnIndBuffer[],ColorIndBuffer[];
//---- ���������� ���������� ����������
int Count1[],Count2[];
double Value[],Price[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos1(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                           int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=Size;
   Max1=Max2-1;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos2(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                           int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=Size;
   Max1=Max2-1;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=int(iAverage);
   min_rates_2=min_rates_1+int(iPeriod);
   min_rates_3=min_rates_2+XMA1.GetStartBars(iMA_Method,iLength,iPhase);
   min_rates_total=min_rates_3+XMA1.GetStartBars(iMA_Method,iLength,iPhase);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Length",iLength);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("Phase",iPhase,iMA_Method);

//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count1,iPeriod);
   ArrayResize(Value,iPeriod);
   ArrayResize(Count2,iAverage);
   ArrayResize(Price,iAverage);
//----
   ArrayInitialize(Count1,0);
   ArrayInitialize(Value,0.0);
   ArrayInitialize(Count2,0);
   ArrayInitialize(Price,0.0);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpIndBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnIndBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(2,ColorIndBuffer,INDICATOR_DATA);

//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname="AFL_Winner";
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);

//---- ��������� ��������� ������� ����������
   IndicatorSetInteger(INDICATOR_LEVELS,5);   
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,+50);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MathMin(+50,HighLevel));
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,0.0);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,3,MathMax(-50,LowLevel));
   IndicatorSetDouble(INDICATOR_LEVELVALUE,4,-50);
//---- � �������� ������ ����� �������������� ������� ������������ 
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,3,clrRed);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,4,clrRed);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_SOLID);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,3,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,4,STYLE_DASHDOT);
//---- ���������� �������������
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//---- ���������� ���������� � ��������� ������  
   double rsv,scost5,max,min,x1xma,x2xma;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar,clr;
   long svolume5;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ����
      Price[Count2[0]]=PriceSeries(IPC,bar,open,low,high,close);
      if(bar<min_rates_1-1)
        {
         if(bar<rates_total-1) Recount_ArrayZeroPos2(Count2,iAverage);
         continue;
        }
      //---- 
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

      if(bar<min_rates_2-1)
        {
         if(bar<rates_total-1)
           {
            Recount_ArrayZeroPos1(Count1,iPeriod);
            Recount_ArrayZeroPos2(Count2,iAverage);
           }
         continue;
        }

      max=Value[ArrayMaximum(Value,0,iPeriod)];
      min=Value[ArrayMinimum(Value,0,iPeriod)];
      rsv=((Value[Count1[0]]-min)/MathMax(max-min,_Point))*100-50;
      x1xma=XMA1.XMASeries(min_rates_2,prev_calculated,rates_total,iMA_Method,iPhase,iLength,rsv,bar,false);
      x2xma=XMA2.XMASeries(min_rates_3,prev_calculated,rates_total,iMA_Method,iPhase,iLength,x1xma,bar,false);
      //----       
      if(x1xma>x2xma)
        {
         UpIndBuffer[bar]=x1xma;
         DnIndBuffer[bar]=x2xma;
         if((x1xma>HighLevel) || (x1xma>LowLevel && x2xma<=LowLevel)) clr=3;
         else clr=2;
        }
      else
        {
         UpIndBuffer[bar]=x2xma;
         DnIndBuffer[bar]=x1xma;
         if((x1xma<LowLevel) || ((x2xma>HighLevel && x1xma<=HighLevel))) clr=0;
         else clr=1;
        }
      ColorIndBuffer[bar]=clr;

      if(bar<rates_total-1) Recount_ArrayZeroPos1(Count1,iPeriod);
      if(bar<rates_total-1) Recount_ArrayZeroPos2(Count2,iAverage);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
