//+------------------------------------------------------------------+
//|                                                    PPO_Cloud.mq5 |
//|                                       Copyright � 2007 Tom Balfe | 
//|                                         redcarsarasota@yahoo.com | 
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2007 Tom Balfe"
//---- ������ �� ���� ������
#property link "redcarsarasota@yahoo.com"
#property description "This is a momentum indicator."
#property description "Signal line is EMA of PPO."
#property description "Follows formula: PPO=(FastEMA-SlowEMA)/SlowEMA"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1  0
#property indicator_levelcolor clrBlueViolet
#property indicator_levelstyle STYLE_SOLID
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����jd ���������� ������������
#property indicator_color1  clrDodgerBlue,clrPurple
//---- ����������� ����� ����� ����������
#property indicator_label1  "PPO_Cloud"
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3;
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
input Smooth_Method FastMethod=MODE_EMA_; //����� �������� ����������
input uint FastLength=12; //������� �������� ����������          
input int FastPhase=15; //�������� �������� ����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method SlowMethod=MODE_EMA_; //����� ���������� ����������
input uint SlowLength=26; //������� ���������� ����������                 
input int SlowPhase=15; //�������� ���������� ����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method SignMethod=MODE_EMA_; //����� �����������
input uint SignLength=9; //������� �����������                    
input int SignPhase=15; //�������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE_;//������� ���������
input int Shift=0; // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double PPOBuffer[],SignBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_,min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_=int(MathMax(GetStartBars(FastMethod,FastLength,FastPhase),GetStartBars(SlowMethod,SlowLength,SlowPhase)));
   min_rates_total=min_rates_+GetStartBars(SignMethod,SignLength,SignPhase);

//---- ����������� ������������� ������� SignBuffer � ������������ �����
   SetIndexBuffer(0,PPOBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� PPOBuffer � ������������ �����
   SetIndexBuffer(1,SignBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"PPO_Cloud");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(int bar=first; bar<rates_total && !IsStopped(); bar++)
     {

      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price_
      double price=PriceSeries(IPC,bar,open,low,high,close);     
      double fast=XMA1.XMASeries(0,prev_calculated,rates_total,FastMethod,FastPhase,FastLength,price,bar,false);
      double slow=XMA2.XMASeries(0,prev_calculated,rates_total,SlowMethod,SlowPhase,SlowLength,price,bar,false);
      if(!slow) slow=1;
      double ppo=(fast-slow)/slow;
      PPOBuffer[bar]=ppo;
      SignBuffer[bar]=XMA3.XMASeries(min_rates_,prev_calculated,rates_total,SignMethod,SignPhase,SignLength,ppo,bar,false);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
