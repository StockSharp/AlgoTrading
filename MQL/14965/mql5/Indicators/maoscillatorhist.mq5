//+---------------------------------------------------------------------+
//|                                                MAOscillatorHist.mq5 | 
//|                                Copyright � 2014, Vitalie Postolache | 
//|                                                 http://www.mql4.com | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2014, Vitalie Postolache"
#property link "http://www.mql4.com"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ �������
#property indicator_buffers 2
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ������������� ����������� ������������
#property indicator_color1 clrDarkOrange,clrYellow,clrGray,clrAqua,clrDodgerBlue
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "MAOscillator"

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
input Smooth_Method MA_Method1=MODE_SMA_; //����� ���������� ������� �������
input uint Length1=13; //�������  ������� �������                    
input int Phase1=15; //�������� ������� �������,
                     //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method2=MODE_SMA_; //����� ���������� ������� �������
input uint Length2=24; //�������  ������� ������� 
input int Phase2=15;  //�������� ������� �������,
                      //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE;//������� ���������
input uint iPeriod=0; //����� ����� ��������� � �����                    
input int Shift=0; // ����� ���������� �� ����������� � �����
//+-----------------------------------+

//---- ���������� ������������� �������, ������� ����� � 
// ���������� ����������� � �������� ������������� ������
double IndBuffer[],ColorIndBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,iPeriod_;
//---- ���������� ���������� ����������
int Count[];
double SlowXma[];
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
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
   int min_rates_1=GetStartBars(MA_Method1,Length1,Phase1);
   uint min_rates_2=GetStartBars(MA_Method2,Length2,Phase2)+iPeriod;
   min_rates_total=int(MathMax(min_rates_1,min_rates_2));
   iPeriod_=int(iPeriod+1);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Length1", Length1);
   XMA2.XMALengthCheck("Length2", Length2);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("Phase1", Phase1, MA_Method1);
   XMA2.XMAPhaseCheck("Phase2", Phase2, MA_Method2);

//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,iPeriod_);
   ArrayResize(SlowXma,iPeriod_);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(MA_Method1);
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   StringConcatenate(shortname,"MAOscillator(",Length1,", ",Length2,", ",Smooth1,", ",Smooth2,", ",iPeriod,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//---- ���������� ���������� � ��������� ������  
   double price,FastXma,MAOscillator;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
      ArrayInitialize(Count,0);
      ArrayInitialize(SlowXma,0.0);
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      FastXma=XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,price,bar,false);
      SlowXma[Count[0]]=XMA2.XMASeries(0,prev_calculated,rates_total,MA_Method2,Phase2,Length2,price,bar,false);
      MAOscillator=(FastXma-SlowXma[Count[iPeriod]])/_Point;
      IndBuffer[bar]=MAOscillator;
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,iPeriod_);
     }
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//---- �������� ���� ��������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      int clr=2;
      
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }

      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }
      ColorIndBuffer[bar]=clr;
     }
//----        
   return(rates_total);
  }
//+------------------------------------------------------------------+
